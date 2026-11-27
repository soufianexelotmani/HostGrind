using HostGrid.Api.Enums;
using HostGrid.Api.Entities.Organization;

namespace HostGrid.Api.Entities.Booking;

public sealed class Guest : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? EmailMatch { get; set; }
    public string? PhoneMatch { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class Reservation : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid GuestId { get; set; }
    public string Reference { get; set; } = null!;
    public BookingSource Source { get; set; }
    public string? ExternalBookingId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public short Adults { get; set; }
    public short Children { get; set; }
    public decimal? GrossAmount { get; set; }
    public decimal? PlatformFees { get; set; }
    public decimal? PlatformFeeRate { get; set; }
    public RevenueInputMode RevenueInputMode { get; set; }
    public decimal NetPlatformRevenue { get; set; }
    public decimal? CommissionRateSnapshot { get; set; }
    public decimal? CommissionAmount { get; set; }
    public ReservationStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? ActualCheckInAt { get; set; }
    public DateTime? ActualCheckOutAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateOnly? ConfirmationDate { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public DateOnly? HistoricalActualCheckIn { get; set; }
    public DateOnly? HistoricalActualCheckOut { get; set; }
    public string? HistoricalReason { get; set; }
    public Guid? HistoricalEvidenceId { get; set; }
    public Guid? HistoricalEnteredBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateOnly? AvailabilityReleasedFrom { get; set; }
    public DateTime? AvailabilityReleasedAt { get; set; }
    public Guid? AvailabilityReleasedBy { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class ReservationPriceRevision : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ReservationId { get; set; }
    public int Number { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public RevenueInputMode Mode { get; set; }
    public decimal? GrossAmount { get; set; }
    public decimal? PlatformFees { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ActorId { get; set; }
    public List<ReservationNightPrice> Nights { get; set; } = [];
}

public sealed class ReservationNightPrice : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RevisionId { get; set; }
    public DateOnly Night { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; } = null!;
    public Guid? RuleId { get; set; }
    public bool ManualOverride { get; set; }
}

public sealed class ReservationPayment : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ReservationId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public SettlementParty Receiver { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
}

public sealed class ReservationCancellation : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ReservationId { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime CancelledAt { get; set; }
    public decimal RetainedFee { get; set; }
}

public sealed class ReservationRefund : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid? PaymentId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly RefundedOn { get; set; }
    public SettlementParty Payer { get; set; }
    public string? Reference { get; set; }
}

public sealed class ReservationStatusHistory : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ReservationId { get; set; }
    public ReservationStatus OldStatus { get; set; }
    public ReservationStatus NewStatus { get; set; }
    public Guid ActorId { get; set; }
    public DateTime ChangedAt { get; set; }
}

public sealed class CalendarBlock : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = null!;
    public CalendarBlockStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class ReservationNight : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid PropertyId { get; set; }
    public DateOnly NightDate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionRateSnapshot { get; set; }
}
