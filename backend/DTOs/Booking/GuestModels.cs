using HostGrid.Api.Enums;

namespace HostGrid.Api.DTOs.Booking;

public sealed record GuestWrite(string Name, string? Phone, string? Email, byte[]? Version);
public sealed record GuestLookup(string? Email, string? Phone);
public sealed record GuestView(Guid Id, string Name, string? Phone, string? Email,
    DateTime? CreatedAt, DateTime? UpdatedAt, byte[] Version);
public sealed record GuestReservation(Guid Id, string Reference, Guid PropertyId, DateOnly CheckInDate,
    DateOnly CheckOutDate, ReservationStatus Status);
