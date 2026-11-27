using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using static HostGrid.Api.Repositories.PortfolioRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HostGrid.Api.Services;

public sealed class PropertyConfigurationService(PortfolioRepository repository, PortfolioAccess access,
    PropertyPortfolioService properties, AuthenticationRepository locks, PropertyFiles files, TimeProvider clock)
{
    private HostGrid.Api.Data.HostGridDbContext Db => repository.Database;

    internal async Task ContractAsync(Guid id, ContractWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        var row = new ManagementContract { OrganizationId = property.OrganizationId, PropertyId = id, OwnerId = property.OwnerId };
        await ApplyContractAsync(row, input, token);
        Db.Add(row);
        await FinishAsync(property, "contract_created", transaction, token);
    }

    internal async Task UpdateContractAsync(Guid id, Guid contractId, ContractWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        var row = await ContractRowAsync(id, contractId, token);
        PortfolioVersion.Match(row.RowVersion, input.Version);
        var today = PropertyPortfolioService.Today;
        if (row.ArchivedAt.HasValue || row.StartDate <= today || input.StartDate <= today)
            throw new SecurityRequestException(409, "Only future contracts can be edited.");
        await EnsureOpenPeriodAsync(id, new(row.StartDate, row.EndDate), token);
        await ApplyContractAsync(row, input, token);
        await FinishAsync(property, "contract_updated", transaction, token);
    }

    private async Task ApplyContractAsync(ManagementContract contract, ContractWrite input, CancellationToken token)
    {
        var id = contract.PropertyId;
        if (await Db.Set<ManagementContract>().AnyAsync(row => row.PropertyId == id && row.Id != contract.Id &&
            (input.EndDate == null || row.StartDate < input.EndDate) &&
            (row.EndDate == null || input.StartDate < row.EndDate), token))
            throw new SecurityRequestException(409, "Contract dates overlap an existing contract.");
        await EnsureOpenPeriodAsync(id, new(input.StartDate, input.EndDate), token);
        if (input.DocumentId is Guid linkId)
        {
            var document = await files.RequireDocumentAttachmentAsync(id, linkId, token);
            if (contract.FileId is Guid existing && existing != document.FileId)
                throw new SecurityRequestException(409, "The attached agreement must be retained.");
            document.Type = PropertyDocumentType.Contract;
            contract.FileId = document.FileId;
        }
        contract.StartDate = input.StartDate;
        contract.EndDate = input.EndDate;
        contract.Rate = input.Rate ?? await properties.EffectiveCommissionAsync(id, input.StartDate, token);
    }

    internal async Task CloseContractAsync(Guid id, Guid contractId, ContractEndWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        var row = await ContractRowAsync(id, contractId, token);
        PortfolioVersion.Match(row.RowVersion, input.Version);
        if (row.ArchivedAt.HasValue || row.EndDate is not null || input.EndDate <= row.StartDate ||
            input.EndDate <= PropertyPortfolioService.Today)
            throw new SecurityRequestException(409, "Only open contracts can be ended on a future date after their start.");
        await EnsureOpenPeriodAsync(id, new(input.EndDate, null), token);
        row.EndDate = input.EndDate;
        await FinishAsync(property, "contract_ended", transaction, token);
    }

    internal async Task ArchiveContractAsync(Guid id, Guid contractId, VersionWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        var row = await ContractRowAsync(id, contractId, token);
        PortfolioVersion.Match(row.RowVersion, input.Version);
        if (row.ArchivedAt.HasValue || row.EndDate is not DateOnly end || end > PropertyPortfolioService.Today)
            throw new SecurityRequestException(409, "Only ended contracts can be archived.");
        row.ArchivedAt = clock.GetUtcNow().UtcDateTime;
        await FinishAsync(property, "contract_archived", transaction, token);
    }

    internal async Task CommissionAsync(Guid id, CommissionWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        if (input.EffectiveFrom < PropertyPortfolioService.Today)
            throw new SecurityRequestException(409, "Retroactive commission changes require an approved correction policy.");
        await EnsureOpenPeriodAsync(id, new(input.EffectiveFrom, null), token);
        var previous = await repository.CommissionRates(id)
            .OrderByDescending(row => row.EffectiveFrom).FirstOrDefaultAsync(token);
        if (previous is not null)
        {
            if (input.EffectiveFrom <= previous.EffectiveFrom || previous.EffectiveTo > input.EffectiveFrom)
                throw new SecurityRequestException(409, "New commission rates must follow existing history.");
            previous.EffectiveTo ??= input.EffectiveFrom;
        }
        Db.Add(new CommissionRateHistory { OrganizationId = property.OrganizationId, PropertyId = id,
            EffectiveFrom = input.EffectiveFrom, Rate = input.Rate });
        await FinishAsync(property, "commission_changed", transaction, token);
    }

    private async Task EnsureOpenPeriodAsync(Guid id, ContractPeriod period, CancellationToken token)
    {
        if (await repository.HasClosedPeriodAsync(id, period, token))
            throw new SecurityRequestException(409, "The change would affect a closed financial period.");
    }

    private async Task<ManagementContract> ContractRowAsync(Guid propertyId, Guid contractId, CancellationToken token)
        => await Db.Set<ManagementContract>().SingleOrDefaultAsync(row => row.PropertyId == propertyId &&
            row.Id == contractId, token) ?? throw Missing();

    internal async Task AmenitiesAsync(Guid id, AmenitiesWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        PortfolioVersion.Match(property.RowVersion, input.Version);
        var requested = input.Ids.ToHashSet();
        if (requested.Count != input.Ids.Length ||
            await Db.Set<Amenity>().CountAsync(row => requested.Contains(row.Id), token) != requested.Count)
            throw new SecurityRequestException(400, "Select valid amenities once each.");
        var existing = await Db.Set<PropertyAmenity>().Where(row => row.PropertyId == id).ToListAsync(token);
        Db.RemoveRange(existing.Where(row => !requested.Contains(row.AmenityId)));
        foreach (var amenity in requested.Except(existing.Select(row => row.AmenityId)))
            Db.Add(new PropertyAmenity { OrganizationId = property.OrganizationId, PropertyId = id, AmenityId = amenity });
        await FinishAsync(property, "amenities_changed", transaction, token);
    }

    internal async Task InventoryAsync(Guid id, Guid? itemId, InventoryWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        var row = itemId is Guid key ? await InventoryItemAsync(id, key, token) : new PropertyInventoryItem
        {
            OrganizationId = property.OrganizationId, PropertyId = id
        };
        if (itemId.HasValue) PortfolioVersion.Match(row.RowVersion, input.Version);
        else Db.Add(row);
        row.Name = input.Name.Trim();
        row.Quantity = input.Quantity;
        row.Condition = input.Condition?.Trim();
        row.Notes = input.Notes?.Trim();
        await FinishAsync(property, itemId.HasValue ? "inventory_updated" : "inventory_added", transaction, token);
    }

    internal async Task RemoveInventoryAsync(Guid id, Guid item, byte[] version, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        var row = await InventoryItemAsync(id, item, token);
        PortfolioVersion.Match(row.RowVersion, version);
        Db.Remove(row);
        await FinishAsync(property, "inventory_removed", transaction, token);
    }

    internal async Task SeasonAsync(Guid id, Guid? rateId, SeasonWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        if (input.StartDate < PropertyPortfolioService.Today)
            throw new SecurityRequestException(409, "New pricing periods cannot start in the past.");
        var overlap = await Db.Set<SeasonalRate>().Where(row => row.PropertyId == id && row.Id != rateId &&
            row.Priority == input.Priority && row.StartDate < input.EndDate && input.StartDate < row.EndDate).ToListAsync(token);
        if (overlap.Any(row => PropertyPricing.Overlaps(row, input)))
            throw new SecurityRequestException(409, "An equal-priority price applies to the same night.");
        var rate = rateId is Guid key
            ? await Db.Set<SeasonalRate>().SingleOrDefaultAsync(row => row.PropertyId == id && row.Id == key, token) ?? throw Missing()
            : new SeasonalRate { OrganizationId = property.OrganizationId, PropertyId = id };
        if (rateId.HasValue)
        {
            PortfolioVersion.Match(rate.RowVersion, input.Version);
            if (rate.StartDate <= PropertyPortfolioService.Today)
                throw new SecurityRequestException(409, "Past or effective pricing rules are retained.");
        }
        else Db.Add(rate);
        rate.StartDate = input.StartDate;
        rate.EndDate = input.EndDate;
        rate.DaysOfWeek = input.DaysOfWeek;
        rate.NightlyRate = input.NightlyRate;
        rate.Priority = input.Priority;
        await FinishAsync(property, rateId.HasValue ? "season_updated" : "season_created", transaction, token);
    }

    internal async Task BaseRateAsync(Guid id, BaseRateWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        PortfolioVersion.Match(property.RowVersion, input.Version);
        property.BaseNightlyRate = input.NightlyRate!.Value;
        await FinishAsync(property, "base_rate_changed", transaction, token);
    }

    internal async Task ListingAsync(Guid id, BookingSource source, ListingWrite input, CancellationToken token)
    {
        if (!Enum.IsDefined(source)) throw new SecurityRequestException(400, "Invalid listing source.");
        var property = await access.PropertyAsync(id, true, token);
        await using var transaction = await LockEditableAsync(property, token);
        var listing = await Db.Set<PropertyListing>().SingleOrDefaultAsync(row => row.PropertyId == id && row.Source == source, token);
        if (listing is null)
        {
            if (input.Version is not null) throw Missing();
            listing = new PropertyListing { OrganizationId = property.OrganizationId, PropertyId = id, Source = source };
            Db.Add(listing);
        }
        else PortfolioVersion.Match(listing.RowVersion, input.Version);
        listing.Url = string.IsNullOrWhiteSpace(input.Url) ? null : input.Url.Trim();
        listing.ExternalReference = string.IsNullOrWhiteSpace(input.ExternalReference) ? null : input.ExternalReference.Trim();
        await FinishAsync(property, "listing_saved", transaction, token);
    }

    private async Task<IDbContextTransaction> LockEditableAsync(Property property, CancellationToken token)
    {
        var transaction = await locks.LockAsync("property:" + property.Id, token);
        try
        {
            await Db.Entry(property).ReloadAsync(token);
            if (property.Status == PropertyStatus.Archived)
                throw new SecurityRequestException(409, "Reactivate this property first.");
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private async Task FinishAsync(Property property, string action, IDbContextTransaction transaction, CancellationToken token)
    {
        await properties.SaveAsync(property, action, token);
        await transaction.CommitAsync(token);
    }

    private async Task<PropertyInventoryItem> InventoryItemAsync(Guid id, Guid item, CancellationToken token)
        => await Db.Set<PropertyInventoryItem>().SingleOrDefaultAsync(row => row.Id == item && row.PropertyId == id, token)
            ?? throw Missing();

    private static SecurityRequestException Missing() => new(404, "Property resource not found.");
}
