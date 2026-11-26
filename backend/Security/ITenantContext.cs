namespace HostGrid.Api.Security;

public interface ITenantContext
{
    Guid? AuthorizedOrganizationId { get; }
}

internal sealed class UnresolvedTenantContext : ITenantContext
{
    public Guid? AuthorizedOrganizationId => null;
}
