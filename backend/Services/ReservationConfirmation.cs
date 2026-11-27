using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Enums;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class ReservationConfirmation(PortfolioRepository portfolio, AuthenticationRepository locks,
    PortfolioAccess access, PortfolioAudit audit, TimeProvider clock)
{
    internal async Task ConfirmAsync(Reservation reservation, CancellationToken token)
    {
        access.EnsureBookingManager();
        if (reservation.Status != ReservationStatus.Pending || reservation.CommissionRateSnapshot is not null ||
            reservation.ConfirmationDate is not null)
            throw new SecurityRequestException(409, "Only an unconfirmed Pending reservation can be confirmed.",
                "reservation_confirmation_not_allowed");
        // Share the portfolio lock with commission schedule writes.
        await locks.LockResourceAsync("property:" + reservation.PropertyId, token);
        var now = clock.GetUtcNow();
        var day = PropertyAvailability.BusinessDate(now);
        reservation.CommissionRateSnapshot = await ResolveRateAsync(reservation.PropertyId, day, token);
        reservation.ConfirmationDate = day;
        reservation.ConfirmedAt = now.UtcDateTime;
        reservation.ConfirmedBy = access.Actor;
        reservation.Status = ReservationStatus.Confirmed;
        audit.Record("reservation_confirmed", "Reservation", reservation.Id);
    }

    internal async Task<decimal> ResolveRateAsync(Guid property, DateOnly day, CancellationToken token)
    {
        var rates = await portfolio.CommissionRates(property)
            .Where(row => row.EffectiveFrom <= day && (row.EffectiveTo == null || day < row.EffectiveTo))
            .Select(row => row.Rate).Take(2).ToListAsync(token);
        if (rates.Count != 1)
            throw new SecurityRequestException(409, "An unambiguous effective commission rate is required.",
                "commission_rate_unavailable");
        return rates[0];
    }

    internal static void RecordAmount(Reservation row)
    {
        var amount = row.NetPlatformRevenue * row.CommissionRateSnapshot / 100m;
        row.CommissionAmount = amount is decimal exact ? decimal.Round(exact, 2, MidpointRounding.AwayFromZero) : null;
    }
}
