using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Options;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.Extensions.Options;

namespace HostGrid.Api.Services;

public sealed class CalendarManagement
{
    private readonly CalendarRepository repository;
    private readonly PortfolioAccess access;
    private readonly int maximumDays;
    private readonly TimeProvider clock;

    internal CalendarManagement(CalendarRepository repository, PortfolioAccess access, IOptions<CalendarOptions> options,
        TimeProvider clock)
        => (this.repository, this.access, maximumDays, this.clock) = (repository, access, options.Value.MaximumDays, clock);

    internal async Task<CalendarView> GetAsync(CalendarFilter filter, CancellationToken token)
    {
        var dates = new StayDates(filter.StartDate, filter.EndDate);
        var days = filter.EndDate.DayNumber - filter.StartDate.DayNumber;
        if (filter.StartDate == default || days < 1 || days > maximumDays)
            throw new SecurityRequestException(400, $"Calendar date range must be between 1 and {maximumDays} days.",
                "calendar_invalid_range");
        if (filter.Page is < 1 or > 10000 || filter.Size is < 1 or > 100)
            throw new SecurityRequestException(400, "Invalid calendar filters.", "calendar_invalid_filter");
        var operationalDetails = access.IsOrganizationManager;
        if (filter.BlockReason is not null && !operationalDetails)
            throw new SecurityRequestException(403, "Block reason filtering requires an organization Manager.");

        var scope = await PropertiesAsync(filter, token);
        var properties = await repository.PropertiesAsync(scope, operationalDetails, filter.Page, filter.Size, token);
        var ids = properties.Items.Select(row => row.Id).ToArray();
        var today = PropertyAvailability.BusinessDate(clock.GetUtcNow());
        if (ids.Length == 0) return new(filter.StartDate, filter.EndDate, today, properties, [], [], [], []);

        var visibleIds = scope.Where(row => ids.Contains(row.Id)).Select(row => row.Id);
        return new(filter.StartDate, filter.EndDate, today, properties,
            await repository.ReservationsAsync(visibleIds, dates, filter, operationalDetails, token),
            await repository.BlocksAsync(visibleIds, dates, filter, operationalDetails, today, token),
            await repository.ReservationHoldsAsync(visibleIds, dates, token),
            await repository.BlockHoldsAsync(visibleIds, dates, token));
    }

    internal async Task<CalendarSummary> SummaryAsync(CalendarSummaryFilter filter, CancellationToken token)
    {
        var date = filter.Date;
        if (date == default || date == DateOnly.MaxValue)
            throw new SecurityRequestException(400, "A valid calendar summary date is required.", "calendar_invalid_date");
        var properties = await PropertiesAsync(filter, token);
        var followingDate = date.AddDays(1);
        var nextMidnight = TimeZoneInfo.ConvertTimeToUtc(followingDate.ToDateTime(TimeOnly.MinValue),
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Casablanca"));
        return await repository.SummaryAsync(properties, new(date, followingDate), nextMidnight, token);
    }

    private async Task<IQueryable<Property>> PropertiesAsync(CalendarScope filter, CancellationToken token)
    {
        var properties = access.VisibleProperties();
        if (filter.PropertyId is Guid property)
        {
            await access.PropertyAsync(property, false, token, tracking: false);
            properties = properties.Where(row => row.Id == property);
        }
        if (filter.OwnerId is Guid owner)
        {
            if (!access.IsOrganizationManager)
                throw new SecurityRequestException(403, "Owner filtering requires an organization Manager.");
            properties = properties.Where(row => row.OwnerId == owner);
        }
        if (filter.Search?.Trim() is { Length: > 0 } search)
        {
            if (search.Length > 120)
                throw new SecurityRequestException(400, "Calendar search is too long.", "calendar_invalid_filter");
            properties = properties.Where(row => row.Name.Contains(search) || row.Reference.Contains(search));
        }
        return properties;
    }
}
