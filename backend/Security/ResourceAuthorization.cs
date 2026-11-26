using HostGrid.Api.Entities.Property;
using HostGrid.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace HostGrid.Api.Security;

public static class SecurityPolicies
{
    public const string GlobalAdmin = "GlobalAdmin";
    public const string TenantManager = "TenantManager";
}

public sealed record PropertyAccess(bool Write) : IAuthorizationRequirement;

internal sealed class PropertyAccessHandler(AuthenticatedTenantContext tenant)
    : AuthorizationHandler<PropertyAccess, Property>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PropertyAccess requirement, Property resource)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            tenant.AuthorizedOrganizationId == resource.OrganizationId &&
            (context.User.IsInRole(CurrentUserService.SuperAdmin) ||
             tenant.Membership?.Role == CurrentUserService.Manager ||
             !requirement.Write && tenant.Membership?.OwnerId == resource.OwnerId))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
