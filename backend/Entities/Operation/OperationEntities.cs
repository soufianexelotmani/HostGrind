using HostGrid.Api.Enums;
using HostGrid.Api.Entities.Organization;

namespace HostGrid.Api.Entities.Operation;

public sealed class CleaningTask : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public bool IsAutomatic { get; set; }
    public Guid? ContactId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public CleaningStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class ChecklistTemplate : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = null!;
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class ChecklistTemplateItem : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TemplateId { get; set; }
    public int SortOrder { get; set; }
    public string Label { get; set; } = null!;
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class CleaningChecklistItem : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CleaningTaskId { get; set; }
    public int SortOrder { get; set; }
    public string Label { get; set; } = null!;
    public bool IsChecked { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class MaintenanceTicket : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public Guid? ContactId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class ExternalContact : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Specialty { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class WorkTask : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public string Title { get; set; } = null!;
    public WorkTaskType Type { get; set; }
    public Guid? CleaningTaskId { get; set; }
    public Guid? MaintenanceTicketId { get; set; }
    public WorkTaskStatus Status { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? ContactId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class OperationAttachment : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid WorkTaskId { get; set; }
    public Guid FileId { get; set; }
}

public sealed class OperationExpenseLink : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid WorkTaskId { get; set; }
    public Guid ExpenseId { get; set; }
}
