using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Enums;

namespace HostGrid.Api.DTOs.Booking;

public record CalendarScope(Guid? PropertyId, Guid? OwnerId, string? Search);

public sealed record CalendarFilter(DateOnly StartDate, DateOnly EndDate, Guid? PropertyId = null, Guid? OwnerId = null,
    string? Search = null, BookingSource? BookingSource = null, ReservationStatus? ReservationStatus = null,
    CalendarBlockReason? BlockReason = null, bool IncludeCancelledReservations = false, bool IncludeCancelledBlocks = false,
    int Page = 1, int Size = 30) : CalendarScope(PropertyId, OwnerId, Search);

public sealed record CalendarProperty(Guid Id, string Reference, string Name, string Type, string? OwnerName,
    PropertyStatus Status, OperationalStatus OperationalStatus);

public sealed record CalendarReservation(Guid Id, Guid PropertyId, string Reference, BookingSource Source,
    ReservationStatus Status, PaymentStatus? PaymentStatus, DateOnly CheckInDate, DateOnly CheckOutDate,
    DateOnly? EffectiveStart, DateOnly? EffectiveEnd, DateTime? ActualCheckInAt, DateTime? ActualCheckOutAt,
    DateOnly? HistoricalActualCheckIn, DateOnly? HistoricalActualCheckOut, string? GuestName,
    bool CanViewDetails, bool CanEdit);

public sealed record CalendarBlockEvent(Guid Id, Guid PropertyId, DateOnly StartDate, DateOnly EndDate,
    CalendarBlockReason? Reason, string? LegacyReason, string? Description, CalendarBlockStatus Status,
    DateOnly? AvailabilityReleasedFrom, byte[]? Version, bool CanEdit, bool CanCancel);

public sealed record CalendarBlockWrite(DateOnly StartDate, DateOnly EndDate, CalendarBlockReason? Reason,
    string? Description, byte[]? Version);

public sealed record CalendarBlockCreate(Guid PropertyId, CalendarBlockWrite Block);
public sealed record CalendarBlockCancel(byte[] Version, string Reason);

public sealed record CalendarHold(Guid PropertyId, DateOnly StartDate, DateOnly EndDate);

public sealed record CalendarView(DateOnly StartDate, DateOnly EndDate, DateOnly BusinessDate, Page<CalendarProperty> Properties,
    IReadOnlyList<CalendarReservation> Reservations, IReadOnlyList<CalendarBlockEvent> Blocks,
    IReadOnlyList<CalendarHold> ReservationHolds, IReadOnlyList<CalendarHold> BlockHolds);

public sealed record CalendarSummaryFilter(DateOnly Date, Guid? PropertyId = null, Guid? OwnerId = null,
    string? Search = null) : CalendarScope(PropertyId, OwnerId, Search);

public sealed record CalendarSummary(DateOnly Date, int ScheduledArrivals, int ScheduledDepartures,
    int OccupiedProperties, int AvailableProperties, int BlockedProperties, int PendingHeldProperties);
