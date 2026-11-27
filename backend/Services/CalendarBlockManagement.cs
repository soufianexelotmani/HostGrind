using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Enums;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class CalendarBlockManagement
{
    private readonly AvailabilityRepository repository;
    private readonly AvailabilityTransactions transactions;
    private readonly PropertyAvailability availability;
    private readonly PortfolioAccess access;
    private readonly PortfolioAudit audit;
    private readonly TimeProvider clock;

    internal CalendarBlockManagement(AvailabilityRepository repository, AvailabilityTransactions transactions,
        PropertyAvailability availability, PortfolioAccess access, PortfolioAudit audit, TimeProvider clock)
        => (this.repository, this.transactions, this.availability, this.access, this.audit, this.clock) =
            (repository, transactions, availability, access, audit, clock);

    internal async Task<CalendarBlockEvent> GetAsync(Guid id, CancellationToken token)
    {
        var row = await FindAsync(id, token);
        var property = await access.PropertyAsync(row.PropertyId, false, token, tracking: false);
        var actionable = row.Status == CalendarBlockStatus.Active && row.EndDate > Today();
        return new(row.Id, row.PropertyId, row.StartDate, row.EndDate, row.Category, row.Category is null ? row.Reason : null,
            row.Description, row.Status, row.AvailabilityReleasedFrom, row.RowVersion,
            actionable && property.Status != PropertyStatus.Archived, actionable);
    }

    internal async Task<Guid> CreateAsync(CalendarBlockCreate input, CancellationToken token)
    {
        access.EnsureBookingManager();
        var id = Guid.NewGuid();
        await transactions.ExecuteAsync([input.PropertyId], async properties =>
        {
            var now = clock.GetUtcNow();
            var dates = new StayDates(input.Block.StartDate, input.Block.EndDate);
            await AdmitAsync(input.PropertyId, properties[input.PropertyId].Status, dates,
                PropertyAvailability.BusinessDate(now), null, token);
            Add(id, input.PropertyId, input.Block, dates, now);
        }, token);
        return id;
    }

    internal async Task<Guid> UpdateAsync(Guid id, CalendarBlockWrite input, CancellationToken token)
    {
        var original = await FindAsync(id, token);
        var replacementId = Guid.NewGuid();
        var result = id;
        await transactions.ExecuteAsync([original.PropertyId], async properties =>
        {
            var now = clock.GetUtcNow();
            var today = PropertyAvailability.BusinessDate(now);
            var current = await ReloadAsync(id, original.PropertyId, input.Version, today, token);
            var ongoing = current.StartDate < today;
            var start = ongoing && input.StartDate == current.StartDate ? today : input.StartDate;
            var dates = new StayDates(start, input.EndDate);
            await availability.ProtectDatesAsync(current.PropertyId, new(ongoing ? today : current.StartDate, current.EndDate), token);
            await AdmitAsync(current.PropertyId, properties[current.PropertyId].Status, dates, today, id, token);
            result = id;
            if (ongoing)
            {
                Release(current, today, "Replaced by a future revision", now);
                Add(replacementId, current.PropertyId, input, dates, now);
                result = replacementId;
                audit.Record("calendar_block_superseded", "CalendarBlock", id);
                return;
            }
            Apply(current, input, dates);
            current.UpdatedAt = now.UtcDateTime;
            current.UpdatedByUserId = access.Actor;
            audit.Record("calendar_block_revised", "CalendarBlock", id);
        }, token);
        return result;
    }

    internal async Task CancelAsync(Guid id, CalendarBlockCancel input, CancellationToken token)
    {
        var original = await FindAsync(id, token);
        await transactions.ExecuteAsync([original.PropertyId], async _ =>
        {
            var now = clock.GetUtcNow();
            var today = PropertyAvailability.BusinessDate(now);
            var current = await ReloadAsync(id, original.PropertyId, input.Version, today, token);
            var releaseFrom = current.StartDate < today ? today : current.StartDate;
            await availability.ProtectDatesAsync(current.PropertyId, new(releaseFrom, current.EndDate), token);
            Release(current, releaseFrom, input.Reason.Trim(), now);
            audit.Record("calendar_block_cancelled", "CalendarBlock", id);
        }, token);
    }

    private async Task<CalendarBlock> FindAsync(Guid id, CancellationToken token)
    {
        access.EnsureBookingManager();
        return await repository.Blocks.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, token)
            ?? throw new SecurityRequestException(404, "Calendar block not found.", "calendar_block_not_found");
    }

    private async Task<CalendarBlock> ReloadAsync(Guid id, Guid property, byte[]? version, DateOnly today, CancellationToken token)
    {
        var row = await repository.Blocks.SingleOrDefaultAsync(item => item.Id == id && item.PropertyId == property, token);
        if (row is null || version is null || !row.RowVersion.SequenceEqual(version)) throw AvailabilityTransactions.Stale();
        if (row.Status != CalendarBlockStatus.Active || row.EndDate <= today)
            throw new SecurityRequestException(409, "This calendar block is no longer editable.", "calendar_block_immutable");
        return row;
    }

    private async Task AdmitAsync(Guid property, PropertyStatus status, StayDates dates, DateOnly today, Guid? current, CancellationToken token)
    {
        if (dates.CheckInDate < today || dates.CheckOutDate <= dates.CheckInDate)
            throw new SecurityRequestException(400, "The block needs a current or future start and a later exclusive end.",
                "calendar_block_invalid_dates");
        if (status == PropertyStatus.Archived)
            throw new SecurityRequestException(409, "Archived properties cannot receive new blocked nights.", "calendar_block_property_archived");
        await availability.ProtectDatesAsync(property, dates, token);
        await availability.EnsureFreeAsync(property, dates, null, current, token);
    }

    private void Add(Guid id, Guid property, CalendarBlockWrite input, StayDates dates, DateTimeOffset now)
    {
        var row = new CalendarBlock { Id = id, PropertyId = property, OrganizationId = access.OrganizationId,
            CreatedAt = now.UtcDateTime, CreatedByUserId = access.Actor, Status = CalendarBlockStatus.Active };
        Apply(row, input, dates);
        repository.Blocks.Add(row);
        audit.Record("calendar_block_created", "CalendarBlock", id);
    }

    private static void Apply(CalendarBlock row, CalendarBlockWrite input, StayDates dates)
    {
        row.StartDate = dates.CheckInDate;
        row.EndDate = dates.CheckOutDate;
        row.Category = input.Reason;
        row.Reason = input.Reason!.Value.ToString();
        row.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
    }

    private void Release(CalendarBlock row, DateOnly firstReleasedNight, string reason, DateTimeOffset now)
    {
        row.Status = CalendarBlockStatus.Cancelled;
        row.CancelledAt = now.UtcDateTime;
        row.CancelledByUserId = access.Actor;
        row.CancellationReason = reason;
        row.AvailabilityReleasedFrom = firstReleasedNight;
    }

    private DateOnly Today() => PropertyAvailability.BusinessDate(clock.GetUtcNow());
}
