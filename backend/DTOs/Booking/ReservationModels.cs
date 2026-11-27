using HostGrid.Api.Enums;

namespace HostGrid.Api.DTOs.Booking;

public sealed record NightPriceWrite(DateOnly Night, decimal Amount);
public sealed record ReservationWrite(Guid PropertyId, Guid GuestId, BookingSource Source,
    string? ExternalBookingId, DateOnly CheckInDate, DateOnly CheckOutDate, short Adults, short Children,
    RevenueInputMode Mode, decimal? GrossAmount, decimal? PlatformFees, decimal? NetAmount, string? Notes,
    bool Confirm, bool AcceptPriceChanges, IReadOnlyList<NightPriceWrite>? NightlyPrices, byte[]? Version);
public sealed record ReservationPreview(Guid PropertyId, DateOnly CheckInDate, DateOnly CheckOutDate, Guid? ReservationId);
public sealed record HistoricalReservation(ReservationWrite Booking, DateOnly ConfirmationDate, DateOnly? ActualCheckInDate,
    DateOnly? ActualCheckOutDate, string Reason, DateTimeOffset? ActualCheckInAt = null, DateTimeOffset? ActualCheckOutAt = null);
public sealed record HistoricalView(DateOnly? ActualCheckInDate, DateOnly? ActualCheckOutDate, string Reason, Guid? EnteredBy);
public sealed record ReservationSummary(Guid Id, string Reference, string? ExternalBookingId, Guid PropertyId,
    string PropertyName, Guid GuestId, string GuestName, BookingSource Source, DateOnly CheckInDate,
    DateOnly CheckOutDate, ReservationStatus Status, PaymentStatus PaymentStatus, decimal NetAmount);
public sealed record ReservationFilter(Guid? Property, Guid? Owner, BookingSource? Source, ReservationStatus? Status,
    PaymentStatus? PaymentStatus, DateOnly? ArrivalFrom, DateOnly? ArrivalTo, DateOnly? DepartureFrom,
    DateOnly? DepartureTo, string? Reference, string? ExternalReference, string? Guest,
    string? Sort, int Page = 1, int Size = 20);
public sealed record AcceptedNight(DateOnly Night, decimal Amount, string Source, Guid? RuleId, bool ManualOverride);
public sealed record PriceRevisionView(int Number, DateOnly CheckInDate, DateOnly CheckOutDate, RevenueInputMode Mode,
    decimal? GrossAmount, decimal? PlatformFees, decimal NetAmount, DateTime CreatedAt, Guid ActorId,
    IReadOnlyList<AcceptedNight> Nights);
public sealed record ReservationView(ReservationSummary Summary, short Adults, short Children, int NightCount,
    RevenueInputMode Mode, decimal? GrossAmount, decimal? PlatformFees, decimal? CommissionRate,
    decimal? CommissionAmount, DateOnly? ConfirmationDate, DateTime? ConfirmedAt, Guid? ConfirmedBy,
    string? Notes, DateTime? CreatedAt, DateTime? UpdatedAt, DateTime? ActualCheckInAt, DateTime? ActualCheckOutAt,
    DateOnly? AvailabilityReleasedFrom, PriceRevisionView? Pricing, HistoricalView? Historical, byte[] Version);
