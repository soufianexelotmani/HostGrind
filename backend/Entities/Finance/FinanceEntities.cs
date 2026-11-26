using HostGrid.Api.Enums;
using HostGrid.Api.Entities.Organization;

namespace HostGrid.Api.Entities.Finance;

public sealed class MonthlyClosure : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid OwnerId { get; set; }
    public short Year { get; set; }
    public short Month { get; set; }
    public decimal? GrossAmount { get; set; }
    public decimal? PlatformFees { get; set; }
    public decimal NetPlatformRevenue { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal OwnerExpenses { get; set; }
    public decimal OwnerProfit { get; set; }
    public decimal Advances { get; set; }
    public decimal OwnerDirectPayments { get; set; }
    public decimal OpeningSettlementBalance { get; set; }
    public decimal PayoutDueAtClosing { get; set; }
    public ClosureStatus Status { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedById { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class MonthlyClosureLine : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ClosureId { get; set; }
    public FinanceSource SourceType { get; set; }
    public Guid SourceId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public decimal Amount { get; set; }
    public string? SnapshotJson { get; set; }
}

public sealed class CommissionAdjustment : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public FinanceSource SourceType { get; set; }
    public Guid SourceId { get; set; }
    public DateOnly OriginPeriod { get; set; }
    public DateOnly TargetPeriod { get; set; }
    public string Reason { get; set; } = null!;
    public decimal Amount { get; set; }
    public Guid ActorId { get; set; }
}

public sealed class FinancialAdjustment : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public FinanceSource SourceType { get; set; }
    public Guid SourceId { get; set; }
    public DateOnly OriginPeriod { get; set; }
    public DateOnly TargetPeriod { get; set; }
    public string Reason { get; set; } = null!;
    public decimal Amount { get; set; }
    public Guid ActorId { get; set; }
}

public sealed class OwnerPayout : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OwnerId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly? PaidOn { get; set; }
    public PaymentMethod Method { get; set; }
    public PayoutStatus Status { get; set; }
    public string Reference { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class OwnerPayoutAllocation : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PayoutId { get; set; }
    public Guid ClosureId { get; set; }
    public Guid OwnerId { get; set; }
    public decimal Amount { get; set; }
}

public sealed class FinancialTransaction : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public FinancialTransactionType Type { get; set; }
    public FinanceSource SourceType { get; set; }
    public Guid SourceId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
    public SettlementParty Payer { get; set; }
    public SettlementParty Receiver { get; set; }
    public string IdempotencyKey { get; set; } = null!;
}
