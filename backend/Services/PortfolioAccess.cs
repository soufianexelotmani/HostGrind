using HostGrid.Api.Entities.Property;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class PortfolioAccess(PortfolioRepository repository, AuthenticatedTenantContext tenant,
    IAuthorizationService authorization, IHttpContextAccessor context)
{
    internal Guid OrganizationId => tenant.AuthorizedOrganizationId ?? throw new SecurityRequestException(403, "Organization required.");
    internal System.Security.Claims.ClaimsPrincipal User => context.HttpContext!.User;
    internal Guid Actor => CurrentUserService.UserId(User);
    internal Guid? OwnerId => tenant.Membership?.OwnerId;
    internal bool CanManage => User.IsInRole(CurrentUserService.SuperAdmin) || tenant.Membership?.Role == CurrentUserService.Manager;

    internal IQueryable<Property> VisibleProperties()
    {
        _ = OrganizationId;
        return CanManage ? repository.Properties : repository.Properties.Where(row => row.OwnerId == OwnerId && OwnerId != null);
    }

    internal async Task<Property> PropertyAsync(Guid id, bool write, CancellationToken token)
    {
        var property = await repository.Properties.SingleOrDefaultAsync(row => row.Id == id, token);
        if (property is null || !(await authorization.AuthorizeAsync(User, property, new PropertyAccess(write))).Succeeded)
            throw new SecurityRequestException(404, "Property not found.");
        return property;
    }

    internal void EnsureManager()
    {
        _ = OrganizationId;
        if (!CanManage) throw new SecurityRequestException(403, "Property management permission required.");
    }
}
