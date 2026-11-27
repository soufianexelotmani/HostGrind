using HostGrid.Api.Data;
using HostGrid.Api.Entities.Booking;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Repositories;

public sealed class GuestRepository(HostGridDbContext database)
{
    internal DbSet<Guest> Guests => database.Set<Guest>();
    internal IQueryable<Reservation> Reservations => database.Set<Reservation>().AsNoTracking();
}
