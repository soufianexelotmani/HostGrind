using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Booking;

namespace HostGrid.Api.Mappers;

internal static class ReservationMapper
{
    internal static PriceRevisionView Revision(ReservationPriceRevision row)
        => new(row.Number, row.CheckInDate, row.CheckOutDate, row.Mode, row.GrossAmount, row.PlatformFees, row.NetAmount,
            row.CreatedAt, row.ActorId, row.Nights.OrderBy(night => night.Night)
                .Select(night => new AcceptedNight(night.Night, night.Amount, night.Source, night.RuleId, night.ManualOverride)).ToArray());
}
