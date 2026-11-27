using System.Text.Json;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Document;
using HostGrid.Api.Enums;
using HostGrid.Api.Mappers;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class ReservationManagement
{
    private readonly ReservationRepository repository;
    private readonly AvailabilityTransactions transactions;
    private readonly PropertyAvailability availability;
    private readonly PortfolioAccess access;
    private readonly PortfolioAudit audit;
    private readonly GuestRepository guests;
    private readonly ReservationPricing pricing;
    private readonly PropertyPricing quotes;
    private readonly ReservationConfirmation confirmation;
    private readonly ReservationEvidence evidence;
    private readonly AuthenticationRepository locks;
    private readonly TimeProvider clock;

    internal ReservationManagement(ReservationRepository repository, AvailabilityTransactions transactions,
        PropertyAvailability availability, PortfolioAccess access, PortfolioAudit audit, GuestRepository guests,
        ReservationPricing pricing, PropertyPricing quotes, ReservationConfirmation confirmation, ReservationEvidence evidence,
        AuthenticationRepository locks, TimeProvider clock)
        => (this.repository, this.transactions, this.availability, this.access, this.audit, this.guests, this.pricing,
            this.quotes, this.confirmation, this.evidence, this.locks, this.clock) =
            (repository, transactions, availability, access, audit, guests, pricing, quotes, confirmation, evidence, locks, clock);

    internal Task<Page<ReservationSummary>> ListAsync(ReservationFilter filter, CancellationToken token)
    {
        access.EnsureBookingManager();
        return PortfolioRepository.PageAsync(repository.Summaries(filter), filter.Page, filter.Size, token);
    }

    internal async Task<ReservationView> GetAsync(Guid id, CancellationToken token)
    {
        var row = await FindAsync(id, token);
        var summary = await repository.Summaries(new(null, null, null, null, null, null, null, null, null, null, null, null, null), id)
            .SingleAsync(token);
        var price = await LatestAsync(id, token);
        return new(summary, row.Adults, row.Children, row.CheckOutDate.DayNumber - row.CheckInDate.DayNumber,
            row.RevenueInputMode, row.GrossAmount, row.PlatformFees, row.CommissionRateSnapshot, row.CommissionAmount,
            row.ConfirmationDate, row.ConfirmedAt, row.ConfirmedBy, row.Notes, row.CreatedAt, row.UpdatedAt,
            row.ActualCheckInAt, row.ActualCheckOutAt, row.AvailabilityReleasedFrom,
            price is null ? null : ReservationMapper.Revision(price),
            row.HistoricalReason is null ? null : new(row.HistoricalActualCheckIn, row.HistoricalActualCheckOut,
                row.HistoricalReason, row.HistoricalEnteredBy), row.RowVersion);
    }

    internal async Task<Page<PriceRevisionView>> HistoryAsync(Guid id, int page, int size, CancellationToken token)
    {
        await FindAsync(id, token);
        var result = await PortfolioRepository.PageAsync(RevisionRows(id).OrderByDescending(row => row.Number)
            .Include(row => row.Nights), page, size, token);
        return new(result.Items.Select(ReservationMapper.Revision).ToArray(), result.Total, result.PageNumber, result.PageSize);
    }

    internal async Task<IReadOnlyList<AcceptedNight>> PreviewAsync(ReservationPreview input, CancellationToken token)
    {
        access.EnsureBookingManager();
        var property = await access.PropertyAsync(input.PropertyId, true, token, tracking: false);
        ReservationPriceRevision? previous = null;
        if (input.ReservationId is Guid id && (await FindAsync(id, token)).PropertyId == input.PropertyId)
            previous = await LatestAsync(id, token);
        var previousNights = previous?.Nights.ToDictionary(row => row.Night) ?? [];
        var proposal = await quotes.PreviewAsync(property, input.CheckInDate, input.CheckOutDate, token);
        return proposal.Select(row => previousNights.TryGetValue(row.Night, out var accepted)
            ? new AcceptedNight(accepted.Night, accepted.Amount, accepted.Source, accepted.RuleId, accepted.ManualOverride)
            : new AcceptedNight(row.Night, row.Amount, row.Source, row.RuleId, false)).ToArray();
    }

    internal Task<Guid> CreateAsync(ReservationWrite input, CancellationToken token)
        => WriteAsync(null, input, null, null, token);

    internal async Task UpdateAsync(Guid id, ReservationWrite input, CancellationToken token)
    {
        var original = await FindAsync(id, token);
        await WriteAsync(original, input, null, null, token);
    }

    internal async Task<Guid> HistoricalAsync(HistoricalReservation input, IFormFile? upload, CancellationToken token)
    {
        access.EnsureBookingManager();
        ValidateHistory(input);
        await access.PropertyAsync(input.Booking.PropertyId, true, token, tracking: false);
        var file = await evidence.StageAsync(upload, token);
        try { return await WriteAsync(null, input.Booking, input, file, token); }
        catch (Exception error) when (error is not SecurityRequestException { Code: "availability_commit_unknown" })
        {
            await evidence.DeleteAsync(file);
            throw;
        }
    }

    internal async Task<(Stream Content, string Name)> EvidenceAsync(Guid id, CancellationToken token)
    {
        var row = await FindAsync(id, token);
        var file = await repository.Database.Set<StoredFile>().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == row.HistoricalEvidenceId, token);
        return await evidence.DownloadAsync(file, token);
    }

    private async Task<Guid> WriteAsync(Reservation? original, ReservationWrite input, HistoricalReservation? history,
        StoredFile? file, CancellationToken token)
    {
        access.EnsureBookingManager();
        var id = original?.Id ?? Guid.NewGuid();
        var external = GuestManagement.Optional(input.ExternalBookingId);
        var dates = new StayDates(input.CheckInDate, input.CheckOutDate);
        try
        {
            await transactions.ExecuteAsync([original?.PropertyId ?? input.PropertyId, input.PropertyId], async properties =>
            {
                var current = original is null ? null : await FindAsync(id, token);
                if (current is not null) await ValidateRevisionAsync(original!, current, input, token);
                var property = properties[input.PropertyId];
                await availability.ValidateBookingAsync(property, dates, input.Adults, input.Children, current?.Id, token, history is not null);
                if (!await guests.Guests.AnyAsync(row => row.Id == input.GuestId, token)) throw GuestManagement.Missing();
                // Match property configuration and commission writers while accepting quotes or capturing a rate.
                await locks.LockResourceAsync("property:" + property.Id, token);
                await repository.Database.Entry(property).ReloadAsync(token);
                var prior = current is null ? null : await LatestAsync(id, token);
                if (current?.Status == ReservationStatus.Confirmed && prior is null)
                    throw new SecurityRequestException(409, "The original nightly prices are unavailable; use a historical correction.",
                        "reservation_legacy_pricing_protected");
                if (current?.Status == ReservationStatus.Confirmed && input.Mode != current.RevenueInputMode)
                    throw new SecurityRequestException(409, "The confirmed pricing basis is fixed.", "reservation_pricing_basis_protected");
                var retained = current?.PropertyId == input.PropertyId && current.Source == input.Source ? prior : null;
                var accepted = await pricing.AcceptAsync(property, input, retained, history is not null, token);
                var changedPrices = !ReservationPricing.Same(prior, accepted);
                if (current?.Status == ReservationStatus.Confirmed && changedPrices && !input.AcceptPriceChanges)
                    throw new SecurityRequestException(409, "Accept the price revision before saving.", "price_revision_acceptance_required");

                var now = clock.GetUtcNow().UtcDateTime;
                var oldValues = current is null ? null : JsonSerializer.Serialize(AuditState(current, prior?.Number));
                var guestChanged = current is not null && current.GuestId != input.GuestId;
                var referenceChanged = current is not null && current.ExternalBookingId != external;
                var notesChanged = current is not null && current.Notes != GuestManagement.Optional(input.Notes);
                var row = current ?? new Reservation { Id = id, OrganizationId = access.OrganizationId,
                    Reference = "RS-" + id.ToString("N").ToUpperInvariant(), CreatedAt = now };
                Apply(row, input, external, accepted, now);
                if (history is not null)
                {
                    row.Status = ReservationStatus.CheckedOut;
                    row.ConfirmationDate = history.ConfirmationDate;
                    row.CommissionRateSnapshot = await confirmation.ResolveRateAsync(property.Id, history.ConfirmationDate, token);
                    row.HistoricalActualCheckIn = history.ActualCheckInDate;
                    row.HistoricalActualCheckOut = history.ActualCheckOutDate;
                    row.ActualCheckInAt = history.ActualCheckInAt?.UtcDateTime;
                    row.ActualCheckOutAt = history.ActualCheckOutAt?.UtcDateTime;
                    row.HistoricalReason = history.Reason.Trim();
                    row.HistoricalEnteredBy = access.Actor;
                    row.HistoricalEvidenceId = file!.Id;
                    repository.Database.Add(Clone(file));
                }
                else if (current is null && input.Confirm)
                    await confirmation.ConfirmAsync(row, token);
                ReservationConfirmation.RecordAmount(row);

                if (current is null) repository.Reservations.Add(row);
                else await SaveRevisionRowAsync(row, input.Version!, token);

                if (changedPrices)
                {
                    accepted.OrganizationId = row.OrganizationId;
                    accepted.ReservationId = id;
                    accepted.Number = (prior?.Number ?? 0) + 1;
                    accepted.CreatedAt = now;
                    accepted.ActorId = access.Actor;
                    foreach (var night in accepted.Nights) night.OrganizationId = row.OrganizationId;
                    repository.Revisions.Add(accepted);
                }
                audit.Record(history is not null ? "reservation_history_entered" :
                    current is not null ? "reservation_revised" : "reservation_created", "Reservation", id, oldValues,
                    JsonSerializer.Serialize(new
                    {
                        State = AuditState(row, changedPrices ? accepted.Number : prior?.Number),
                        GuestChanged = guestChanged, ExternalReferenceChanged = referenceChanged, NotesChanged = notesChanged,
                        EvidenceAttached = history is not null
                    }));
            }, token);
        }
        catch (Exception error) when (error.GetBaseException() is SqlException { Number: 2601 or 2627 } sql &&
            sql.Message.Contains("UX_Reservation_Organization_Source_ExternalBookingId", StringComparison.Ordinal))
        {
            throw new SecurityRequestException(409, "This external reference is already recorded for the booking source.",
                "external_booking_reference_duplicate");
        }
        return id;
    }

    private async Task ValidateRevisionAsync(Reservation original, Reservation current, ReservationWrite input, CancellationToken token)
    {
        if (current.PropertyId != original.PropertyId || input.Version is not { Length: 8 } version || !current.RowVersion.SequenceEqual(version))
            throw AvailabilityTransactions.Stale();
        if (current.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw new SecurityRequestException(409, "This reservation is protected from general editing.", "reservation_edit_not_allowed");
        if (input.Confirm)
            throw new SecurityRequestException(409, "Confirmation requires its dedicated workflow.", "reservation_confirmation_not_allowed");
        if (current.Status == ReservationStatus.Confirmed && current.PropertyId != input.PropertyId)
            throw new SecurityRequestException(409, "Only Pending reservations can move.", "reservation_move_not_allowed");
        if (current.Status == ReservationStatus.Confirmed && (current.Source != input.Source || current.GuestId != input.GuestId ||
            current.ExternalBookingId != GuestManagement.Optional(input.ExternalBookingId)))
            throw new SecurityRequestException(409, "Confirmed guest, source and external reference are fixed.", "reservation_identity_protected");
        if ((current.PropertyId != input.PropertyId || current.CheckInDate != input.CheckInDate ||
             current.CheckOutDate != input.CheckOutDate) && await repository.Database.Set<ReservationNight>()
                .AnyAsync(row => row.ReservationId == current.Id, token))
            throw new SecurityRequestException(409, "Existing financial nights are protected.", "reservation_nightly_history_protected");
        await availability.ProtectDatesAsync(current.PropertyId, new(current.CheckInDate, current.CheckOutDate), token);
    }

    private async Task SaveRevisionRowAsync(Reservation row, byte[] version, CancellationToken token)
    {
        // A Pending move can modify the alternate key only when no financial nights depend on it.
        var count = await repository.Reservations.Where(item => item.Id == row.Id && item.RowVersion == version)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.PropertyId, row.PropertyId)
                .SetProperty(item => item.GuestId, row.GuestId).SetProperty(item => item.Source, row.Source)
                .SetProperty(item => item.ExternalBookingId, row.ExternalBookingId)
                .SetProperty(item => item.CheckInDate, row.CheckInDate).SetProperty(item => item.CheckOutDate, row.CheckOutDate)
                .SetProperty(item => item.Adults, row.Adults).SetProperty(item => item.Children, row.Children)
                .SetProperty(item => item.RevenueInputMode, row.RevenueInputMode).SetProperty(item => item.GrossAmount, row.GrossAmount)
                .SetProperty(item => item.PlatformFees, row.PlatformFees).SetProperty(item => item.PlatformFeeRate, (decimal?)null)
                .SetProperty(item => item.NetPlatformRevenue, row.NetPlatformRevenue)
                .SetProperty(item => item.CommissionAmount, row.CommissionAmount)
                .SetProperty(item => item.Notes, row.Notes).SetProperty(item => item.UpdatedAt, row.UpdatedAt), token);
        if (count != 1) throw AvailabilityTransactions.Stale();
    }

    private static void Apply(Reservation row, ReservationWrite input, string? external, ReservationPriceRevision price, DateTime now)
    {
        row.PropertyId = input.PropertyId;
        row.GuestId = input.GuestId;
        row.Source = input.Source;
        row.ExternalBookingId = external;
        row.CheckInDate = input.CheckInDate;
        row.CheckOutDate = input.CheckOutDate;
        row.Adults = input.Adults;
        row.Children = input.Children;
        row.RevenueInputMode = price.Mode;
        row.GrossAmount = price.GrossAmount;
        row.PlatformFees = price.PlatformFees;
        row.PlatformFeeRate = null;
        row.NetPlatformRevenue = price.NetAmount;
        row.Notes = GuestManagement.Optional(input.Notes);
        row.UpdatedAt = now;
    }

    private async Task<Reservation> FindAsync(Guid id, CancellationToken token)
    {
        access.EnsureBookingManager();
        return await repository.Reservations.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, token)
            ?? throw new SecurityRequestException(404, "Reservation not found.", "reservation_not_found");
    }

    private IQueryable<ReservationPriceRevision> RevisionRows(Guid id) =>
        repository.Revisions.AsNoTracking().Where(row => row.ReservationId == id);

    private Task<ReservationPriceRevision?> LatestAsync(Guid id, CancellationToken token) =>
        RevisionRows(id).OrderByDescending(row => row.Number).Include(row => row.Nights).FirstOrDefaultAsync(token);

    private void ValidateHistory(HistoricalReservation history)
    {
        var now = clock.GetUtcNow();
        var today = PropertyAvailability.BusinessDate(now);
        var arrival = history.ActualCheckInDate ?? (history.ActualCheckInAt is { } start ? PropertyAvailability.BusinessDate(start) : null);
        var departure = history.ActualCheckOutDate ?? (history.ActualCheckOutAt is { } end ? PropertyAvailability.BusinessDate(end) : null);
        if (history.ConfirmationDate == default || history.ConfirmationDate > history.Booking.CheckInDate ||
            history.Booking.CheckOutDate > today || string.IsNullOrWhiteSpace(history.Reason) || history.Reason.Length > 1000 ||
            arrival < history.ConfirmationDate || arrival > today || departure > today || departure < arrival ||
            history.ActualCheckInAt > now || history.ActualCheckOutAt > now ||
            history.ActualCheckOutAt != null && (history.ActualCheckInAt == null || history.ActualCheckOutAt < history.ActualCheckInAt) ||
            history.ActualCheckInAt is { } checkIn && arrival != PropertyAvailability.BusinessDate(checkIn) ||
            history.ActualCheckOutAt is { } checkOut && departure != PropertyAvailability.BusinessDate(checkOut))
            throw new SecurityRequestException(400, "Supply valid historical dates and the entry reason.", "historical_dates_invalid");
    }

    private static StoredFile Clone(StoredFile file) => new()
    {
        Id = file.Id, OrganizationId = file.OrganizationId, UploaderId = file.UploaderId, StorageKey = file.StorageKey,
        OriginalName = file.OriginalName, ContentType = file.ContentType, ContentHash = file.ContentHash,
        SizeBytes = file.SizeBytes, ScanStatus = file.ScanStatus, UploadedAt = file.UploadedAt
    };

    private static object AuditState(Reservation row, int? priceRevision) => new
    {
        row.PropertyId, row.Source, row.Status, row.CheckInDate, row.CheckOutDate, row.Adults, row.Children,
        row.ConfirmationDate, PriceRevision = priceRevision
    };
}
