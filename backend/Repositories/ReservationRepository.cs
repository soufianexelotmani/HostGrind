using HostGrid.Api.Data;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Property;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Repositories;

public sealed class ReservationRepository(HostGridDbContext database)
{
    internal HostGridDbContext Database => database;
    internal DbSet<Reservation> Reservations => database.Set<Reservation>();
    internal DbSet<ReservationPriceRevision> Revisions => database.Set<ReservationPriceRevision>();

    internal IQueryable<ReservationSummary> Summaries(ReservationFilter filter, Guid? id = null)
    {
        var source = Reservations.AsNoTracking();
        if (id is Guid key) source = source.Where(row => row.Id == key);
        if (filter.Property is Guid property) source = source.Where(row => row.PropertyId == property);
        if (filter.Source is { } platform) source = source.Where(row => row.Source == platform);
        if (filter.Status is { } status) source = source.Where(row => row.Status == status);
        if (filter.PaymentStatus is { } payment) source = source.Where(row => row.PaymentStatus == payment);
        if (filter.ArrivalFrom is { } arrivalFrom) source = source.Where(row => row.CheckInDate >= arrivalFrom);
        if (filter.ArrivalTo is { } arrivalTo) source = source.Where(row => row.CheckInDate <= arrivalTo);
        if (filter.DepartureFrom is { } departureFrom) source = source.Where(row => row.CheckOutDate >= departureFrom);
        if (filter.DepartureTo is { } departureTo) source = source.Where(row => row.CheckOutDate <= departureTo);
        if (!string.IsNullOrWhiteSpace(filter.Reference))
            source = source.Where(row => row.Reference.Contains(filter.Reference));
        if (!string.IsNullOrWhiteSpace(filter.ExternalReference))
            source = source.Where(row => row.ExternalBookingId != null && row.ExternalBookingId.Contains(filter.ExternalReference));

        var query = from row in source
                    join propertyRow in database.Set<Property>() on row.PropertyId equals propertyRow.Id
                    join guestRow in database.Set<Guest>() on row.GuestId equals guestRow.Id
                    where (filter.Owner == null || propertyRow.OwnerId == filter.Owner) &&
                          (filter.Guest == null || guestRow.Name.Contains(filter.Guest))
                    select new
                    {
                        row.Id, row.Reference, row.ExternalBookingId, row.PropertyId, PropertyName = propertyRow.Name,
                        row.GuestId, GuestName = guestRow.Name, row.Source, row.CheckInDate, row.CheckOutDate,
                        row.Status, row.PaymentStatus, row.NetPlatformRevenue
                    };
        var ordered = filter.Sort switch
        {
            "arrival" => query.OrderBy(row => row.CheckInDate),
            "departure" => query.OrderBy(row => row.CheckOutDate),
            "-departure" => query.OrderByDescending(row => row.CheckOutDate),
            "reference" => query.OrderBy(row => row.Reference),
            "-reference" => query.OrderByDescending(row => row.Reference),
            _ => query.OrderByDescending(row => row.CheckInDate)
        };
        return ordered.ThenBy(row => row.Id).Select(row => new ReservationSummary(row.Id, row.Reference,
            row.ExternalBookingId, row.PropertyId, row.PropertyName, row.GuestId, row.GuestName, row.Source,
            row.CheckInDate, row.CheckOutDate, row.Status, row.PaymentStatus, row.NetPlatformRevenue));
    }
}
