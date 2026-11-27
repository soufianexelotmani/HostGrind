using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Mappers;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using HostGrid.Api.Validators;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class GuestManagement(GuestRepository repository, PortfolioAccess access, PortfolioAudit audit, TimeProvider clock)
{
    internal Task<Page<GuestView>> ListAsync(string? search, bool? hasContact, string? sort, int page, int size, CancellationToken token)
    {
        access.EnsureBookingManager();
        var query = repository.Guests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(row => row.Name.Contains(term));
        }
        if (hasContact is bool filter)
            query = query.Where(row => (row.Email != null || row.Phone != null) == filter);
        var ordered = sort switch
        {
            "-name" => query.OrderByDescending(row => row.Name),
            "created" => query.OrderBy(row => row.CreatedAt),
            "-created" => query.OrderByDescending(row => row.CreatedAt),
            _ => query.OrderBy(row => row.Name)
        };
        return PortfolioRepository.PageAsync(ordered.ThenBy(row => row.Id).Select(GuestMapper.View), page, size, token);
    }

    internal async Task<GuestView> GetAsync(Guid id, CancellationToken token)
    {
        access.EnsureBookingManager();
        return await repository.Guests.AsNoTracking().Where(row => row.Id == id)
            .Select(GuestMapper.View).SingleOrDefaultAsync(token) ?? throw Missing();
    }

    internal Task<List<GuestView>> SuggestionsAsync(GuestLookup lookup, CancellationToken token)
    {
        access.EnsureBookingManager();
        var email = Optional(lookup.Email)?.ToUpperInvariant();
        var phone = GuestValidation.NormalizePhone(lookup.Phone);
        return repository.Guests.AsNoTracking().Where(row => email != null && row.EmailMatch == email
                || phone != null && row.PhoneMatch == phone)
            .OrderBy(row => row.Name).ThenBy(row => row.Id).Take(20).Select(GuestMapper.View).ToListAsync(token);
    }

    internal async Task<Page<GuestReservation>> HistoryAsync(Guid id, int page, int size, CancellationToken token)
    {
        await GetAsync(id, token);
        return await PortfolioRepository.PageAsync(repository.Reservations.Where(row => row.GuestId == id)
            .OrderByDescending(row => row.CheckInDate).ThenBy(row => row.Id)
            .Select(row => new GuestReservation(row.Id, row.Reference, row.PropertyId, row.CheckInDate, row.CheckOutDate, row.Status)),
            page, size, token);
    }

    internal async Task<Guid> SaveAsync(Guid? id, GuestWrite input, CancellationToken token)
    {
        access.EnsureBookingManager();
        var now = clock.GetUtcNow().UtcDateTime;
        Guest guest;
        if (id is Guid key)
        {
            guest = await repository.Guests.SingleOrDefaultAsync(row => row.Id == key, token) ?? throw Missing();
            PortfolioVersion.Match(guest.RowVersion, input.Version);
        }
        else
        {
            guest = new Guest { Id = Guid.NewGuid(), OrganizationId = access.OrganizationId, CreatedAt = now };
            repository.Guests.Add(guest);
        }
        guest.Name = input.Name.Trim();
        guest.Email = Optional(input.Email);
        guest.Phone = Optional(input.Phone);
        guest.EmailMatch = guest.Email?.ToUpperInvariant();
        guest.PhoneMatch = GuestValidation.NormalizePhone(guest.Phone);
        guest.UpdatedAt = now;
        await audit.SaveAsync(id is null ? "guest_created" : "guest_updated", "Guest", guest.Id, token);
        return guest.Id;
    }

    internal static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static SecurityRequestException Missing() => new(404, "Guest not found.", "guest_not_found");
}
