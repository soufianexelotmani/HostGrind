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
    public decimal CommissionRateSnapshot { get; set; }
    public decimal CommissionAmount { get; set; }
    public ReservationStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? ActualCheckInAt { get; set; }
    public DateTime? ActualCheckOutAt { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = null!;
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
