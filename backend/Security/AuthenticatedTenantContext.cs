using HostGrid.Api.DTOs.Auth;

namespace HostGrid.Api.Security;

public sealed class AuthenticatedTenantContext : ITenantContext
{
    public Guid? AuthorizedOrganizationId { get; private set; }
    public MembershipView? Membership { get; private set; }

    internal void Authorize(Guid organizationId, MembershipView? membership)
    {
        if (organizationId == Guid.Empty || AuthorizedOrganizationId is not null)
        {
            throw new InvalidOperationException("Tenant authorization must select exactly one organization.");
        }

        AuthorizedOrganizationId = organizationId;
        Membership = membership;
    }
}
