using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using HostGrid.Api.Security;

namespace HostGrid.Api.Services;

public sealed class ReservationPricing(PropertyPricing pricing)
{
    internal async Task<ReservationPriceRevision> AcceptAsync(Property property, ReservationWrite input,
        ReservationPriceRevision? previous, bool historical, CancellationToken token)
    {
        var nights = input.CheckOutDate.DayNumber - input.CheckInDate.DayNumber;
        if (nights is < 1 or > 366) throw Invalid("Choose a stay of 1 to 366 nights.");
        var supplied = input.NightlyPrices ?? [];
        if (supplied.Count > nights || supplied.Any(row => row is null || row.Night < input.CheckInDate ||
            row.Night >= input.CheckOutDate || !Money(row.Amount)) || supplied.Select(row => row.Night).Distinct().Count() != supplied.Count)
            throw Invalid("Nightly prices must be unique, valid amounts within the stay.");
        var amounts = supplied.ToDictionary(row => row.Night, row => row.Amount);
        var reuse = previous?.Mode == input.Mode ? previous.Nights.ToDictionary(row => row.Night) : [];
        var revision = new ReservationPriceRevision { Id = Guid.NewGuid(), Mode = input.Mode,
            CheckInDate = input.CheckInDate, CheckOutDate = input.CheckOutDate };
        var directProposal = input.Source == BookingSource.Direct && !historical;
        var entered = input.Mode == RevenueInputMode.NetDirect ? input.NetAmount : input.GrossAmount;
        if (input.Mode is not (RevenueInputMode.NetDirect or RevenueInputMode.GrossWithFees) ||
            input.Mode == RevenueInputMode.NetDirect && (input.GrossAmount is not null || input.PlatformFees is not null) ||
            input.Mode == RevenueInputMode.GrossWithFees && (input.NetAmount is not null || input.PlatformFees is null) ||
            entered is decimal totalInput && !Money(totalInput) || input.PlatformFees is decimal feeInput && !Money(feeInput))
            throw Invalid("Choose one complete pricing mode and valid MAD amounts.");
        if (!directProposal && entered is null) throw Invalid("Enter the actual booking total.");

        var added = Enumerable.Range(0, nights).Select(offset => input.CheckInDate.AddDays(offset)).ToArray();
        var quotes = directProposal && added.Any(day => !reuse.ContainsKey(day) && !amounts.ContainsKey(day))
            ? (await pricing.PreviewAsync(property, input.CheckInDate, input.CheckOutDate, token)).ToDictionary(row => row.Night)
            : [];
        var unresolved = new List<ReservationNightPrice>();
        foreach (var day in added)
        {
            reuse.TryGetValue(day, out var retained);
            ReservationNightPrice row;
            if (retained is not null && (!amounts.TryGetValue(day, out var value) || retained.Amount == value))
                row = NewNight(day, retained.Amount, retained.Source, retained.RuleId, retained.ManualOverride);
            else if (amounts.TryGetValue(day, out var manual))
                row = NewNight(day, manual, directProposal ? "manual" : "external", directProposal ? retained?.RuleId : null, directProposal);
            else if (directProposal)
            {
                var quote = quotes[day];
                row = NewNight(day, quote.Amount, quote.Source, quote.RuleId, false);
            }
            else
            {
                row = NewNight(day, 0, "allocated", null, false);
                unresolved.Add(row);
            }
            revision.Nights.Add(row);
        }

        var known = revision.Nights.Sum(row => row.Amount);
        if (unresolved.Count != 0)
        {
            var residual = (entered!.Value - known) * 100m;
            if (residual < 0) throw Invalid("The entered total is below the sum of accepted nights.");
            var cents = decimal.Truncate(residual / unresolved.Count);
            var extra = residual - cents * unresolved.Count;
            for (var index = 0; index < unresolved.Count; index++)
                unresolved[index].Amount = (cents + (index < extra ? 1 : 0)) / 100m;
        }
        var total = revision.Nights.Sum(row => row.Amount);
        if (!Money(total) || entered is decimal expected && expected != total)
            throw Invalid("The entered total must equal the sum of accepted nightly prices.");
        var fees = input.PlatformFees ?? 0m;
        if (fees > total) throw Invalid("Platform fees cannot exceed the gross total.");
        revision.GrossAmount = input.Mode == RevenueInputMode.GrossWithFees ? total : null;
        revision.PlatformFees = input.PlatformFees;
        revision.NetAmount = total - fees;
        return revision;
    }

    internal static bool Same(ReservationPriceRevision? before, ReservationPriceRevision after)
        => before is not null && before.Mode == after.Mode && before.GrossAmount == after.GrossAmount &&
            before.PlatformFees == after.PlatformFees && before.NetAmount == after.NetAmount &&
            before.Nights.Count == after.Nights.Count && before.Nights.OrderBy(row => row.Night)
                .Zip(after.Nights, (left, right) => left.Night == right.Night && left.Amount == right.Amount &&
                    left.Source == right.Source && left.RuleId == right.RuleId && left.ManualOverride == right.ManualOverride)
                .All(equal => equal);

    private static ReservationNightPrice NewNight(DateOnly night, decimal amount, string source, Guid? rule, bool manual)
        => new() { Night = night, Amount = amount, Source = source, RuleId = rule, ManualOverride = manual };

    private static bool Money(decimal amount) => amount is >= 0 and <= 9999999999999999.99m && decimal.Round(amount, 2) == amount;
    private static SecurityRequestException Invalid(string message) => new(400, message, "reservation_pricing_invalid");
}
