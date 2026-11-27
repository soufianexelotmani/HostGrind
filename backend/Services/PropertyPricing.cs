using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class PropertyPricing(PortfolioRepository repository, PortfolioAccess access)
{
    private const int MaxPreviewNights = 366;

    internal async Task<NightlyQuote> ResolveAsync(Guid propertyId, DateOnly night, CancellationToken token)
    {
        var property = await access.PropertyAsync(propertyId, false, token);
        var rules = await Rules(propertyId).Where(row => row.StartDate <= night && night < row.EndDate).ToListAsync(token);
        return Quote(night, property.BaseNightlyRate, rules);
    }

    internal async Task<IReadOnlyList<NightlyQuote>> PreviewAsync(Guid propertyId, DateOnly start, DateOnly end, CancellationToken token)
    {
        var property = await access.PropertyAsync(propertyId, false, token);
        return await PreviewAsync(property, start, end, token);
    }

    internal async Task<IReadOnlyList<NightlyQuote>> PreviewAsync(Property property, DateOnly start, DateOnly end, CancellationToken token)
    {
        var count = end.DayNumber - start.DayNumber;
        if (start == DateOnly.MinValue || count is < 1 or > MaxPreviewNights)
            throw new HostGrid.Api.Security.SecurityRequestException(400, "Choose a range of 1 to 366 nights.");
        var rules = await Rules(property.Id).Where(row => row.StartDate < end && start < row.EndDate).ToListAsync(token);
        return Enumerable.Range(0, count).Select(offset => Quote(start.AddDays(offset), property.BaseNightlyRate, rules)).ToArray();
    }

    private IQueryable<SeasonalRate> Rules(Guid propertyId)
        => repository.Database.Set<SeasonalRate>().AsNoTracking().Where(row => row.PropertyId == propertyId)
            .OrderByDescending(row => row.Priority).ThenBy(row => row.Id);

    private static NightlyQuote Quote(DateOnly night, decimal baseRate, IReadOnlyList<SeasonalRate> rules)
    {
        var rule = rules.FirstOrDefault(row => row.StartDate <= night && night < row.EndDate &&
            (row.DaysOfWeek & Mask(night)) != 0);
        var source = rule?.Priority switch { 3 => "custom", 2 => "seasonal", 1 => "weekend", _ => "base" };
        return new(night, rule?.NightlyRate ?? baseRate, "MAD", source, rule?.Id);
    }

    internal static bool Overlaps(SeasonalRate existing, SeasonWrite candidate)
    {
        var start = Math.Max(existing.StartDate.DayNumber, candidate.StartDate.DayNumber);
        var end = Math.Min(existing.EndDate.DayNumber, candidate.EndDate.DayNumber);
        for (var day = start; day < end && day < start + 7; day++)
        {
            if ((existing.DaysOfWeek & candidate.DaysOfWeek & Mask(DateOnly.FromDayNumber(day))) != 0)
                return true;
        }
        return false;
    }

    private static int Mask(DateOnly date) => 1 << (int)date.DayOfWeek;
}
