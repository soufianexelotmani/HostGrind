using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Finance;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Document;
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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using SkiaSharp;

namespace HostGrid.Persistence.Tests;

public sealed partial class AuthenticationTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
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

    private Task<Account> AddAccountAsync(Application application, OrganizationRole role = OrganizationRole.Owner,
        Guid? organizationId = null, bool superAdmin = false, bool createPortfolio = true)
        => AddAccountToCatalogAsync(application, database, role, organizationId, superAdmin, createPortfolio);

    private static async Task<Account> AddAccountToCatalogAsync(Application application, SqlServerFixture catalog,
        OrganizationRole role, Guid? organizationId = null, bool superAdmin = false, bool createPortfolio = true)
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
        await using var context = catalog.CreateContext(organization);
        if (organizationId is null)
        {
            context.Add(new Organization { Id = organization, Name = "Test organization", Reference = organization.ToString("N"), IsActive = true });
        }
        context.Add(new OrganizationMember { OrganizationId = organization, UserId = user.Id, Role = role, IsActive = true });
        await context.SaveChangesAsync(Token);
        if (role != OrganizationRole.Owner || !createPortfolio) return new(user.Id, organization, email, Guid.Empty);
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
        Assert.Equal(new[] { "Apartment", "House", "Riad", "Room", "Studio", "Villa" }, type.Select(item => item.Code));
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
        using var portfolio = await Send(client, HttpMethod.Get, Portfolio(org,
            $"properties?search=Fes&type={type[0].Id}&status=Active&sort=-name&page=2&size=1"), ownerSession.AccessToken);
        var ownerPage = await Read<Page<PropertySummary>>(portfolio);
        Assert.Equal(2, ownerPage.Total);
        Assert.Equal(second, Assert.Single(ownerPage.Items).Id);
        using var otherPortfolio = await Send(client, HttpMethod.Get, Portfolio(org,
            $"properties?owner={await OwnerId(other)}"), ownerSession.AccessToken);
        Assert.Equal(0, (await Read<Page<PropertySummary>>(otherPortfolio)).Total);
        Assert.Equal(HttpStatusCode.OK, await Status(client, Portfolio(org, $"properties/{first}"), ownerSession.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, Portfolio(org, $"properties/{other.PropertyId}"), ownerSession.AccessToken));
        using (var write = await Send(client, HttpMethod.Put, Portfolio(org, $"properties/{first}"), ownerSession.AccessToken,
            PropertyInput(ownerId, type[0].Id, "Unauthorized", "Fes")))
            Assert.Equal(HttpStatusCode.NotFound, write.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, Portfolio(org, "owners"), ownerSession.AccessToken));
    }

    [Fact]
    public async Task OwnerResendReplacesTheProofAndScopedReactivationRequiresANewSession()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        var foreign = await AddAccountAsync(application, OrganizationRole.Manager);
        var admin = await AddAccountAsync(application, superAdmin: true);
        using var client = Client(application);
        var (managerSession, _) = await EnrollAsync(client, manager);
        var (foreignSession, _) = await EnrollAsync(client, foreign);
        var (adminSession, _) = await EnrollAsync(client, admin);
        var org = manager.OrganizationId;
        var email = $"{Guid.NewGuid():N}@example.test";
        var ownerId = await CreatePortfolioOwnerAsync(client, org, managerSession.AccessToken!, email);
        var path = Portfolio(org, $"owners/{ownerId}");

        async Task<OwnerView> OwnerAsync()
        {
            using var response = await Send(client, HttpMethod.Get, path, managerSession.AccessToken);
            return (await Read<OwnerPortfolio>(response)).Owner;
        }

        async Task<HttpStatusCode> ActionAsync(string action, string? bearer)
        {
            using var response = await Send(client, HttpMethod.Post, path + "/" + action, bearer, new { });
            return response.StatusCode;
        }

        Assert.Null((await OwnerAsync()).UserId);
        Assert.Equal(HttpStatusCode.OK, await ActionAsync("invitation", managerSession.AccessToken));
        var oldProof = application.Email.Proof();
        var pending = await OwnerAsync();
        Assert.NotNull(pending.UserId);
        Assert.False(pending.Activated);
        Assert.True(pending.AccessEnabled);
        Assert.Equal(HttpStatusCode.OK, await ActionAsync("invitation", managerSession.AccessToken));
        var newProof = application.Email.Proof();
        using (var old = await Post(client, "/activate", new { Email = email, Token = oldProof, Password }))
            Assert.Equal(HttpStatusCode.BadRequest, old.StatusCode);
        using (var activation = await Post(client, "/activate", new { Email = email, Token = newProof, Password }))
            Assert.Equal(HttpStatusCode.NoContent, activation.StatusCode);
        Assert.True((await OwnerAsync()).Activated);
        Assert.Equal(HttpStatusCode.Conflict, await ActionAsync("invitation", managerSession.AccessToken));

        var account = new Account(pending.UserId!.Value, org, email, Guid.Empty);
        var (ownerSession, ownerCookie) = await LoginAsync(client, account);
        Assert.Equal(HttpStatusCode.Forbidden, await ActionAsync("disable-access", ownerSession.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await ActionAsync("enable-access", foreignSession.AccessToken));
        Assert.Equal(HttpStatusCode.OK, await ActionAsync("disable-access", managerSession.AccessToken));
        Assert.False((await OwnerAsync()).AccessEnabled);
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, Portfolio(org, "properties"), ownerSession.AccessToken));
        using (var refresh = await Post(client, "/refresh", new { }, ownerCookie))
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        Assert.Equal(HttpStatusCode.OK, await ActionAsync("enable-access", managerSession.AccessToken));
        Assert.True((await OwnerAsync()).AccessEnabled);
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, Portfolio(org, "properties"), ownerSession.AccessToken));
        using (var refresh = await Post(client, "/refresh", new { }, ownerCookie))
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        var (newSession, _) = await LoginAsync(client, account);
        Assert.Equal(HttpStatusCode.OK, await Status(client, Portfolio(org, "properties"), newSession.AccessToken));
        Assert.Equal(HttpStatusCode.OK, await ActionAsync("disable-access", managerSession.AccessToken));
        Assert.Equal(HttpStatusCode.OK, await ActionAsync("enable-access", adminSession.AccessToken));
    }

    [Fact]
    public async Task OwnerEditsAndLinksAnEligibleAccountOnlyOnceAndArchiveKeepsPropertyHistory()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        var org = manager.OrganizationId;
        var account = await AddAccountAsync(application, OrganizationRole.Owner, org, createPortfolio: false);
        using var client = Client(application);
        var (session, _) = await EnrollAsync(client, manager);
        var bearer = session.AccessToken!;
        var ownerId = await CreatePortfolioOwnerAsync(client, org, bearer, "other@example.test");
        var duplicateId = await CreatePortfolioOwnerAsync(client, org, bearer, account.Email);
        var path = Portfolio(org, $"owners/{ownerId}");

        async Task<OwnerPortfolio> DetailAsync(string url)
        {
            using var response = await Send(client, HttpMethod.Get, url, bearer);
            return await Read<OwnerPortfolio>(response);
        }

        var owner = (await DetailAsync(path)).Owner;
        using (var wrongEmail = await Send(client, HttpMethod.Post, path + "/link", bearer,
            new { account.UserId, owner.Version }))
            Assert.Equal(HttpStatusCode.NotFound, wrongEmail.StatusCode);
        using (var edit = await Send(client, HttpMethod.Put, path, bearer,
            new { Name = "Edited Owner", account.Email, Phone = "555987", owner.Version }))
            Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var edited = (await DetailAsync(path)).Owner;
        Assert.Equal("Edited Owner", edited.Name);
        Assert.Equal("555987", edited.Phone);
        using (var link = await Send(client, HttpMethod.Post, path + "/link", bearer,
            new { account.UserId, edited.Version }))
            Assert.Equal(HttpStatusCode.OK, link.StatusCode);
        var duplicatePath = Portfolio(org, $"owners/{duplicateId}");
        using (var link = await Send(client, HttpMethod.Post, duplicatePath + "/link", bearer,
            new { account.UserId, (await DetailAsync(duplicatePath)).Owner.Version }))
            Assert.Equal(HttpStatusCode.Conflict, link.StatusCode);

        using var catalog = await Send(client, HttpMethod.Get, Portfolio(org, "properties/types"), bearer);
        var type = (await Read<List<Option>>(catalog))[0];
        var propertyId = await CreatePropertyAsync(client, org, bearer, ownerId, type.Id, "Retained property", "Fes");
        await using var context = database.CreateContext(org);
        context.Add(new MonthlyClosure
        {
            OrganizationId = org, OwnerId = ownerId, PropertyId = propertyId, Year = 2025, Month = 1,
            Status = ClosureStatus.Closed, ClosedAt = DateTime.UtcNow, ClosedById = manager.UserId
        });
        await context.SaveChangesAsync(Token);
        var version = (await DetailAsync(path)).Owner.Version;
        using (var blocked = await Send(client, HttpMethod.Post, path + "/archive", bearer, new { version }))
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var propertyPath = Portfolio(org, $"properties/{propertyId}");
        using var loading = await Send(client, HttpMethod.Get, propertyPath, bearer);
        var property = await Read<PropertyView>(loading);
        using (var archive = await Send(client, HttpMethod.Put, propertyPath + "/status", bearer,
            new { Status = PropertyStatus.Archived, property.Version }))
            Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        using (var archive = await Send(client, HttpMethod.Post, path + "/archive", bearer, new { version }))
            Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        var archived = await DetailAsync(path);
        Assert.True(archived.Owner.IsArchived);
        Assert.False(archived.Owner.AccessEnabled);
        Assert.Equal(propertyId, Assert.Single(archived.Properties).Id);
        var closure = await context.Set<MonthlyClosure>().AsNoTracking()
            .SingleAsync(row => row.PropertyId == propertyId, Token);
        Assert.Equal(ownerId, closure.OwnerId);
        Assert.Equal(ClosureStatus.Closed, closure.Status);
        using (var denied = await Send(client, HttpMethod.Post, path + "/enable-access", bearer, new { }))
            Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        using var retained = await Send(client, HttpMethod.Get, propertyPath, bearer);
        var retainedProperty = await Read<PropertyView>(retained);
        Assert.Equal(PropertyStatus.Archived, retainedProperty.Summary.Status);
        using var deniedProperty = await Send(client, HttpMethod.Put, propertyPath + "/status", bearer,
            new { Status = PropertyStatus.Active, retainedProperty.Version });
        Assert.Equal(HttpStatusCode.BadRequest, deniedProperty.StatusCode);
    }

    [Fact]
    public async Task ConcurrentPropertyEditsHaveOneWinnerAndThePropertyCanBeReactivated()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        using var client = Client(application);
        var (session, _) = await EnrollAsync(client, manager);
        var bearer = session.AccessToken!;
        var org = manager.OrganizationId;
        var ownerId = await CreatePortfolioOwnerAsync(client, org, bearer, "property-race@example.test");
        var otherOwnerId = await CreatePortfolioOwnerAsync(client, org, bearer, "property-other@example.test");
        using var catalog = await Send(client, HttpMethod.Get, Portfolio(org, "properties/types"), bearer);
        var type = (await Read<List<Option>>(catalog))[0];
        var id = await CreatePropertyAsync(client, org, bearer, ownerId, type.Id, "Before edit", "Fes");
        var path = Portfolio(org, $"properties/{id}");

        async Task<PropertyView> ReadPropertyAsync()
        {
            using var response = await Send(client, HttpMethod.Get, path, bearer);
            return await Read<PropertyView>(response);
        }

        var original = await ReadPropertyAsync();
        var edits = await Task.WhenAll(
            Send(client, HttpMethod.Put, path, bearer, PropertyInput(ownerId, type.Id, "First edit", "Fes", original.Version)),
            Send(client, HttpMethod.Put, path, bearer, PropertyInput(ownerId, type.Id, "Second edit", "Rabat", original.Version)));
        using var first = edits[0];
        using var second = edits[1];
        Assert.Single(edits, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(edits, response => response.StatusCode == HttpStatusCode.Conflict);
        var current = await ReadPropertyAsync();
        Assert.Equal(first.IsSuccessStatusCode ? "First edit" : "Second edit", current.Summary.Name);
        using (var transfer = await Send(client, HttpMethod.Put, path, bearer,
            PropertyInput(otherOwnerId, type.Id, "Wrong owner", "Fes", current.Version)))
            Assert.Equal(HttpStatusCode.Conflict, transfer.StatusCode);
        using (var archive = await Send(client, HttpMethod.Put, path + "/status", bearer,
            new { Status = PropertyStatus.Archived, current.Version }))
            Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        var archived = await ReadPropertyAsync();
        using (var reactivate = await Send(client, HttpMethod.Put, path + "/status", bearer,
            new { Status = PropertyStatus.Active, archived.Version }))
            Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
        var reactivated = await ReadPropertyAsync();
        Assert.Equal(PropertyStatus.Active, reactivated.Summary.Status);
        Assert.Equal(OperationalStatus.Ready, reactivated.Summary.OperationalStatus);
        Assert.Equal(original.Summary.Reference, reactivated.Summary.Reference);
        Assert.Equal(ownerId, reactivated.Summary.OwnerId);
        Assert.Single(reactivated.History, entry => entry.Action == "property_updated");
        Assert.Equal(2, reactivated.History.Count(entry => entry.Action == "property_status_changed"));

        var sameTenant = await CreatePropertyAsync(client, org, bearer, ownerId, type.Id, "Another property", "Fes");
        await using var context = database.CreateContext(org);
        var duplicate = await context.Set<Property>().SingleAsync(row => row.Id == sameTenant, Token);
        duplicate.Reference = original.Summary.Reference;
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task OwnerSearchFiltersAndPaginatesWithinTheManagersOrganization()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        var foreign = await AddAccountAsync(application);
        using var client = Client(application);
        var (session, _) = await EnrollAsync(client, manager);
        var token = session.AccessToken!;
        var org = manager.OrganizationId;
        var first = await CreatePortfolioOwnerAsync(client, org, token, "a-portfolio-filter@example.test");
        var second = await CreatePortfolioOwnerAsync(client, org, token, "z-portfolio-filter@example.test");
        _ = await CreatePortfolioOwnerAsync(client, org, token, "unmatched@example.test");
        await using (var context = database.CreateContext(foreign.OrganizationId))
        {
            context.Add(new Owner
            {
                OrganizationId = foreign.OrganizationId, Reference = "FOREIGN", Name = "Foreign owner",
                Email = "x-portfolio-filter@example.test", IsActive = true
            });
            await context.SaveChangesAsync(Token);
        }
        var path = Portfolio(org, "owners?search=portfolio-filter&archived=false&sort=-email&size=1");
        using var firstResponse = await Send(client, HttpMethod.Get, path + "&page=1", token);
        var firstPage = await Read<Page<OwnerView>>(firstResponse);
        Assert.Equal(2, firstPage.Total);
        Assert.Equal(second, Assert.Single(firstPage.Items).Id);
        using var secondResponse = await Send(client, HttpMethod.Get, path + "&page=2", token);
        var secondPage = await Read<Page<OwnerView>>(secondResponse);
        Assert.Equal(2, secondPage.Total);
        Assert.Equal(first, Assert.Single(secondPage.Items).Id);
        using var detail = await Send(client, HttpMethod.Get, Portfolio(org, $"owners/{first}"), token);
        Assert.Equal(first, (await Read<OwnerPortfolio>(detail)).Owner.Id);
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

    [Fact]
    public async Task PhotosDecodeAndReorderWithOneConcurrentCoverWinner()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        var owner = await AddAccountAsync(application, organizationId: manager.OrganizationId);
        var stranger = await AddAccountAsync(application, OrganizationRole.Manager);
        var otherOwner = await AddAccountAsync(application, organizationId: manager.OrganizationId);
        var admin = await AddAccountAsync(application, superAdmin: true);
        using var client = Client(application);
        var (m, _) = await EnrollAsync(client, manager);
        var (s, _) = await EnrollAsync(client, stranger);
        var (a, _) = await EnrollAsync(client, admin);
        var (o, _) = await LoginAsync(client, owner);
        var (other, _) = await LoginAsync(client, otherOwner);
        var path = Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}");
        var url = path + "/photos";
        using var bitmap = new SKBitmap(12, 8);
        bitmap.Erase(SKColors.SteelBlue);

        using (var malformed = await Upload(client, url, m.AccessToken!, "truncated.png",
            [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 0, 0, 0, 0]))
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, malformed.StatusCode);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        var pngBytes = png.ToArray();
        using (var malformed = await Upload(client, url, m.AccessToken!, "missing-pixels.png", [..pngBytes[..33], ..pngBytes[^12..]]))
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, malformed.StatusCode);
        using (var invalid = await Upload(client, url, m.AccessToken!, "mislabeled.jpg", pngBytes))
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, invalid.StatusCode);
        using (var unsupported = await Upload(client, url, m.AccessToken!, "unsupported.svg", "<svg/>"u8.ToArray()))
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
        using (var unsupported = await Upload(client, url, m.AccessToken!, "program.exe", pngBytes))
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
        using (var denied = await Upload(client, url, o.AccessToken!, "owner.png", pngBytes))
            Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);

        var uploads = new List<FileView>();
        foreach (var (format, ext) in new[] { (SKEncodedImageFormat.Png, "png"), (SKEncodedImageFormat.Jpeg, "jpg"),
            (SKEncodedImageFormat.Webp, "webp") })
        {
            using var image = bitmap.Encode(format, 90);
            using var upload = await Upload(client, url, m.AccessToken!, $"Example.{ext}", image.ToArray());
            Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
            var file = await Read<FileView>(upload);
            Assert.Equal(manager.UserId, file.UploaderId);
            Assert.True(file.Readable);
            Assert.DoesNotContain("storageKey", await upload.Content.ReadAsStringAsync(Token), StringComparison.OrdinalIgnoreCase);
            uploads.Add(file);
        }

        var ids = uploads.Select(file => file.Id).ToArray();
        var imageUrl = url + "/" + ids[0];
        using (var download = await Send(client, HttpMethod.Get, imageUrl, o.AccessToken))
        {
            Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);
            Assert.True(download.Headers.CacheControl?.NoStore);
            Assert.True(download.Headers.CacheControl?.Private);
            Assert.Equal("nosniff", Assert.Single(download.Headers.GetValues("X-Content-Type-Options")));
            Assert.Equal(pngBytes, await download.Content.ReadAsByteArrayAsync(Token));
        }
        Assert.Equal(HttpStatusCode.OK, await Status(client, imageUrl, a.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, imageUrl, other.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, imageUrl, s.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client,
            Portfolio(stranger.OrganizationId, $"properties/{owner.PropertyId}/photos/{ids[0]}"), s.AccessToken));
        using (var denied = await Send(client, HttpMethod.Delete, imageUrl, o.AccessToken))
            Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);

        using var before = await Send(client, HttpMethod.Get, path, m.AccessToken);
        var version = (await Read<PropertyView>(before)).Version;
        var first = new[] { ids[1], ids[2], ids[0] };
        var second = new[] { ids[2], ids[0], ids[1] };
        var results = await Task.WhenAll(Send(client, HttpMethod.Put, url + "/order", m.AccessToken, new { ids = first, version }),
            Send(client, HttpMethod.Put, url + "/order", m.AccessToken, new { ids = second, version }));
        using var firstResult = results[0];
        using var secondResult = results[1];
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.Conflict);
        var winner = firstResult.IsSuccessStatusCode ? first : second;
        using var after = await Send(client, HttpMethod.Get, path, o.AccessToken);
        var property = await Read<PropertyView>(after);
        Assert.Equal(winner, property.Photos.Select(file => file.Id));
        Assert.Equal(winner[0], property.Summary.CoverPhotoId);
        using var archive = await Send(client, HttpMethod.Delete, url + "/" + winner[0], m.AccessToken);
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, url + "/" + winner[0], o.AccessToken));
        using var remaining = await Send(client, HttpMethod.Get, path, m.AccessToken);
        Assert.Equal(winner[1], (await Read<PropertyView>(remaining)).Summary.CoverPhotoId);
    }

    [Fact]
    public async Task PdfMetadataAndHistoricalAttachmentRemainAvailableThroughAuthorizedDownloads()
    {
        using var application = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        var owner = await AddAccountAsync(application, organizationId: manager.OrganizationId);
        using var client = Client(application);
        var (m, _) = await EnrollAsync(client, manager);
        var (o, _) = await LoginAsync(client, owner);
        var path = Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}");
        var url = path + "/documents";
        using var content = new MemoryStream();
        using (var pdf = SKDocument.CreatePdf(content))
        {
            pdf.BeginPage(90, 60).Clear(SKColors.Wheat);
            pdf.EndPage();
            pdf.Close();
        }
        var bytes = content.ToArray();
        using var uploaded = await Upload(client, url, m.AccessToken!, "Fictitious agreement.pdf", bytes);
        var file = await Read<FileView>(uploaded);
        Assert.Equal("Fictitious agreement.pdf", file.Name);
        Assert.Equal(manager.UserId, file.UploaderId);
        Assert.Equal(bytes.Length, file.SizeBytes);
        Assert.Equal(DateTimeKind.Utc, file.UploadedAt.Kind);
        using var listing = await Send(client, HttpMethod.Get, url, o.AccessToken);
        Assert.Equal(file.Id, Assert.Single(await Read<List<FileView>>(listing)).Id);
        var downloadUrl = url + "/" + file.Id;
        using var download = await Send(client, HttpMethod.Get, downloadUrl, o.AccessToken);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal(file.Name, download.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(bytes, await download.Content.ReadAsByteArrayAsync(Token));

        using var attached = await Send(client, HttpMethod.Post, path + "/contracts", m.AccessToken,
            new { StartDate = "2020-01-01", EndDate = "2020-12-31", DocumentId = file.Id });
        Assert.Equal(HttpStatusCode.OK, attached.StatusCode);
        using var retained = await Send(client, HttpMethod.Delete, downloadUrl, m.AccessToken);
        Assert.Equal(HttpStatusCode.Conflict, retained.StatusCode);
        Assert.Equal(HttpStatusCode.OK, await Status(client, downloadUrl, o.AccessToken));
        using var detail = await Send(client, HttpMethod.Get, path, m.AccessToken);
        var contract = Assert.Single((await Read<PropertyView>(detail)).Contracts);
        using var archiveContract = await Send(client, HttpMethod.Post, path + $"/contracts/{contract.Id}/archive",
            m.AccessToken, new { contract.Version });
        Assert.Equal(HttpStatusCode.OK, archiveContract.StatusCode);
        using var ownerDetail = await Send(client, HttpMethod.Get, path, o.AccessToken);
        var archivedContract = Assert.Single((await Read<PropertyView>(ownerDetail)).Contracts);
        Assert.Equal("archived", archivedContract.Status);
        Assert.NotNull(archivedContract.ArchivedAt);
        Assert.Equal(file.Id, archivedContract.DocumentId);
        using var retainedAfterArchive = await Send(client, HttpMethod.Delete, downloadUrl, m.AccessToken);
        Assert.Equal(HttpStatusCode.Conflict, retainedAfterArchive.StatusCode);

        using var unreferenced = await Upload(client, url, m.AccessToken!, "Unreferenced.pdf", bytes);
        var unreferencedFile = await Read<FileView>(unreferenced);
        using var archived = await Send(client, HttpMethod.Delete, url + "/" + unreferencedFile.Id, m.AccessToken);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, url + "/" + unreferencedFile.Id, o.AccessToken));
        await using var tenant = database.CreateContext(manager.OrganizationId);
        Assert.NotNull((await tenant.Set<StoredFile>().SingleAsync(row => row.OriginalName == "Unreferenced.pdf", Token)).ArchivedAt);
    }

    [Fact]
    public async Task QuarantinedPhotosStayOutOfOwnerListsAndCoverWhenPreviewIsDisabled()
    {
        using var application = new Application(database.ConnectionString, allowUnscanned: false);
        var manager = await AddAccountAsync(application, OrganizationRole.Manager);
        var owner = await AddAccountAsync(application, organizationId: manager.OrganizationId);
        using var client = Client(application);
        var (m, _) = await EnrollAsync(client, manager);
        var (o, _) = await LoginAsync(client, owner);
        var path = Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}");
        using var bitmap = new SKBitmap(4, 4);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var uploaded = await Upload(client, path + "/photos", m.AccessToken!, "Quarantined.png", png.ToArray());
        var file = await Read<FileView>(uploaded);
        Assert.False(file.Readable);
        using var read = await Send(client, HttpMethod.Get, path, o.AccessToken);
        var property = await Read<PropertyView>(read);
        Assert.Null(property.Summary.CoverPhotoId);
        Assert.Empty(property.Photos);
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, path + "/photos/" + file.Id, o.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, path + "/photos/" + file.Id, m.AccessToken));
    }

    [Fact]
    public async Task StorageFailuresAndAmbiguousFilesCanBeReconciledWithoutChangingTheCatalog()
    {
        var folder = Directory.CreateTempSubdirectory("HostGridCheckpoint53-");
        try
        {
            var root = Path.Combine(folder.FullName, "blobs");
            await File.WriteAllTextAsync(root, "Fixture blocks directory creation", Token);
            using var application = new Application(database.ConnectionString, storageRoot: root, failFileSave: true);
            var manager = await AddAccountAsync(application, OrganizationRole.Manager);
            var owner = await AddAccountAsync(application, organizationId: manager.OrganizationId);
            using var client = Client(application);
            var (m, _) = await EnrollAsync(client, manager);
            var url = Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}/documents");
            using (var failed = await Upload(client, url, m.AccessToken!, "Unavailable.pdf", "%PDF-1.7"u8.ToArray()))
            {
                Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
                Assert.DoesNotContain(folder.FullName, await failed.Content.ReadAsStringAsync(Token), StringComparison.OrdinalIgnoreCase);
            }
            File.Delete(root);
            Directory.CreateDirectory(root);
            using (var failed = await Upload(client, url, m.AccessToken!, "Save failed.pdf", "%PDF-1.7"u8.ToArray()))
                Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
            Assert.Empty(Directory.EnumerateFiles(root));
            await using var tenant = database.CreateContext(manager.OrganizationId);
            Assert.Empty(await tenant.Set<StoredFile>().ToListAsync(Token));

            var local = application.Services.GetRequiredService<IFileStorageService>();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            using var source = new MemoryStream([1, 2, 3]);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                local.StoreAsync(Guid.NewGuid().ToString("N"), source, cancellation.Token));
            Assert.Empty(Directory.EnumerateFiles(root));
            var key = Guid.NewGuid().ToString("N");
            var orphan = Path.Combine(root, key);
            await File.WriteAllTextAsync(orphan, "Fixture for an uncertain commit", Token);
            File.SetLastWriteTimeUtc(orphan, DateTime.UtcNow.AddDays(-2));
            var staging = Path.Combine(root, key + "." + Guid.NewGuid().ToString("N") + ".staging");
            await File.WriteAllTextAsync(staging, "Fixture for an interrupted write", Token);
            File.SetLastWriteTimeUtc(staging, DateTime.UtcNow.AddDays(-2));
            await using var scope = application.Services.CreateAsyncScope();
            using var result = new StringWriter();
            await scope.ServiceProvider.GetRequiredService<PrivateFileInventory>().WriteAsync(result, Token);
            Assert.Contains("orphan candidate " + key, result.ToString());
            Assert.Contains("stale staging candidate", result.ToString());
            Assert.True(File.Exists(orphan));
            Assert.True(File.Exists(staging));
        }
        finally { folder.Delete(recursive: true); }
    }

    [Fact]
    public async Task ContractsExposeActiveHistoryAndGuardScheduledEditsAndTermination()
    {
        using var app = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(app, OrganizationRole.Manager);
        var owner = await AddAccountAsync(app, organizationId: manager.OrganizationId);
        var otherOwner = await AddAccountAsync(app, organizationId: manager.OrganizationId);
        var foreign = await AddAccountAsync(app, OrganizationRole.Manager);
        var admin = await AddAccountAsync(app, superAdmin: true);
        using var client = Client(app);
        var (m, _) = await EnrollAsync(client, manager);
        var (o, _) = await LoginAsync(client, owner);
        var (b, _) = await LoginAsync(client, otherOwner);
        var (outside, _) = await EnrollAsync(client, foreign);
        var (a, _) = await EnrollAsync(client, admin);
        var path = Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}");
        var day = HostGrid.Api.Services.PropertyPortfolioService.Today;

        async Task<PropertyView> Details(string? token)
        {
            using var response = await Send(client, HttpMethod.Get, path, token);
            return await Read<PropertyView>(response);
        }
        async Task<HttpStatusCode> Write(HttpMethod method, string suffix, object body, string? accessToken)
        {
            using var response = await Send(client, method, path + suffix, accessToken, body);
            return response.StatusCode;
        }

        Assert.Null((await Details(o.AccessToken)).ActiveContract);
        Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Post, "/contracts", new { StartDate = day }, m.AccessToken));
        var active = Assert.IsType<ContractView>((await Details(o.AccessToken)).ActiveContract);
        Assert.Equal(20m, active.Rate);
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, path, b.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, path, outside.AccessToken));
        var end = new { EndDate = day.AddDays(2), active.Version };
        var endPath = $"/contracts/{active.Id}/end";
        Assert.Equal(HttpStatusCode.NotFound, await Write(HttpMethod.Put, endPath, end, o.AccessToken));
        Assert.Equal(HttpStatusCode.Conflict, await Write(HttpMethod.Put, $"/contracts/{active.Id}",
            new { StartDate = day, Rate = 22m, active.Version }, m.AccessToken));
        Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Put, endPath, end, a.AccessToken));
        Assert.Equal(day.AddDays(2), (await Details(o.AccessToken)).ActiveContract?.EndDate);

        Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Post, "/contracts",
            new { StartDate = day.AddDays(2), Rate = 22.25m }, m.AccessToken));
        var before = await Details(m.AccessToken);
        var scheduled = Assert.Single(before.Contracts, row => row.Status == "scheduled");
        var editPath = $"/contracts/{scheduled.Id}";
        Assert.Equal(HttpStatusCode.Conflict, await Write(HttpMethod.Put, editPath,
            new { StartDate = day.AddDays(1), Rate = 22m, scheduled.Version }, m.AccessToken));
        var edit = new { StartDate = day.AddDays(3), Rate = 24.75m, scheduled.Version };
        Assert.Equal(HttpStatusCode.NotFound, await Write(HttpMethod.Put, editPath, edit, b.AccessToken));
        Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Put, editPath, edit, m.AccessToken));
        Assert.Equal(HttpStatusCode.Conflict, await Write(HttpMethod.Put, editPath, edit, m.AccessToken));
        var after = await Details(m.AccessToken);
        Assert.Equal(24.75m, Assert.Single(after.Contracts, row => row.Id == scheduled.Id).Rate);
        Assert.Equal(20m, after.CurrentCommission);
        Assert.Contains(after.History, row => row.Action == "contract_updated");
        Assert.Contains(after.History, row => row.Action == "contract_ended");
    }

    [Fact]
    public async Task CommissionUsesDatedHistoryAndProtectsClosedPeriodsFromConfigurationChanges()
    {
        using var app = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(app, OrganizationRole.Manager);
        var owner = await AddAccountAsync(app, organizationId: manager.OrganizationId);
        using var client = Client(app);
        var (m, _) = await EnrollAsync(client, manager);
        var (o, _) = await LoginAsync(client, owner);
        var path = Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}/commission-rates");
        var day = HostGrid.Api.Services.PropertyPortfolioService.Today;
        var tomorrow = day.AddDays(1);

        async Task<decimal> RateOn(DateOnly date)
        {
            using var response = await Send(client, HttpMethod.Get, path + $"/effective?date={date:yyyy-MM-dd}", o.AccessToken);
            var rate = await Read<EffectiveCommissionView>(response);
            Assert.Equal(date, rate.Date);
            return rate.Rate;
        }
        Task<HttpResponseMessage> Change(DateOnly date, decimal rate, string? accessToken)
            => Send(client, HttpMethod.Post, path, accessToken, new { EffectiveFrom = date, Rate = rate });

        Assert.Equal(20m, await RateOn(day));
        await using var tenant = database.CreateContext(manager.OrganizationId);
        var guest = new Guest { OrganizationId = manager.OrganizationId, Name = "Fictitious snapshot guest" };
        tenant.Add(guest);
        await tenant.SaveChangesAsync(Token);
        var reservation = new Reservation
        {
            OrganizationId = manager.OrganizationId, PropertyId = owner.PropertyId, GuestId = guest.Id,
            Reference = "SNAPSHOT-54", Adults = 1, CheckInDate = tomorrow, CheckOutDate = tomorrow.AddDays(1),
            RevenueInputMode = RevenueInputMode.NetDirect, NetPlatformRevenue = 100m,
            CommissionRateSnapshot = 20m, CommissionAmount = 20m
        };
        tenant.Add(reservation);
        await tenant.SaveChangesAsync(Token);
        using (var retroactive = await Change(day.AddDays(-1), 19m, m.AccessToken))
            Assert.Equal(HttpStatusCode.Conflict, retroactive.StatusCode);
        using (var tooPrecise = await Change(day, 20.123m, m.AccessToken))
            Assert.Equal(HttpStatusCode.BadRequest, tooPrecise.StatusCode);
        using (var denied = await Change(day, 15m, o.AccessToken))
            Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        using (var firstRate = await Change(day, 22.25m, m.AccessToken))
            Assert.Equal(HttpStatusCode.OK, firstRate.StatusCode);
        var results = await Task.WhenAll(Change(tomorrow, 23.5m, m.AccessToken), Change(tomorrow, 24m, m.AccessToken));
        using var first = results[0];
        using var second = results[1];
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(22.25m, await RateOn(day));
        Assert.Equal(first.IsSuccessStatusCode ? 23.5m : 24m, await RateOn(tomorrow));

        var closed = tomorrow.AddMonths(2);
        var ownerId = await OwnerId(owner);
        var closure = new MonthlyClosure
        {
            OrganizationId = manager.OrganizationId, OwnerId = ownerId, PropertyId = owner.PropertyId,
            Year = (short)closed.Year, Month = (short)closed.Month, Status = ClosureStatus.Closed,
            ClosedAt = DateTime.UtcNow, ClosedById = manager.UserId, NetPlatformRevenue = 100m,
            CommissionAmount = 25m, OwnerProfit = 75m
        };
        tenant.Add(closure);
        await tenant.SaveChangesAsync(Token);
        using (var blocked = await Change(closed, 30m, m.AccessToken))
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        using (var blockedContract = await Send(client, HttpMethod.Post,
            Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}/contracts"), m.AccessToken, new { StartDate = closed }))
            Assert.Equal(HttpStatusCode.Conflict, blockedContract.StatusCode);
        var kept = await tenant.Set<MonthlyClosure>().AsNoTracking().SingleAsync(row => row.Id == closure.Id, Token);
        Assert.Equal(25m, kept.CommissionAmount);
        Assert.Equal(closure.RowVersion, kept.RowVersion);
        var snapshot = await tenant.Set<Reservation>().AsNoTracking().SingleAsync(row => row.Id == reservation.Id, Token);
        Assert.Equal(20m, snapshot.CommissionRateSnapshot);
        Assert.Equal(20m, snapshot.CommissionAmount);
        Assert.Equal(reservation.RowVersion, snapshot.RowVersion);
        Assert.Equal(2, await tenant.Set<CommissionRateHistory>().CountAsync(row => row.PropertyId == owner.PropertyId, Token));
    }

    [Fact]
    public async Task AmenitiesInventoryAndManualListingsRoundTripWithScopedAccess()
    {
        using var app = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(app, OrganizationRole.Manager);
        var owner = await AddAccountAsync(app, organizationId: manager.OrganizationId);
        var other = await AddAccountAsync(app, organizationId: manager.OrganizationId);
        var foreign = await AddAccountAsync(app, OrganizationRole.Manager);
        using var client = Client(app);
        var (m, _) = await EnrollAsync(client, manager);
        var (o, _) = await LoginAsync(client, owner);
        var (b, _) = await LoginAsync(client, other);
        var (f, _) = await EnrollAsync(client, foreign);
        var org = manager.OrganizationId;
        var path = Portfolio(org, $"properties/{owner.PropertyId}");
        async Task<PropertyView> ReadProperty()
        {
            using var response = await Send(client, HttpMethod.Get, path, o.AccessToken);
            return await Read<PropertyView>(response);
        }
        async Task<HttpStatusCode> Write(HttpMethod method, string suffix, object payload, string? accessToken)
        {
            using var response = await Send(client, method, path + suffix, accessToken, payload);
            return response.StatusCode;
        }
        using var catalog = await Send(client, HttpMethod.Get, Portfolio(org, "properties/amenities"), m.AccessToken);
        var amenities = await Read<List<Option>>(catalog);
        Assert.Equal(9, amenities.Select(row => row.Code).Distinct().Count());
        var version = (await ReadProperty()).Version;
        var id = amenities[0].Id;
        Assert.Equal(HttpStatusCode.BadRequest, await Write(HttpMethod.Put, "/amenities", new { Ids = new[] { id, id }, version }, m.AccessToken));
        Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Put, "/amenities", new { Ids = new[] { id }, version }, m.AccessToken));
        var selected = await ReadProperty();
        Assert.Equal(id, Assert.Single(selected.Amenities));
        Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Put, "/amenities", new { Ids = Array.Empty<Guid>(), selected.Version }, m.AccessToken));
        Assert.Empty((await ReadProperty()).Amenities);

        var item = new { Name = "Coffee maker", Quantity = 2, Condition = "Good", Notes = "Spare filter in cabinet" };
        Assert.Equal(HttpStatusCode.NotFound, await Write(HttpMethod.Post, "/inventory", item, o.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Write(HttpMethod.Post, "/inventory", item, f.AccessToken));
        Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Post, "/inventory", item, m.AccessToken));
        var savedItem = Assert.Single((await ReadProperty()).Inventory);
        Assert.Equal(item.Notes, savedItem.Notes);
        Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Put, $"/inventory/{savedItem.Id}",
            new { item.Name, Quantity = 3, Condition = "New", item.Notes, savedItem.Version }, m.AccessToken));
        var edited = Assert.Single((await ReadProperty()).Inventory);
        Assert.Equal(3, edited.Quantity);
        Assert.Equal(HttpStatusCode.Conflict, await Write(HttpMethod.Delete, $"/inventory/{savedItem.Id}", new { savedItem.Version }, m.AccessToken));
        Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Delete, $"/inventory/{edited.Id}", new { edited.Version }, m.AccessToken));

        Assert.Equal(HttpStatusCode.BadRequest, await Write(HttpMethod.Put, "/listings/Airbnb", new { Url = "javascript:void(0)" }, m.AccessToken));
        Assert.Equal(HttpStatusCode.BadRequest, await Write(HttpMethod.Put, "/listings/BookingCom", new { Url = "https://user@example.test/" }, m.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Write(HttpMethod.Put, "/listings/Direct", new { ExternalReference = "forbidden" }, b.AccessToken));
        foreach (var source in new[] { "Airbnb", "BookingCom", "Direct" })
            Assert.Equal(HttpStatusCode.OK, await Write(HttpMethod.Put, "/listings/" + source, new { ExternalReference = source + "-fixture" }, m.AccessToken));
        Assert.Equal(HttpStatusCode.Conflict, await Write(HttpMethod.Put, "/listings/Direct", new { ExternalReference = "duplicate" }, m.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, path, b.AccessToken));
        var result = await ReadProperty();
        Assert.Empty(result.Inventory);
        Assert.Equal(3, result.Listings.Count);
    }

    [Fact]
    public async Task PricingPreviewReturnsEverySourceAndPreservesStoredReservationPrices()
    {
        using var app = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(app, OrganizationRole.Manager);
        var owner = await AddAccountAsync(app, organizationId: manager.OrganizationId);
        var other = await AddAccountAsync(app, organizationId: manager.OrganizationId);
        using var client = Client(app);
        var (m, _) = await EnrollAsync(client, manager);
        var (o, _) = await LoginAsync(client, owner);
        var (b, _) = await LoginAsync(client, other);
        var path = Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}");
        var Friday = HostGrid.Api.Services.PropertyPortfolioService.Today.AddDays(7);
        while (Friday.DayOfWeek != DayOfWeek.Friday) Friday = Friday.AddDays(1);
        var end = Friday.AddDays(6);
        var previewPath = path + $"/pricing-preview?start={Friday:yyyy-MM-dd}&end={end:yyyy-MM-dd}";
        await using var db = database.CreateContext(manager.OrganizationId);
        var guest = new Guest { OrganizationId = manager.OrganizationId, Name = "Pricing fixture" };
        db.Add(guest);
        await db.SaveChangesAsync(Token);
        var reservation = new Reservation { OrganizationId = manager.OrganizationId, PropertyId = owner.PropertyId,
            GuestId = guest.Id, Reference = "PRICE-55", Adults = 1, CheckInDate = Friday, CheckOutDate = Friday.AddDays(1),
            RevenueInputMode = RevenueInputMode.NetDirect, NetPlatformRevenue = 400m, CommissionRateSnapshot = 20m, CommissionAmount = 80m };
        db.Add(reservation);
        await db.SaveChangesAsync(Token);
        using var details = await Send(client, HttpMethod.Get, path, m.AccessToken);
        var version = (await Read<PropertyView>(details)).Version;
        using var baseRate = await Send(client, HttpMethod.Put, path + "/base-rate", m.AccessToken, new { NightlyRate = 500.25m, version });
        Assert.Equal(HttpStatusCode.OK, baseRate.StatusCode);
        using var forbidden = await Send(client, HttpMethod.Put, path + "/base-rate", o.AccessToken, new { NightlyRate = 500m, version });
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
        Task<HttpResponseMessage> Rule(short priority, DateOnly from, DateOnly to, short mask, decimal amount)
            => Send(client, HttpMethod.Post, path + "/seasonal-rates", m.AccessToken,
                new { StartDate = from, EndDate = to, DaysOfWeek = mask, NightlyRate = amount, Priority = priority });
        using var weekend = await Rule(1, Friday, end, 65, 600m);
        using var season = await Rule(2, Friday.AddDays(2), Friday.AddDays(4), 127, 700m);
        Assert.Equal(HttpStatusCode.OK, weekend.StatusCode);
        Assert.Equal(HttpStatusCode.OK, season.StatusCode);
        var overlapping = await Task.WhenAll(Rule(3, Friday.AddDays(3), Friday.AddDays(4), 127, 800m),
            Rule(3, Friday.AddDays(3), Friday.AddDays(4), 127, 900m));
        using var first = overlapping[0];
        using var second = overlapping[1];
        Assert.Single(overlapping, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(overlapping, response => response.StatusCode == HttpStatusCode.Conflict);
        using var quote = await Send(client, HttpMethod.Get, previewPath, o.AccessToken);
        var nights = await Read<List<NightlyQuote>>(quote);
        Assert.Equal(new[] { "base", "weekend", "seasonal", "custom", "base", "base" }, nights.Select(row => row.Source));
        Assert.Equal(end.AddDays(-1), nights[^1].Night);
        Assert.Equal(500.25m, nights[0].Amount);
        Assert.Equal(first.IsSuccessStatusCode ? 800m : 900m, nights[3].Amount);
        Assert.All(nights, row => Assert.Equal("MAD", row.Currency));
        Assert.Null(nights[0].RuleId);
        Assert.NotNull(nights[3].RuleId);
        using var single = await Send(client, HttpMethod.Get, path + $"/nightly-rate?night={nights[3].Night:yyyy-MM-dd}", o.AccessToken);
        Assert.Equal(nights[3], await Read<NightlyQuote>(single));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, previewPath, b.AccessToken));
        Assert.Equal(HttpStatusCode.BadRequest, await Status(client, path + $"/pricing-preview?start={Friday:yyyy-MM-dd}&end={Friday:yyyy-MM-dd}", m.AccessToken));
        var persisted = await db.Set<Reservation>().AsNoTracking().SingleAsync(row => row.Id == reservation.Id, Token);
        Assert.Equal(400m, persisted.NetPlatformRevenue);
        Assert.Equal(reservation.RowVersion, persisted.RowVersion);
    }

    [Fact]
    public async Task OwnerReadsScopedPropertyFilesWithoutUploaderIdentityAndCannotWritePortfolioResources()
    {
        using var app = new Application(database.ConnectionString);
        var manager = await AddAccountAsync(app, OrganizationRole.Manager);
        var owner = await AddAccountAsync(app, organizationId: manager.OrganizationId);
        var other = await AddAccountAsync(app, organizationId: manager.OrganizationId);
        var foreign = await AddAccountAsync(app);
        using var client = Client(app);
        var (m, _) = await EnrollAsync(client, manager);
        var (o, _) = await LoginAsync(client, owner);
        var path = Portfolio(manager.OrganizationId, $"properties/{owner.PropertyId}");
        var otherPath = Portfolio(manager.OrganizationId, $"properties/{other.PropertyId}");
        using var sourcePdf = new MemoryStream();
        using (var pdf = SKDocument.CreatePdf(sourcePdf))
        {
            pdf.BeginPage(90, 60).Clear(SKColors.Wheat);
            pdf.EndPage();
            pdf.Close();
        }
        using var document = await Upload(client, path + "/documents", m.AccessToken!, "Owner evidence.pdf", sourcePdf.ToArray());
        var file = await Read<FileView>(document);
        Assert.NotNull(file.UploaderId);
        using var metadata = await Send(client, HttpMethod.Get, path + "/documents", o.AccessToken);
        Assert.DoesNotContain("uploaderId", await metadata.Content.ReadAsStringAsync(Token), StringComparison.OrdinalIgnoreCase);
        using var property = await Send(client, HttpMethod.Get, path, o.AccessToken);
        var detail = await Read<PropertyView>(property);
        Assert.Null(Assert.Single(detail.Documents).UploaderId);
        Assert.Empty(detail.History);
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, otherPath, o.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, otherPath + "/documents/" + file.Id, o.AccessToken));
        var foreignPath = Portfolio(foreign.OrganizationId, $"properties/{foreign.PropertyId}");
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, foreignPath + "/documents/" + file.Id, o.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, "/api/v1/admin/organizations", o.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, Portfolio(manager.OrganizationId, "accounts"), o.AccessToken));

        var day = detail.BusinessDate.AddDays(1);
        (HttpMethod Method, string Resource, object Body)[] writes =
        [
            (HttpMethod.Put, "/status", new { Status = PropertyStatus.Archived, detail.Version }),
            (HttpMethod.Post, "/contracts", new { StartDate = day }),
            (HttpMethod.Post, "/commission-rates", new { EffectiveFrom = day, Rate = 25 }),
            (HttpMethod.Put, "/amenities", new { Ids = Array.Empty<Guid>(), detail.Version }),
            (HttpMethod.Put, "/base-rate", new { NightlyRate = 500, detail.Version }),
            (HttpMethod.Post, "/seasonal-rates", new { StartDate = day, EndDate = day.AddDays(2), DaysOfWeek = 127, NightlyRate = 600, Priority = 3 }),
            (HttpMethod.Put, "/listings/Direct", new { ExternalReference = "forbidden" }),
            (HttpMethod.Delete, "/documents/" + file.Id, new { })
        ];
        foreach (var (method, resource, body) in writes)
        {
            using var denied = await Send(client, method, path + resource, o.AccessToken, body);
            Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        }
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

    private sealed class Application(string connection, bool expiredLinks = false, bool allowUnscanned = true,
        string? storageRoot = null, bool failFileSave = false, IInterceptor[]? bookingInterceptors = null,
        int? availabilityLockTimeout = null, bool retryAvailability = false) : WebApplicationFactory<Program>
    {
        public byte[] Key { get; } = RandomNumberGenerator.GetBytes(32);
        public TestEmail Email { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:HostGrid"] = connection,
                    ["Security:SigningKey"] = Convert.ToBase64String(Key),
                    ["PropertyStorage:Root"] = storageRoot,
                    ["PropertyStorage:AllowUnscannedInDevelopment"] = allowUnscanned.ToString()
                };
                if (availabilityLockTimeout is int milliseconds)
                    settings["Availability:LockTimeoutMilliseconds"] = milliseconds.ToString();
                config.AddInMemoryCollection(settings);
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ISecurityEmail>(Email);
                if (failFileSave)
                    services.ConfigureDbContext<HostGrid.Api.Data.HostGridDbContext>(options => options.AddInterceptors(new RejectFileSave()));
                if (bookingInterceptors is not null)
                    services.ConfigureDbContext<HostGrid.Api.Data.HostGridDbContext>(options => options.AddInterceptors(bookingInterceptors));
                if (retryAvailability)
                    services.ConfigureDbContext<HostGrid.Api.Data.HostGridDbContext>(options => options.UseSqlServer(connection,
                        sql => sql.EnableRetryOnFailure(1, TimeSpan.FromSeconds(1), [51005])));
                if (expiredLinks)
                    services.PostConfigure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromSeconds(-1));
            });
        }
    }

    private sealed class RejectFileSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<StoredFile>().Any(entry => entry.State == EntityState.Added))
                throw new DbUpdateException("Simulated metadata write failure.");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
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
