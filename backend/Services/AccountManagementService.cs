using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class AccountManagementService(UserManager<ApplicationUser> users,
    AuthenticationRepository repository, AuthenticatedTenantContext tenant, AccountSecurityService accountSecurity,
    SecurityAuditService audit, TimeProvider clock)
{
    private HostGrid.Api.Data.HostGridDbContext Database => repository.Database;
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    internal async Task<IReadOnlyList<OrganizationView>> ListOrganizationsAsync(int page, CancellationToken token)
    {
        await audit.RecordAsync("admin_organizations_read", token);
        return await Database.Set<Organization>().IgnoreQueryFilters().AsNoTracking()
            .OrderBy(row => row.Id).Skip(Offset(page)).Take(50)
            .Select(row => new OrganizationView(row.Id, row.Name, row.Reference, row.IsActive)).ToListAsync(token);
    }

    internal async Task<IReadOnlyList<SecurityEventView>> ListAuditAsync(int page, CancellationToken token)
    {
        var records = await Database.Set<SecurityEvent>().AsNoTracking().OrderByDescending(row => row.OccurredAt)
            .ThenByDescending(row => row.Id).Skip(Offset(page)).Take(50)
            .Select(row => new SecurityEventView(row.Action, row.OccurredAt, row.ActorId, row.SubjectId,
                row.OrganizationId, row.CorrelationId)).ToListAsync(token);
        await audit.RecordAsync("admin_security_events_read", token);
        return records;
    }

    internal async Task<IReadOnlyList<AccountMembershipView>> ListMembershipsAsync(int page, bool superAdmin, CancellationToken token)
    {
        RequireTenant();
        return await (from membership in Database.Set<OrganizationMember>().AsNoTracking()
                      join user in Database.Users.AsNoTracking() on membership.UserId equals user.Id
                      where superAdmin || membership.Role == OrganizationRole.Owner
                      orderby membership.Id
                      select new AccountMembershipView(user.Id, user.Email!, membership.Role.ToString(),
                          membership.IsActive, user.IsActive)).Skip(Offset(page)).Take(50).ToListAsync(token);
    }

    internal Task<Property?> FindPropertyAsync(Guid id, CancellationToken token)
        => Database.Set<Property>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, token);

    private static int Offset(int page) => (Math.Clamp(page, 1, 100000) - 1) * 50;

    internal async Task<Guid> CreateOrganizationAsync(OrganizationRequest request, CancellationToken token)
    {
        var id = Guid.NewGuid();
        tenant.Authorize(id, null);
        await using var transaction = await repository.LockAsync("organizations", token);
        if (await Database.Set<Organization>().IgnoreQueryFilters().AnyAsync(row => row.Reference == request.Reference, token))
        {
            throw new SecurityRequestException(409, "This organization reference is already in use.");
        }
        Database.Add(new Organization { Id = id, Reference = request.Reference, Name = request.Name, IsActive = true });
        Database.Add(new OrganizationSettings { OrganizationId = id, Language = "fr" });
        Database.AddRange(HostGrid.Api.Data.Seed.ReferenceData.MissingExpenseCategories(id, []));
        await audit.RecordAsync("organization_created", token);
        await transaction.CommitAsync(token);
        return id;
    }

    internal async Task<Guid> InviteAsync(InviteRequest request, bool superAdmin, CancellationToken token, Guid? ownerProfileId = null)
    {
        var organizationId = RequireTenant();
        var role = SelectRole(request.Role, superAdmin);
        await using var transaction = await repository.LockAsync("invite:" + TokenService.Hash(request.Email.ToUpperInvariant()), token);
        var profile = ownerProfileId is null ? null : await Database.Set<Owner>().SingleOrDefaultAsync(row =>
            row.Id == ownerProfileId && !row.IsArchived && row.Email == request.Email, token)
            ?? throw new SecurityRequestException(404, "An eligible owner profile was not found.");
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null)
        {
            if (profile?.UserId is not null)
                throw new SecurityRequestException(409, "The profile already has an account.");
            user = new ApplicationUser { UserName = request.Email, Email = request.Email, LockoutEnabled = true };
            AuthenticationService.RequireSuccess(await users.CreateAsync(user));
            Database.Add(new OrganizationMember { OrganizationId = organizationId, UserId = user.Id, Role = role, IsActive = true });
            if (role == OrganizationRole.Owner)
            {
                if (profile is null)
                    Database.Add(new Owner { OrganizationId = organizationId, UserId = user.Id, Email = request.Email,
                        Reference = user.Id.ToString("N"), Name = request.Name, IsActive = true });
                else
                    profile.UserId = user.Id;
            }
            await audit.RecordAsync("account_created", token, user.Id);
        }
        else
        {
            await repository.LockResourceAsync(user.Id.ToString("N"), token);
            await Database.Entry(user).ReloadAsync(token);
            if (profile is not null) await Database.Entry(profile).ReloadAsync(token);
            var pending = !user.EmailConfirmed && user.ActivatedAt == null &&
                await Database.Set<OrganizationMember>().AnyAsync(row =>
                    row.UserId == user.Id && row.IsActive && row.Role == role, token);
            if (!pending || profile is not null && (profile.IsArchived || profile.UserId != user.Id ||
                !string.Equals(profile.Email, user.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new SecurityRequestException(409, "This invitation could not be created.");
            }
            AuthenticationService.RequireSuccess(await users.UpdateSecurityStampAsync(user));
        }
        await transaction.CommitAsync(token);
        await accountSecurity.SendInvitationAsync(user, token);
        return user.Id;
    }

    internal async Task ChangeMembershipAsync(Guid userId, MembershipChangeRequest request, bool superAdmin,
        CancellationToken token, Guid? ownerProfileId = null)
    {
        var organizationId = RequireTenant();
        var role = SelectRole(request.Role, superAdmin);
        await using var transaction = await repository.LockAsync(userId.ToString("N"), token);
        var member = await Database.Set<OrganizationMember>().SingleOrDefaultAsync(row => row.UserId == userId, token);
        var user = await users.FindByIdAsync(userId.ToString());
        if (member is null || user is null || (!superAdmin && member.Role != OrganizationRole.Owner)
            || await users.IsInRoleAsync(user, CurrentUserService.SuperAdmin))
        {
            throw new SecurityRequestException(404, "Account membership was not found.");
        }
        var owner = await Database.Set<Owner>().SingleOrDefaultAsync(row => row.UserId == userId, token);
        if (ownerProfileId.HasValue)
        {
            if (member.Role != OrganizationRole.Owner || owner?.Id != ownerProfileId)
                throw new SecurityRequestException(404, "Owner account membership was not found.");
            if (request.Active && (owner!.IsArchived || user.EmailConfirmed && !user.IsActive))
                throw new SecurityRequestException(409, "This account cannot be reactivated in this organization.");
        }
        if (role == OrganizationRole.Owner && owner is null)
        {
            throw new SecurityRequestException(409, "The account requires an Owner profile before assigning this role.");
        }
        member.IsActive = request.Active;
        member.Role = role;
        if (owner is not null)
        {
            owner.IsActive = !owner.IsArchived && request.Active && role == OrganizationRole.Owner;
        }
        AuthenticationService.RequireSuccess(await users.UpdateSecurityStampAsync(user));
        await repository.RevokeAsync(userId, Now, token);
        await audit.RecordAsync(request.Active ? "membership_changed" : "membership_disabled", token, userId);
        await transaction.CommitAsync(token);
    }

    internal async Task AssignManagerAsync(Guid userId, CancellationToken token)
    {
        var organizationId = RequireTenant();
        await using var transaction = await repository.LockAsync(userId.ToString("N"), token);
        var user = await users.FindByIdAsync(userId.ToString());
        if (!CurrentUserService.IsEnabled(user) || await users.IsInRoleAsync(user!, CurrentUserService.SuperAdmin))
        {
            throw new SecurityRequestException(404, "An eligible account was not found.");
        }
        if (await Database.Set<OrganizationMember>().AnyAsync(row => row.UserId == userId, token))
        {
            throw new SecurityRequestException(409, "This account already has a membership in the organization.");
        }
        Database.Add(new OrganizationMember
        {
            OrganizationId = organizationId, UserId = userId, Role = OrganizationRole.Manager, IsActive = true
        });
        AuthenticationService.RequireSuccess(await users.UpdateSecurityStampAsync(user!));
        await repository.RevokeAsync(userId, Now, token);
        await audit.RecordAsync("manager_assigned", token, userId);
        await transaction.CommitAsync(token);
    }

    internal async Task DisableAsync(Guid userId, Guid actorId, CancellationToken token)
    {
        if (userId == actorId)
        {
            throw new SecurityRequestException(400, "You cannot disable your own account.");
        }
        await using var transaction = await repository.LockAsync(userId.ToString("N"), token);
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new SecurityRequestException(404, "Account was not found.");
        }
        user.IsActive = false;
        AuthenticationService.RequireSuccess(await users.UpdateSecurityStampAsync(user));
        await repository.RevokeAsync(userId, Now, token);
        await audit.RecordAsync("account_disabled", token, userId);
        await transaction.CommitAsync(token);
    }

    internal async Task BootstrapAsync(string address, CancellationToken token)
    {
        await using var transaction = await repository.LockAsync("bootstrap", token);
        var admins = await users.GetUsersInRoleAsync(CurrentUserService.SuperAdmin);
        if (admins.Count > 1 || admins.Count == 1 &&
            (!string.Equals(admins[0].Email, address, StringComparison.OrdinalIgnoreCase) || admins[0].EmailConfirmed))
        {
            throw new SecurityRequestException(409, "A platform administrator already exists.");
        }
        var user = admins.SingleOrDefault();
        if (user is null)
        {
            if (await users.FindByEmailAsync(address) is not null)
            {
                throw new SecurityRequestException(409, "This email is already in use.");
            }
            user = new ApplicationUser { UserName = address, Email = address, LockoutEnabled = true };
            AuthenticationService.RequireSuccess(await users.CreateAsync(user));
            AuthenticationService.RequireSuccess(await users.AddToRoleAsync(user, CurrentUserService.SuperAdmin));
            await audit.RecordAsync("super_admin_bootstrapped", token, user.Id);
        }
        await transaction.CommitAsync(token);
        await accountSecurity.SendInvitationAsync(user, token);
    }

    private Guid RequireTenant() => tenant.AuthorizedOrganizationId ??
        throw new SecurityRequestException(403, "Organization access is required.");

    private static OrganizationRole SelectRole(string role, bool superAdmin) => role switch
    {
        CurrentUserService.Owner => OrganizationRole.Owner,
        CurrentUserService.Manager when superAdmin => OrganizationRole.Manager,
        _ => throw new SecurityRequestException(403, "This role cannot be assigned.")
    };
}
