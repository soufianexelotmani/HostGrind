using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class PropertyAvailability
{
    private readonly AvailabilityRepository repository;
    private readonly AvailabilityTransactions transactions;
    private readonly PortfolioRepository portfolio;
    private readonly PortfolioAccess access;
    private readonly PortfolioAudit audit;
    private readonly TimeProvider clock;

    internal PropertyAvailability(AvailabilityRepository repository, AvailabilityTransactions transactions,
        PortfolioRepository portfolio, PortfolioAccess access, PortfolioAudit audit, TimeProvider clock)
        => (this.repository, this.transactions, this.portfolio, this.access, this.audit, this.clock) =
            (repository, transactions, portfolio, access, audit, clock);

    internal async Task<AvailabilityView> QueryAsync(Guid propertyId, StayDates dates, CancellationToken token)
    {
        var property = await access.PropertyAsync(propertyId, false, token, tracking: false);
        ValidateDates(dates);
        var reason = property.Status == PropertyStatus.Active
            ? await ConflictAsync(propertyId, dates, null, null, token) : "property_not_bookable";
        return new(dates.CheckInDate, dates.CheckOutDate, reason is null, reason ?? "available");
    }

    internal Task UpdatePropertyAsync(Guid propertyId, Func<Property, Task> update, CancellationToken token)
        => transactions.ExecuteAsync([propertyId], properties => update(properties[propertyId]), token);

    internal async Task<Guid> HoldAsync(Guid propertyId, Reservation pending, CancellationToken token)
    {
        EnsureBookingManager();
        // Stable across pre-commit retries and detached from the caller's tracked state.
        var id = Guid.NewGuid();
        await transactions.ExecuteAsync([propertyId], async properties =>
        {
            var dates = new StayDates(pending.CheckInDate, pending.CheckOutDate);
            await ValidateBookingAsync(properties[propertyId], dates, pending.Adults, pending.Children, null, token);
            repository.Reservations.Add(new Reservation
            {
                Id = id, OrganizationId = access.OrganizationId, PropertyId = propertyId, GuestId = pending.GuestId,
                Reference = pending.Reference, Source = pending.Source, ExternalBookingId = pending.ExternalBookingId,
                CheckInDate = dates.CheckInDate, CheckOutDate = dates.CheckOutDate, Adults = pending.Adults,
                Children = pending.Children, Status = ReservationStatus.Pending, PaymentStatus = PaymentStatus.Unpaid,
                GrossAmount = pending.GrossAmount, PlatformFees = pending.PlatformFees, PlatformFeeRate = pending.PlatformFeeRate,
                RevenueInputMode = pending.RevenueInputMode, NetPlatformRevenue = pending.NetPlatformRevenue, Notes = pending.Notes
            });
            audit.Record("reservation_hold_created", "Reservation", id);
        }, token);
        return id;
    }

    internal async Task ReviseAsync(Guid id, Guid destination, byte[] version, StayDates dates, short adults, short children,
        CancellationToken token)
    {
        var original = await FindReservationAsync(id, token);
        await transactions.ExecuteAsync([original.PropertyId, destination], async properties =>
        {
            var current = await FindReservationAsync(id, token);
            Match(current, original.PropertyId, version);
            var moving = current.PropertyId != destination;
            if (moving && current.Status != ReservationStatus.Pending)
                throw new SecurityRequestException(409, "Only pending reservations can move.",
                    "reservation_move_not_allowed");
            if (current.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
                throw new SecurityRequestException(409, "This reservation cannot be edited.", "reservation_edit_not_allowed");
            var changingNights = moving || current.CheckInDate != dates.CheckInDate || current.CheckOutDate != dates.CheckOutDate;
            if (changingNights && await repository.Database.Set<ReservationNight>().AnyAsync(row => row.ReservationId == id, token))
                throw new SecurityRequestException(409, "Existing nightly history requires an approved revision workflow.",
                    "reservation_nightly_history_protected");
            await ProtectDatesAsync(current.PropertyId, new(current.CheckInDate, current.CheckOutDate), token);
            await ValidateBookingAsync(properties[destination], dates, adults, children, current.Id, token);

            // PropertyId is part of the parent alternate key used by ReservationNight.
            // A scoped direct update permits a Pending move after checking that no such rows exist.
            var affected = await repository.Reservations.Where(row => row.Id == id && row.RowVersion == version)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.PropertyId, destination)
                    .SetProperty(row => row.CheckInDate, dates.CheckInDate).SetProperty(row => row.CheckOutDate, dates.CheckOutDate)
                    .SetProperty(row => row.Adults, adults).SetProperty(row => row.Children, children), token);
            if (affected != 1) throw AvailabilityTransactions.Stale();
            audit.Record("reservation_hold_revised", "Reservation", id);
        }, token);
    }

    internal async Task<Guid> SaveBlockAsync(Guid propertyId, Guid? existingId, byte[]? version, StayDates dates,
        string reason, CancellationToken token)
    {
        EnsureBookingManager();
        var id = existingId ?? Guid.NewGuid();
        await transactions.ExecuteAsync([propertyId], async _ =>
        {
            ValidateDates(dates);
            var current = existingId is null ? null : await repository.Blocks
                .SingleOrDefaultAsync(row => row.Id == id && row.PropertyId == propertyId, token)
                ?? throw new SecurityRequestException(404, "Calendar block not found.");
            if (current is not null && (version is null || !current.RowVersion.SequenceEqual(version)))
                throw AvailabilityTransactions.Stale();
            await EnsureFreeAsync(propertyId, dates, null, current?.Id, token);
            if (current is null)
            {
                current = new CalendarBlock { Id = id, OrganizationId = access.OrganizationId, PropertyId = propertyId };
                repository.Blocks.Add(current);
            }
            current.StartDate = dates.CheckInDate;
            current.EndDate = dates.CheckOutDate;
            current.Reason = reason;
            current.Status = CalendarBlockStatus.Active;
            audit.Record(existingId is null ? "calendar_block_created" : "calendar_block_revised", "CalendarBlock", id);
        }, token);
        return id;
    }

    internal async Task ReleaseAsync(Guid id, byte[] version, DateOnly firstReleasedNight, CancellationToken token)
    {
        var original = await FindReservationAsync(id, token);
        await transactions.ExecuteAsync([original.PropertyId], async _ =>
        {
            var current = await repository.Reservations.SingleOrDefaultAsync(row => row.Id == id, token)
                ?? throw MissingReservation();
            Match(current, original.PropertyId, version);
            var now = clock.GetUtcNow();
            var earliest = BusinessDate(now);
            if (current.Status == ReservationStatus.CheckedOut && current.ActualCheckOutAt is DateTime departure)
            {
                if (departure > now.UtcDateTime) throw InvalidRelease();
                var departureDate = BusinessDate(new DateTimeOffset(departure, TimeSpan.Zero));
                if (departureDate > earliest) earliest = departureDate;
            }
            else if (current.Status != ReservationStatus.NoShow || current.ActualCheckInAt is not null ||
                current.CheckInDate > earliest)
            {
                throw InvalidRelease();
            }

            if (current.AvailabilityReleasedFrom is not null || firstReleasedNight < earliest ||
                firstReleasedNight < current.CheckInDate || firstReleasedNight >= current.CheckOutDate)
                throw InvalidRelease();
            await ProtectDatesAsync(current.PropertyId, new(firstReleasedNight, current.CheckOutDate), token);

            current.AvailabilityReleasedFrom = firstReleasedNight;
            current.AvailabilityReleasedAt = now.UtcDateTime;
            current.AvailabilityReleasedBy = access.Actor;
            audit.Record("reservation_nights_released", "Reservation", id);
        }, token);
    }

    private async Task<Reservation> FindReservationAsync(Guid id, CancellationToken token)
    {
        EnsureBookingManager();
        return await repository.Reservations.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, token)
            ?? throw MissingReservation();
    }

    private void EnsureBookingManager()
    {
        _ = access.OrganizationId;
        if (!access.IsOrganizationManager)
            throw new SecurityRequestException(403, "Booking management requires an organization Manager.");
    }

    internal async Task ValidateBookingAsync(Property property, StayDates dates, short adults, short children,
        Guid? authorizedReservation, CancellationToken token, bool historical = false)
    {
        ValidateDates(dates);
        if (property.Status != PropertyStatus.Active)
            throw new SecurityRequestException(409, "The property is not bookable.", "property_not_bookable");
        if (adults < 1 || children < 0 || adults + children > property.MaxGuests)
            throw new SecurityRequestException(400, "The guest count exceeds capacity or is invalid.", "guest_capacity_invalid");
        await ProtectDatesAsync(property.Id, dates, token, historical);
        await EnsureFreeAsync(property.Id, dates, authorizedReservation, null, token);
    }

    internal async Task ProtectDatesAsync(Guid property, StayDates dates, CancellationToken token, bool historical = false)
    {
        if (await portfolio.HasClosedPeriodAsync(property, new(dates.CheckInDate, dates.CheckOutDate), token))
            throw new SecurityRequestException(409, "The change would affect a closed financial period.", "financial_period_closed");
        if (!historical && dates.CheckInDate < BusinessDate(clock.GetUtcNow()))
            throw new SecurityRequestException(409, "Historical booking changes require the approved history workflow.",
                "historical_booking_workflow_required");
    }

    private async Task EnsureFreeAsync(Guid property, StayDates dates, Guid? reservation, Guid? block, CancellationToken token)
    {
        var conflict = await ConflictAsync(property, dates, reservation, block, token);
        if (conflict is not null)
            throw new SecurityRequestException(409, "The requested dates overlap an existing booking or block.", conflict);
    }

    private async Task<string?> ConflictAsync(Guid property, StayDates dates, Guid? reservation, Guid? block, CancellationToken token)
    {
        if (await repository.HasReservationAsync(property, dates, reservation, token)) return "booking_conflict";
        if (await repository.HasBlockAsync(property, dates, block, token)) return "calendar_block_conflict";
        return null;
    }

    private static void ValidateDates(StayDates dates)
    {
        if (dates.CheckInDate == DateOnly.MinValue || dates.CheckOutDate <= dates.CheckInDate)
            throw new SecurityRequestException(400, "Checkout must be after check-in.", "invalid_date_range");
    }

    private static void Match(Reservation current, Guid originalProperty, byte[] version)
    {
        if (current.PropertyId != originalProperty || !current.RowVersion.SequenceEqual(version))
            throw AvailabilityTransactions.Stale();
    }

    private static SecurityRequestException MissingReservation() => new(404, "Reservation not found.");
    private static SecurityRequestException InvalidRelease() =>
        new(409, "The requested night release is not allowed.", "availability_release_invalid");

    internal static DateOnly BusinessDate(DateTimeOffset time) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(time, "Africa/Casablanca").DateTime);
}
