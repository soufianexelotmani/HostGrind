using HostGrid.Api.Enums;
using HostGrid.Api.Entities.Organization;

namespace HostGrid.Api.Entities.Expense;

public sealed class ExpenseCategory : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class Expense : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid CategoryId { get; set; }
    public string Reference { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? RecurringTemplateId { get; set; }
    public DateOnly? OccurrenceDate { get; set; }
    public ExpenseStatus Status { get; set; }
    public Guid ActorId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class ExpensePayment : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ExpenseId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public ExpenseFundingSource Payer { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
}

public sealed class ExpenseAttachment : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid FileId { get; set; }
}

public sealed class RecurringExpenseTemplate : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public ExpenseCadence Cadence { get; set; }
    public DateOnly NextDueDate { get; set; }
    public bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class ExpenseReimbursement : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ExpensePaymentId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public SettlementParty Beneficiary { get; set; }
    public string? Reference { get; set; }
    public string IdempotencyKey { get; set; } = null!;
}
