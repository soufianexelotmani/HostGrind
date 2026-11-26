using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class PropertyPricing(PortfolioRepository repository, PortfolioAccess access)
{
    internal async Task<decimal> ResolveAsync(Guid propertyId, DateOnly night, CancellationToken token)
    {
        var property = await access.PropertyAsync(propertyId, false, token);
        var day = Mask(night);
        return await repository.Database.Set<SeasonalRate>().Where(row => row.PropertyId == propertyId && row.StartDate <= night &&
            night < row.EndDate && (row.DaysOfWeek & day) != 0).OrderByDescending(row => row.Priority)
            .Select(row => (decimal?)row.NightlyRate).FirstOrDefaultAsync(token) ?? property.BaseNightlyRate;
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
