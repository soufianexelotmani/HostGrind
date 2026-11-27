namespace HostGrid.Api.DTOs.Booking;

public sealed record AvailabilityView(DateOnly CheckInDate, DateOnly CheckOutDate, bool Available, string Status);

internal sealed record StayDates(DateOnly CheckInDate, DateOnly CheckOutDate);
