using System.Linq.Expressions;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Booking;

namespace HostGrid.Api.Mappers;

internal static class GuestMapper
{
    internal static readonly Expression<Func<Guest, GuestView>> View = row =>
        new GuestView(row.Id, row.Name, row.Phone, row.Email, row.CreatedAt, row.UpdatedAt, row.RowVersion);
}
