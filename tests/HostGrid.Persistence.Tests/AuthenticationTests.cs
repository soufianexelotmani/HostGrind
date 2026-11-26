using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using HostGrid.Api.Integrations.Email;
using HostGrid.Api.Integrations.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HostGrid.Persistence.Tests;

public sealed class AuthenticationTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    private const string Password = "A long integration test passphrase";
    private const string NewPassword = "A different integration passphrase";
    private const string Base = "/api/v1/auth";
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LoginRejectsWrongPasswordAndLocksAfterFiveFailures()
    {
        using var application = new Application(database.ConnectionString);
        var account = await AddAccountAsync(application);
        using var client = Client(application);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failure = await Post(client, "/login", new { account.Email, Password = "wrong" });
            Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        }
        using var blocked = await Post(client, "/login", new { account.Email, Password });
        Assert.Equal(HttpStatusCode.Unauthorized, blocked.StatusCode);
        await using var context = database.CreateContext();
        var user = await context.Users.SingleAsync(row => row.Id == account.UserId, Token);
        Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow.AddMinutes(14));
    }

    [Fact]
    public async Task AccessRejectsExpiredAndInvalidTokensAndLogoutRevokesTheSession()
    {
        using var application = new Application(database.ConnectionString);
        var account = await AddAccountAsync(application);
        using var client = Client(application);
        var (session, cookie) = await LoginAsync(client, account);
        Assert.Equal(HttpStatusCode.OK, await Status(client, "/api/v1/auth/me", session.AccessToken));
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/v1/auth/me", "invalid"));
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/v1/auth/me", Expired(session.AccessToken!, application.Key)));
        using var logout = await Post(client, "/logout", new { }, cookie);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/v1/auth/me", session.AccessToken));
        using var refresh = await Post(client, "/refresh", new { }, cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task RefreshRotationAndReuseRevokeTheWholeFamily()
    {
        using var application = new Application(database.ConnectionString);
        var account = await AddAccountAsync(application);
        using var client = Client(application);
        var (_, original) = await LoginAsync(client, account);
        using var rotated = await Post(client, "/refresh", new { }, original);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var replacement = Cookie(rotated);
        Assert.NotEqual(original, replacement);
        using var replay = await Post(client, "/refresh", new { }, original);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using var revoked = await Post(client, "/refresh", new { }, replacement);
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRefreshCreatesAtMostOneReplacementAndRevokesOnReuse()
    {
        using var application = new Application(database.ConnectionString);
        var account = await AddAccountAsync(application);
        using var client = Client(application);
        var (_, original) = await LoginAsync(client, account);
        var responses = await Task.WhenAll(Post(client, "/refresh", new { }, original), Post(client, "/refresh", new { }, original));
        using var accepted = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        using var denied = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
        using var later = await Post(client, "/refresh", new { }, Cookie(accepted));
        Assert.Equal(HttpStatusCode.Unauthorized, later.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal(2, await context.Set<RefreshToken>().CountAsync(row => row.UserId == account.UserId, Token));
    }

    [Fact]
    public async Task DisabledAccountCannotReuseExistingAccessOrRefresh()
    {
        using var application = new Application(database.ConnectionString);
        var account = await AddAccountAsync(application);
        using var client = Client(application);
        var (session, cookie) = await LoginAsync(client, account);
        await using (var context = database.CreateContext())
        {
            await context.Users.Where(user => user.Id == account.UserId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.IsActive, false), Token);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/v1/auth/me", session.AccessToken));
        using var failed = await Post(client, "/login", new { account.Email, Password });
        using var refresh = await Post(client, "/refresh", new { }, cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task ManagerMustEnrollAndVerifyBeforeReceivingAFullSession()
    {
        using var application = new Application(database.ConnectionString);
        var account = await AddAccountAsync(application, OrganizationRole.Manager);
        using var client = Client(application);
        var usedTotp = "";
        var (session, _) = await EnrollAsync(client, account, code => usedTotp = code);
        Assert.True(session.User!.MfaEnabled);
        Assert.Equal(10, session.RecoveryCodes!.Length);
        Assert.Equal(HttpStatusCode.OK, await Status(client, "/api/v1/auth/me", session.AccessToken));
        using var login = await Post(client, "/login", new { account.Email, Password });
        var pending = await Read<AuthResponse>(login);
        Assert.Equal("mfa_required", pending.Status);
        using var repeatedCode = await Post(client, "/mfa/verify", new { pending.Challenge, Code = usedTotp });
        Assert.Equal(HttpStatusCode.Unauthorized, repeatedCode.StatusCode);
        using var recovered = await Post(client, "/mfa/verify",
            new { pending.Challenge, Code = session.RecoveryCodes[0], Recovery = true });
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        using var loginAgain = await Post(client, "/login", new { account.Email, Password });
        var next = await Read<AuthResponse>(loginAgain);
        using var reused = await Post(client, "/mfa/verify",
            new { next.Challenge, Code = session.RecoveryCodes[0], Recovery = true });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task PasswordResetIsSingleUseAndRevokesExistingTokens()
    {
        using var application = new Application(database.ConnectionString);
        var account = await AddAccountAsync(application);
        using var client = Client(application);
        var (session, _) = await LoginAsync(client, account);
        using var request = await Post(client, "/password/reset-request", new { account.Email });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var proof = application.Email.Proof();
        var body = new { account.Email, Token = proof, Password = NewPassword };
        using var success = await Post(client, "/password/reset", body);
        Assert.Equal(HttpStatusCode.NoContent, success.StatusCode);
        using var reuse = await Post(client, "/password/reset", body);
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/v1/auth/me", session.AccessToken));
    }

    [Fact]
    public async Task ExpiredPasswordResetProofIsRejected()
    {
        using var application = new Application(database.ConnectionString, expiredLinks: true);
        var account = await AddAccountAsync(application);
        using var client = Client(application);
        using var request = await Post(client, "/password/reset-request", new { account.Email });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        using var result = await Post(client, "/password/reset",
            new { account.Email, Token = application.Email.Proof(), Password = NewPassword });
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task ManagerCannotGrantManagerOrSuperAdminAndCannotEnterAnotherOrganization()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        var foreign = await AddAccountAsync(application);
        using var client = Client(application);
        var (session, _) = await EnrollAsync(client, manager);
        foreach (var role in new[] { "Manager", "SuperAdmin" })
        {
            using var denied = await Send(client, HttpMethod.Post, InvitePath(manager.OrganizationId), session.AccessToken,
                new { Email = "new-user@example.test", Name = "New user", Role = role });
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
        using var cross = await Send(client, HttpMethod.Post, InvitePath(foreign.OrganizationId), session.AccessToken,
            new { Email = "new-user@example.test", Name = "New user", Role = "Owner" });
        Assert.Equal(HttpStatusCode.Forbidden, cross.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, "/api/v1/admin/organizations", session.AccessToken));
    }

    [Fact]
    public async Task OwnerCanReadOnlyTheirPropertyAndMissingScopeFailsClosed()
    {
        using var application = new Application(database.ConnectionString);
        var owner = await AddAccountAsync(application);
        var neighbor = await AddAccountAsync(application, organizationId: owner.OrganizationId);
        var foreign = await AddAccountAsync(application);
        using var client = Client(application);
        var (session, _) = await LoginAsync(client, owner);
        Assert.Equal(HttpStatusCode.NoContent, await Status(client, PropertyPath(owner), session.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, PropertyPath(neighbor), session.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, PropertyPath(foreign), session.AccessToken));
        await using var context = database.CreateContext();
        Assert.False(await context.Set<Property>().AnyAsync(Token));
    }

    [Fact]
    public async Task ExplicitAdministratorAccessWorksAndMembershipRemovalInvalidatesAuthorization()
    {
        using var application = new Application(database.ConnectionString);
        var admin = await AddAccountAsync(application, superAdmin: true);
        var owner = await AddAccountAsync(application);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        using var client = Client(application);
        var (adminSession, _) = await EnrollAsync(client, admin);
        var (ownerSession, ownerCookie) = await LoginAsync(client, owner);
        Assert.Equal(HttpStatusCode.OK, await Status(client, "/api/v1/admin/organizations", adminSession.AccessToken));
        Assert.Equal(HttpStatusCode.NoContent, await Status(client, PropertyPath(owner), adminSession.AccessToken));
        using var assigned = await Send(client, HttpMethod.Post,
            $"/api/v1/admin/organizations/{owner.OrganizationId}/managers/{manager.UserId}",
            adminSession.AccessToken, new { });
        Assert.Equal(HttpStatusCode.NoContent, assigned.StatusCode);
        await using (var tenant = database.CreateContext(owner.OrganizationId))
        {
            Assert.True(await tenant.Set<OrganizationMember>().AnyAsync(row =>
                row.UserId == manager.UserId && row.Role == OrganizationRole.Manager && row.IsActive, Token));
        }
        using var result = await Send(client, HttpMethod.Put,
            $"/api/v1/organizations/{owner.OrganizationId}/accounts/{owner.UserId}/membership",
            adminSession.AccessToken, new { Active = false, Role = "Owner" });
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/v1/auth/me", ownerSession.AccessToken));
        using var refresh = await Post(client, "/refresh", new { }, ownerCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task CookieMutationRejectsMissingCsrfHeaderAndForeignOrigin()
    {
        using var application = new Application(database.ConnectionString);
        using var client = Client(application);
        using var missing = await client.PostAsJsonAsync(Base + "/refresh", new { }, Token);
        Assert.Equal(HttpStatusCode.Forbidden, missing.StatusCode);
        using var message = new HttpRequestMessage(HttpMethod.Post, Base + "/refresh") { Content = JsonContent.Create(new { }) };
        message.Headers.Add("X-HostGrid-CSRF", "1");
        message.Headers.Add("Origin", "https://foreign.example.test");
        using var foreign = await client.SendAsync(message, Token);
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
    }

    [Fact]
    public async Task ManagerInvitationActivatesAnOwnerOnceAndPasswordChangeRevokesTheSession()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        using var client = application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), HandleCookies = false, AllowAutoRedirect = false
        });
        var (managerSession, _) = await EnrollAsync(client, manager);
        var email = $"{Guid.NewGuid():N}@example.test";
        using var invite = await Send(client, HttpMethod.Post, InvitePath(manager.OrganizationId), managerSession.AccessToken,
            new { Email = email, Name = "Invited owner", Role = "Owner" });
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        var body = new { Email = email, Token = application.Email.Proof(), Password };
        using var activation = await Post(client, "/activate", body);
        Assert.Equal(HttpStatusCode.NoContent, activation.StatusCode);
        using var replay = await Post(client, "/activate", body);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        using var login = await Post(client, "/login", new { Email = email, Password });
        var owner = await Read<AuthResponse>(login);
        Assert.Equal("Owner", Assert.Single(owner.User!.Memberships).Role);
        Assert.Contains("; secure", Assert.Single(login.Headers.GetValues("Set-Cookie")), StringComparison.OrdinalIgnoreCase);
        using var change = await Send(client, HttpMethod.Post, Base + "/password/change", owner.AccessToken,
            new { CurrentPassword = Password, Password = NewPassword });
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, "/api/v1/auth/me", owner.AccessToken));
    }

    private async Task<Account> AddAccountAsync(Application application, OrganizationRole role = OrganizationRole.Owner,
        Guid? organizationId = null, bool superAdmin = false)
    {
        var organization = organizationId ?? Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@example.test";
        await using var scope = application.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true,
            IsActive = true, ActivatedAt = DateTime.UtcNow, LockoutEnabled = true };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        if (superAdmin)
        {
            Assert.True((await users.AddToRoleAsync(user, "SuperAdmin")).Succeeded);
            return new(user.Id, organization, email, Guid.Empty);
        }
        await using var context = database.CreateContext(organization);
        if (organizationId is null)
        {
            context.Add(new Organization { Id = organization, Name = "Test organization", Reference = organization.ToString("N"), IsActive = true });
        }
        context.Add(new OrganizationMember { OrganizationId = organization, UserId = user.Id, Role = role, IsActive = true });
        await context.SaveChangesAsync(Token);
        if (role != OrganizationRole.Owner) return new(user.Id, organization, email, Guid.Empty);
        var owner = new Owner { OrganizationId = organization, UserId = user.Id, Name = "Test owner",
            Reference = user.Id.ToString("N"), IsActive = true };
        context.Add(owner);
        await context.SaveChangesAsync(Token);
        var property = new Property { OrganizationId = organization, OwnerId = owner.Id,
            TypeId = await context.Set<PropertyType>().Select(row => row.Id).FirstAsync(Token),
            Name = "Test property", Reference = user.Id.ToString("N"), Address = "Test", City = "Test", MaxGuests = 1 };
        context.Add(property);
        await context.SaveChangesAsync(Token);
        return new(user.Id, organization, email, property.Id);
    }

    private static HttpClient Client(Application app)
        => app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });

    [Fact]
    public async Task ManagerCanCreateAndInviteAnOwnerThenOwnerCanReadOnlyTheirPortfolio()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        var other = await AddAccountAsync(application, organizationId: manager.OrganizationId);
        var foreign = await AddAccountAsync(application);
        using var client = Client(application);
        var (managerSession, _) = await EnrollAsync(client, manager);
        var org = manager.OrganizationId;
        var ownerEmail = $"{Guid.NewGuid():N}@example.test";
        using (var invalid = await Send(client, HttpMethod.Post, Portfolio(org, "owners"), managerSession.AccessToken,
            new { Name = "", Email = "invalid", Phone = "" }))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var ownerId = await CreatePortfolioOwnerAsync(client, org, managerSession.AccessToken!, ownerEmail);
        var type = await Read<List<Option>>(await Send(client, HttpMethod.Get, Portfolio(org, "properties/types"), managerSession.AccessToken));
        Assert.Equal(6, type.Count);
        using (var cross = await Send(client, HttpMethod.Post, Portfolio(org, "properties"), managerSession.AccessToken,
            PropertyInput(await OwnerId(foreign), type[0].Id, "Cross", "Rabat")))
            Assert.Equal(HttpStatusCode.BadRequest, cross.StatusCode);
        var first = await CreatePropertyAsync(client, org, managerSession.AccessToken!, ownerId, type[0].Id, "Zeta test", "Fes");
        var second = await CreatePropertyAsync(client, org, managerSession.AccessToken!, ownerId, type[0].Id, "Alpha test", "Fes");
        using var list = await Send(client, HttpMethod.Get, Portfolio(org, $"properties?owner={ownerId}&search=Fes&sort=name&page=1&size=1"), managerSession.AccessToken);
        var paged = await Read<Page<PropertySummary>>(list);
        Assert.Equal(2, paged.Total);
        Assert.Equal(second, Assert.Single(paged.Items).Id);
        using (var denied = await Send(client, HttpMethod.Get, Portfolio(foreign.OrganizationId, "properties"), managerSession.AccessToken))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var invite = await Send(client, HttpMethod.Post, Portfolio(org, $"owners/{ownerId}/invitation"), managerSession.AccessToken, new { }))
            Assert.True(invite.IsSuccessStatusCode);
        using (var activate = await Post(client, "/activate", new { Email = ownerEmail, Token = application.Email.Proof(), Password }))
            Assert.Equal(HttpStatusCode.NoContent, activate.StatusCode);
        using var login = await Post(client, "/login", new { Email = ownerEmail, Password });
        var ownerSession = await Read<AuthResponse>(login);
        Assert.Equal(ownerId, Assert.Single(ownerSession.User!.Memberships).OwnerId);
        Assert.Equal(HttpStatusCode.OK, await Status(client, Portfolio(org, $"properties/{first}"), ownerSession.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, Portfolio(org, $"properties/{other.PropertyId}"), ownerSession.AccessToken));
        using (var write = await Send(client, HttpMethod.Put, Portfolio(org, $"properties/{first}"), ownerSession.AccessToken,
            PropertyInput(ownerId, type[0].Id, "Unauthorized", "Fes")))
            Assert.Equal(HttpStatusCode.NotFound, write.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, Portfolio(org, "owners"), ownerSession.AccessToken));
    }

    [Fact]
    public async Task PropertyWritesValidateHistoryPricingInventoryAndConcurrency()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        using var client = Client(application);
        var (auth, _) = await EnrollAsync(client, manager);
        var org = manager.OrganizationId;
        var token = auth.AccessToken!;
        var ownerId = await CreatePortfolioOwnerAsync(client, org, token, $"{Guid.NewGuid():N}@example.test");
        using var catalog = await Send(client, HttpMethod.Get, Portfolio(org, "properties/types"), token);
        var type = (await Read<List<Option>>(catalog))[0];
        var id = await CreatePropertyAsync(client, org, token, ownerId, type.Id, "Riad test", "Rabat");
        var path = "properties/" + id;

        async Task<HttpStatusCode> PostTo(string suffix, object body)
        {
            using var response = await Send(client, HttpMethod.Post, Portfolio(org, path + "/" + suffix), token, body);
            return response.StatusCode;
        }
        Assert.Equal(HttpStatusCode.BadRequest, await PostTo("contracts", new { StartDate = "2030-01-10", EndDate = "2030-01-01", Rate = 20 }));
        Assert.Equal(HttpStatusCode.OK, await PostTo("contracts", new { StartDate = "2030-01-01", EndDate = "2030-02-01", Rate = 21 }));
        Assert.Equal(HttpStatusCode.Conflict, await PostTo("contracts", new { StartDate = "2030-01-10", EndDate = "2030-02-05", Rate = 25 }));
        Assert.Equal(HttpStatusCode.BadRequest, await PostTo("commission-rates", new { EffectiveFrom = "2030-01-01", Rate = 101 }));
        Assert.Equal(HttpStatusCode.OK, await PostTo("commission-rates", new { EffectiveFrom = "2030-01-01", Rate = 20 }));
        Assert.Equal(HttpStatusCode.OK, await PostTo("commission-rates", new { EffectiveFrom = "2030-02-01", Rate = 23.5 }));
        Assert.Equal(HttpStatusCode.BadRequest, await PostTo("inventory", new { Name = "Chair", Quantity = -1 }));
        Assert.Equal(HttpStatusCode.OK, await PostTo("inventory", new { Name = "Chair", Quantity = 3 }));
        Assert.Equal(HttpStatusCode.OK, await PostTo("seasonal-rates", Rule(1, 65, 700)));
        Assert.Equal(HttpStatusCode.OK, await PostTo("seasonal-rates", Rule(2, 127, 900)));
        Assert.Equal(HttpStatusCode.OK, await PostTo("seasonal-rates", Rule(3, 127, 1200)));
        Assert.Equal(HttpStatusCode.Conflict, await PostTo("seasonal-rates", Rule(3, 127, 1400)));
        using var price = await Send(client, HttpMethod.Get, Portfolio(org, path + "/nightly-rate?night=2030-01-05"), token);
        Assert.Equal(1200m, (await Read<Amount>(price)).Value);

        using var snapshot = await Send(client, HttpMethod.Get, Portfolio(org, path), token);
        var detail = await Read<PropertyView>(snapshot);
        Assert.Equal(2, detail.CommissionRates.Count);
        Assert.Equal(new DateOnly(2030, 2, 1), detail.CommissionRates[1].EffectiveTo);
        Assert.Equal(20m, detail.CommissionRates[1].Rate);
        Assert.Equal(21m, Assert.Single(detail.Contracts).Rate);
        using var changed = await Send(client, HttpMethod.Put, Portfolio(org, path), token,
            PropertyInput(ownerId, type.Id, "Updated", "Rabat", detail.Version));
        Assert.True(changed.IsSuccessStatusCode);
        using var stale = await Send(client, HttpMethod.Put, Portfolio(org, path), token,
            PropertyInput(ownerId, type.Id, "Stale", "Rabat", detail.Version));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var fresh = await Send(client, HttpMethod.Get, Portfolio(org, path), token);
        var current = await Read<PropertyView>(fresh);
        using var archive = await Send(client, HttpMethod.Put, Portfolio(org, path + "/status"), token, new { Status = 2, current.Version });
        Assert.True(archive.IsSuccessStatusCode);
        using var archived = await Send(client, HttpMethod.Get, Portfolio(org, path), token);
        Assert.Equal(PropertyStatus.Archived, (await Read<PropertyView>(archived)).Summary.Status);

        await using var context = database.CreateContext(org);
        var row = await context.Set<Property>().AsNoTracking().SingleAsync(value => value.Id == id, Token);
        row.Id = Guid.NewGuid();
        context.Add(row);
        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Token));
        Assert.Contains(Assert.IsType<Microsoft.Data.SqlClient.SqlException>(failure.InnerException).Number, new[] { 2601, 2627 });
    }

    [Fact]
    public async Task FilesCheckSignatureSizePrivateKeysAndOwnerAuthorization()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        var owner = await AddAccountAsync(application, organizationId: manager.OrganizationId);
        var other = await AddAccountAsync(application, organizationId: manager.OrganizationId);
        using var client = Client(application);
        var (m, _) = await EnrollAsync(client, manager);
        var (o, _) = await LoginAsync(client, owner);
        var (b, _) = await LoginAsync(client, other);
        var path = Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}");
        var document = path + "/documents";
        using (var signature = await Upload(client, document, m.AccessToken!, "test.pdf", "unrecognized"u8.ToArray()))
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, signature.StatusCode);
        using (var size = await Upload(client, path + "/photos", m.AccessToken!, "test.jpg", new byte[1024 * 1024 * 10 + 1]))
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, size.StatusCode);
        using var uploaded = await Upload(client, document, m.AccessToken!, "../contract.pdf", "%PDF-1.7\n%%EOF"u8.ToArray());
        var file = await Read<FileView>(uploaded);
        Assert.Equal("contract.pdf", file.Name);
        Assert.Equal(FileScanStatus.Quarantined, file.ScanStatus);
        Assert.Equal(HttpStatusCode.OK, await Status(client, document + "/" + file.Id, o.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, document + "/" + file.Id, b.AccessToken));
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, document + "/" + file.Id, null));
        var storage = application.Services.GetRequiredService<IFileStorageService>();
        await Assert.ThrowsAsync<ArgumentException>(() => storage.OpenAsync("../contract.pdf", Token));
    }

    private async Task<Guid> OwnerId(Account account)
    {
        await using var context = database.CreateContext(account.OrganizationId);
        return await context.Set<Owner>().Where(owner => owner.UserId == account.UserId).Select(owner => owner.Id).SingleAsync(Token);
    }

    private static async Task<Guid> CreatePortfolioOwnerAsync(HttpClient client, Guid org, string token, string email)
    {
        using var response = await Send(client, HttpMethod.Post, Portfolio(org, "owners"), token,
            new { Name = "Test owner", Email = email, Phone = "555123" });
        return (await Read<Created>(response)).Id;
    }

    private static async Task<Guid> CreatePropertyAsync(HttpClient client, Guid org, string token, Guid ownerId, Guid typeId, string name, string city)
    {
        using var response = await Send(client, HttpMethod.Post, Portfolio(org, "properties"), token,
            PropertyInput(ownerId, typeId, name, city));
        return (await Read<Created>(response)).Id;
    }

    private static PropertyWrite PropertyInput(Guid ownerId, Guid typeId, string name, string city, byte[]? version = null)
        => new(ownerId, typeId, name, null, "Test address", city, null, null, 2, 1, 1, 500, new(15, 0), new(11, 0), version);

    private static object Rule(int priority, int mask, int amount) => new
        { StartDate = "2030-01-01", EndDate = "2030-02-01", DaysOfWeek = mask, NightlyRate = amount, Priority = priority };

    private static Task<HttpResponseMessage> Upload(HttpClient client, string url, string token, string name, byte[] bytes)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new("Bearer", token);
        request.Headers.Add("X-HostGrid-CSRF", "1");
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "file", name);
        request.Content = content;
        return client.SendAsync(request, Token);
    }

    private static string Portfolio(Guid org, string route) => $"/api/v1/organizations/{org}/{route}";
    private sealed record Created(Guid Id);
    private sealed record Amount([property: System.Text.Json.Serialization.JsonPropertyName("amount")] decimal Value);

    private static async Task<(AuthResponse Session, string Cookie)> LoginAsync(HttpClient client, Account account)
    {
        using var response = await Post(client, "/login", new { account.Email, Password });
        var session = await Read<AuthResponse>(response);
        Assert.Equal("authenticated", session.Status);
        return (session, Cookie(response));
    }

    private static async Task<(AuthResponse Session, string Cookie)> EnrollAsync(HttpClient client, Account account,
        Action<string>? captureCode = null)
    {
        using var login = await Post(client, "/login", new { account.Email, Password });
        var pending = await Read<AuthResponse>(login);
        Assert.Equal("mfa_enrollment_required", pending.Status);
        Assert.Null(pending.AccessToken);
        Assert.False(login.Headers.Contains("Set-Cookie"));
        using var setup = await Post(client, "/mfa/enrollment", new { pending.Challenge });
        var enrollment = await Read<EnrollmentView>(setup);
        var code = Totp(enrollment.SetupKey);
        using var verify = await Post(client, "/mfa/verify", new { pending.Challenge, Code = code });
        captureCode?.Invoke(code);
        return (await Read<AuthResponse>(verify), Cookie(verify));
    }

    private static async Task<HttpStatusCode> Status(HttpClient client, string url, string? bearer)
    {
        using var response = await Send(client, HttpMethod.Get, url, bearer);
        return response.StatusCode;
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string path, object body, string? cookie = null)
        => Send(client, HttpMethod.Post, Base + path, null, body, cookie);

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string? bearer,
        object? body = null, string? cookie = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-HostGrid-CSRF", "1");
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request, Token);
    }

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}");
        return (await response.Content.ReadFromJsonAsync<T>(Token))!;
    }

    private static string Cookie(HttpResponseMessage response)
    {
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        return cookie.Split(';')[0];
    }

    private static string Expired(string accessToken, byte[] key)
    {
        var handler = new JwtSecurityTokenHandler();
        var initial = handler.ReadJwtToken(accessToken);
        return handler.WriteToken(new JwtSecurityToken(initial.Issuer, initial.Audiences.Single(),
            initial.Claims.Where(claim => claim.Type is "sub" or "sid" or "sst"),
            DateTime.UtcNow.AddMinutes(-20), DateTime.UtcNow.AddMinutes(-1),
            new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)));
    }

    private static string Totp(string secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var key = new List<byte>();
        var bits = 0;
        var buffer = 0;
        foreach (var character in secret)
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8) { bits -= 8; key.Add((byte)(buffer >> bits)); }
        }
        Span<byte> counter = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        var digest = HMACSHA1.HashData(key.ToArray(), counter);
        var offset = digest[^1] & 15;
        var value = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(digest.AsSpan(offset)) & int.MaxValue;
        return (value % 1_000_000).ToString("D6");
    }

    private static string InvitePath(Guid organization) => $"/api/v1/organizations/{organization}/accounts/invitations";
    private static string PropertyPath(Account account) =>
        $"/api/v1/organizations/{account.OrganizationId}/properties/{account.PropertyId}/access";
    private sealed record Account(Guid UserId, Guid OrganizationId, string Email, Guid PropertyId);

    private sealed class Application(string connection, bool expiredLinks = false) : WebApplicationFactory<Program>
    {
        public byte[] Key { get; } = RandomNumberGenerator.GetBytes(32);
        public TestEmail Email { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HostGrid"] = connection,
                ["Security:SigningKey"] = Convert.ToBase64String(Key)
            }));
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ISecurityEmail>(Email);
                if (expiredLinks)
                    services.PostConfigure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromSeconds(-1));
            });
        }
    }

    private sealed class TestEmail : ISecurityEmail
    {
        private readonly ConcurrentQueue<string> messages = new();

        public Task SendAsync(string recipient, string subject, string body, CancellationToken token)
        {
            messages.Enqueue(body);
            return Task.CompletedTask;
        }

        public string Proof()
        {
            Assert.True(messages.TryDequeue(out var body));
            var token = body!.Split("&token=")[1];
            return Uri.UnescapeDataString(token);
        }
    }
}
