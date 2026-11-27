using HostGrid.Api.Data;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Repositories;

internal sealed class CalendarRepository(HostGridDbContext database)
{
    internal Task<Page<CalendarProperty>> PropertiesAsync(IQueryable<Property> properties, bool operationalDetails,
        int page, int size, CancellationToken token)
        => PortfolioRepository.PageAsync(
            from property in properties.AsNoTracking()
            join type in database.Set<PropertyType>() on property.TypeId equals type.Id
            join owner in database.Set<Owner>() on new { property.OrganizationId, Id = property.OwnerId }
                equals new { owner.OrganizationId, owner.Id }
            orderby property.Name, property.Id
            select new CalendarProperty(property.Id, property.Reference, property.Name, type.Code,
                operationalDetails ? owner.Name : null, property.Status, property.OperationalStatus), page, size, token);

    internal Task<List<CalendarReservation>> ReservationsAsync(IQueryable<Guid> ids, StayDates dates, CalendarFilter filter,
        bool operationalDetails, CancellationToken token)
    {
        var query = Reservations(ids).IntersectingReservations(dates);
        if (!filter.IncludeCancelledReservations) query = query.Where(row => row.Status != ReservationStatus.Cancelled);
        if (filter.BookingSource is { } source) query = query.Where(row => row.Source == source);
        if (filter.ReservationStatus is { } status) query = query.Where(row => row.Status == status);

        return (from row in query
                join guest in database.Set<Guest>() on new { row.OrganizationId, Id = row.GuestId }
                    equals new { guest.OrganizationId, guest.Id }
                orderby row.CheckInDate, row.Id
                let holds = (row.Status != ReservationStatus.Cancelled || row.ActualCheckInAt != null) &&
                    (row.AvailabilityReleasedFrom == null || row.AvailabilityReleasedFrom > row.CheckInDate)
                let effectiveEnd = row.AvailabilityReleasedFrom < row.CheckOutDate
                    ? row.AvailabilityReleasedFrom : row.CheckOutDate
                select new CalendarReservation(row.Id, row.PropertyId, row.Reference, row.Source, row.Status,
                    operationalDetails ? row.PaymentStatus : null, row.CheckInDate, row.CheckOutDate,
                    holds ? row.CheckInDate : null, holds ? effectiveEnd : null,
                    operationalDetails ? row.ActualCheckInAt : null, operationalDetails ? row.ActualCheckOutAt : null,
                    operationalDetails ? row.HistoricalActualCheckIn : null, operationalDetails ? row.HistoricalActualCheckOut : null,
                    operationalDetails ? guest.Name : null, operationalDetails,
                    operationalDetails && (row.Status == ReservationStatus.Pending ||
                        row.Status == ReservationStatus.Confirmed && database.Set<ReservationPriceRevision>()
                            .Any(revision => revision.ReservationId == row.Id))))
            .ToListAsync(token);
    }

    internal Task<List<CalendarBlockEvent>> BlocksAsync(IQueryable<Guid> ids, StayDates dates, CalendarFilter filter,
        bool operationalDetails, DateOnly today, CancellationToken token)
    {
        var query = Blocks(ids).IntersectingBlocks(dates);
        if (!filter.IncludeCancelledBlocks) query = query.Where(row => row.Status == CalendarBlockStatus.Active);
        if (filter.BlockReason is { } reason) query = query.Where(row => row.Category == reason);
        return query.OrderBy(row => row.StartDate).ThenBy(row => row.Id)
            .Select(row => new CalendarBlockEvent(row.Id, row.PropertyId, row.StartDate, row.EndDate,
                row.Category, operationalDetails && row.Category == null ? row.Reason : null,
                operationalDetails ? row.Description : null, row.Status, row.AvailabilityReleasedFrom,
                operationalDetails ? row.RowVersion : null,
                operationalDetails && row.Status == CalendarBlockStatus.Active && row.EndDate > today &&
                    database.Set<Property>().Any(property => property.Id == row.PropertyId && property.Status != PropertyStatus.Archived),
                operationalDetails && row.Status == CalendarBlockStatus.Active && row.EndDate > today)).ToListAsync(token);
    }

    internal Task<List<CalendarHold>> ReservationHoldsAsync(IQueryable<Guid> ids, StayDates dates, CancellationToken token)
        => Reservations(ids).IntersectingReservationHolds(dates)
            .Select(row => new CalendarHold(row.PropertyId, row.CheckInDate,
                row.AvailabilityReleasedFrom < row.CheckOutDate ? row.AvailabilityReleasedFrom!.Value : row.CheckOutDate))
            .ToListAsync(token);

    internal Task<List<CalendarHold>> BlockHoldsAsync(IQueryable<Guid> ids, StayDates dates, CancellationToken token)
        => Blocks(ids).IntersectingBlockHolds(dates)
            .Select(row => new CalendarHold(row.PropertyId, row.StartDate, row.AvailabilityReleasedFrom ?? row.EndDate)).ToListAsync(token);

    internal async Task<CalendarSummary> SummaryAsync(IQueryable<Property> properties, StayDates dates,
        DateTime nextMidnightUtc, CancellationToken token)
    {
        var date = dates.CheckInDate;
        var ids = properties.Select(row => row.Id);
        var reservations = Reservations(ids);
        var holds = reservations.IntersectingReservationHolds(dates);
        var blocks = Blocks(ids).IntersectingBlockHolds(dates);
        var scheduled = reservations.Where(row => row.Status == ReservationStatus.Confirmed ||
            row.Status == ReservationStatus.CheckedIn || row.Status == ReservationStatus.CheckedOut);
        var turnover = await scheduled.Where(row => row.CheckInDate == date || row.CheckOutDate == date)
            .GroupBy(_ => 1).Select(rows => new
            {
                Arrivals = rows.Count(row => row.CheckInDate == date),
                Departures = rows.Count(row => row.CheckOutDate == date)
            }).SingleOrDefaultAsync(token);

        var projectedOccupancy = holds.Where(row => row.Status == ReservationStatus.Confirmed ||
            row.Status == ReservationStatus.CheckedIn).Select(row => row.PropertyId);
        var realizedOccupancy = reservations.IntersectingReservations(dates)
            .Where(row => row.Status == ReservationStatus.CheckedOut || row.Status == ReservationStatus.Cancelled)
            .Where(row => (row.HistoricalActualCheckIn != null ? row.HistoricalActualCheckIn <= date :
                    row.ActualCheckInAt < nextMidnightUtc) &&
                (row.HistoricalActualCheckOut != null ? row.HistoricalActualCheckOut > date :
                    row.ActualCheckOutAt >= nextMidnightUtc)).Select(row => row.PropertyId);
        var occupied = await projectedOccupancy.Union(realizedOccupancy).CountAsync(token);
        var available = await properties.Where(row => row.Status == PropertyStatus.Active &&
            !holds.Any(hold => hold.PropertyId == row.Id) && !blocks.Any(block => block.PropertyId == row.Id)).CountAsync(token);
        var blocked = await blocks.Select(row => row.PropertyId).Distinct().CountAsync(token);
        var pending = await holds.Where(row => row.Status == ReservationStatus.Pending)
            .Select(row => row.PropertyId).Distinct().CountAsync(token);
        return new(date, turnover?.Arrivals ?? 0, turnover?.Departures ?? 0, occupied, available, blocked, pending);
    }

    private IQueryable<Reservation> Reservations(IQueryable<Guid> ids)
        => database.Set<Reservation>().AsNoTracking().Where(row => ids.Contains(row.PropertyId));

    private IQueryable<CalendarBlock> Blocks(IQueryable<Guid> ids)
        => database.Set<CalendarBlock>().AsNoTracking().Where(row => ids.Contains(row.PropertyId));
}
