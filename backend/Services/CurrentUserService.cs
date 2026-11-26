using System.Security.Claims;
using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Enums;
using HostGrid.Api.Mappers;
using HostGrid.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class CurrentUserService(UserManager<ApplicationUser> users, AuthenticationRepository repository)
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Manager = "Manager";
    public const string Owner = "Owner";

    internal static Guid UserId(ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    internal async Task<CurrentUserView?> GetAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var user = await users.FindByIdAsync(UserId(principal).ToString());
        return IsEnabled(user) ? await DescribeAsync(user!, token) : null;
    }

    internal async Task<CurrentUserView> DescribeAsync(ApplicationUser user, CancellationToken token)
    {
        var superAdmin = await users.IsInRoleAsync(user, SuperAdmin);
        var memberships = await repository.ActiveMemberships(user.Id)
            .Select(row => new { row.OrganizationId, row.Role }).ToListAsync(token);
        var owners = await repository.ActiveOwners(user.Id)
            .ToDictionaryAsync(row => row.OrganizationId, row => row.Id, token);
        var scopes = memberships.Where(row => row.Role == OrganizationRole.Manager || owners.ContainsKey(row.OrganizationId))
            .Select(row => new MembershipView(row.OrganizationId, row.Role.ToString(),
                row.Role == OrganizationRole.Owner ? owners[row.OrganizationId] : null)).ToArray();
        return CurrentUserMapper.ToView(user, superAdmin, scopes);
    }

    internal static bool IsEnabled(ApplicationUser? user) => user is { IsActive: true, EmailConfirmed: true };
    internal static bool RequiresMfa(CurrentUserView user) => user.SuperAdmin || user.MfaEnabled ||
        user.Memberships.Any(scope => scope.Role == Manager);
    internal static bool HasAccess(CurrentUserView user) => user.SuperAdmin || user.Memberships.Count > 0;
}
