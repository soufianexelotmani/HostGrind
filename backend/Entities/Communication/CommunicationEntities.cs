using HostGrid.Api.Enums;
using HostGrid.Api.Entities.Organization;

namespace HostGrid.Api.Entities.Communication;

public sealed class OwnerRequest : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }
    public OwnerRequestType Type { get; set; }
    public string Description { get; set; } = null!;
    public OwnerRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class OwnerRequestMessage : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RequestId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public sealed class OwnerRequestStatusHistory : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RequestId { get; set; }
    public Guid ActorId { get; set; }
    public OwnerRequestStatus OldStatus { get; set; }
    public OwnerRequestStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
}

public sealed class Notification : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!;
    public string MessageKey { get; set; } = null!;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class NotificationPreference : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class EmailOutbox : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Recipient { get; set; } = null!;
    public string Template { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public DeliveryStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTime? SentAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
