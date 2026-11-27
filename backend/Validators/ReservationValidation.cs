using FluentValidation;
using HostGrid.Api.DTOs.Booking;

namespace HostGrid.Api.Validators;

internal static class ReservationValidation
{
    internal static void Register(IServiceCollection services)
        => SecurityValidationFilter.Add<ReservationWrite>(services, Configure);

    internal static void Configure(InlineValidator<ReservationWrite> rules)
    {
        rules.RuleFor(row => row.PropertyId).NotEmpty();
        rules.RuleFor(row => row.GuestId).NotEmpty();
        rules.RuleFor(row => row.Source).IsInEnum();
        rules.RuleFor(row => row.Notes).MaximumLength(4000);
        rules.RuleFor(row => row.ExternalBookingId).MaximumLength(200);
        rules.RuleFor(row => row.Version).Must(version => version is null || version.Length == 8);
        rules.RuleFor(row => row.NightlyPrices).Must(prices => prices is null || prices.Count <= 366);
    }
}
