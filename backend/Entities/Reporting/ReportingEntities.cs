using HostGrid.Api.Enums;
using HostGrid.Api.Entities.Organization;

namespace HostGrid.Api.Entities.Reporting;

public sealed class Report : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? PropertyId { get; set; }
    public ReportType Type { get; set; }
    public string FiltersJson { get; set; } = null!;
    public Guid? FileId { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class DashboardPreference : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string LayoutJson { get; set; } = null!;
    public byte[] RowVersion { get; set; } = null!;
}
