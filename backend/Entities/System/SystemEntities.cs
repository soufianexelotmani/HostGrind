using HostGrid.Api.Enums;
using HostGrid.Api.Entities.Organization;

namespace HostGrid.Api.Entities.System;

public sealed class AuditLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public Guid? ResourceId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string CorrelationId { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
}

public sealed class BackgroundJobExecution : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string JobKey { get; set; } = null!;
    public DeliveryStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
