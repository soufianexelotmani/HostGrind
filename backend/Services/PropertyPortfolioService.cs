using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Entities.System;
using HostGrid.Api.Enums;
using HostGrid.Api.Mappers;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class PropertyPortfolioService(PortfolioRepository repository, PortfolioAccess access, PortfolioAudit audit,
    AuthenticationRepository locks, TimeProvider clock, PropertyFiles files)
{
    private HostGrid.Api.Data.HostGridDbContext Database => repository.Database;

    internal Task<Page<PropertySummary>> ListAsync(string? search, Guid? owner, Guid? type, PropertyStatus? status,
        string? sort, int page, int size, CancellationToken token)
    {
        var query = repository.PropertyRows(access.VisibleProperties());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(row => row.Reference.Contains(term) || row.Name.Contains(term) || row.City.Contains(term));
        }
        if (owner.HasValue) query = query.Where(row => row.OwnerId == owner);
        if (type.HasValue) query = query.Where(row => row.TypeId == type);
        if (status.HasValue) query = query.Where(row => row.Status == status);
        var ordered = sort switch
        {
            "-name" => query.OrderByDescending(row => row.Name),
            "reference" => query.OrderBy(row => row.Reference),
            "-reference" => query.OrderByDescending(row => row.Reference),
            "city" => query.OrderBy(row => row.City),
            "-city" => query.OrderByDescending(row => row.City),
            "baseNightlyRate" => query.OrderBy(row => row.BaseNightlyRate),
            "-baseNightlyRate" => query.OrderByDescending(row => row.BaseNightlyRate),
            _ => query.OrderBy(row => row.Name)
        };
        return PortfolioRepository.PageAsync(ordered.ThenBy(row => row.Id), page, size, token);
    }

    internal async Task<PropertyView> GetAsync(Guid id, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, false, token);
        var summary = await repository.PropertyRows(repository.Properties.Where(row => row.Id == id)).SingleAsync(token);
        var documents = await files.ListAsync(id, false, token);
        var contracts = await Database.Set<ManagementContract>().AsNoTracking().Where(row => row.PropertyId == id)
            .OrderByDescending(row => row.StartDate).ToListAsync(token);
        var documentLinks = await Database.Set<PropertyDocument>().AsNoTracking().Where(row => row.PropertyId == id)
            .ToDictionaryAsync(row => row.FileId, row => row.Id, token);
        var history = access.CanManage
            ? await Database.Set<AuditLog>().Where(row => row.ResourceId == id && row.ResourceType == "Property")
                .OrderByDescending(row => row.OccurredAt).Take(100)
                .Select(row => new PortfolioAuditView(row.Action, row.OccurredAt)).ToListAsync(token)
            : [];
        return new(summary, property.Description, property.Address, property.Latitude, property.Longitude, property.MaxGuests,
            property.Bedrooms, property.Bathrooms, property.CheckInTime, property.CheckOutTime, property.CreatedAt,
            property.UpdatedAt, property.RowVersion, await DefaultCommissionAsync(token),
            contracts.Select(row => DescribeContract(row, row.FileId is Guid file && documentLinks.TryGetValue(file, out var link)
                ? documents.SingleOrDefault(doc => doc.Id == link) : null)).ToArray(),
            await Database.Set<CommissionRateHistory>().Where(row => row.PropertyId == id).OrderByDescending(row => row.EffectiveFrom)
                .Select(row => new CommissionView(row.Id, row.EffectiveFrom, row.EffectiveTo, row.Rate)).ToListAsync(token),
            await Database.Set<PropertyInventoryItem>().Where(row => row.PropertyId == id).OrderBy(row => row.Name)
                .Select(row => new InventoryView(row.Id, row.Name, row.Quantity, row.Condition, row.RowVersion)).ToListAsync(token),
            await Database.Set<PropertyAmenity>().Where(row => row.PropertyId == id).Select(row => row.AmenityId).ToListAsync(token),
            await Database.Set<SeasonalRate>().Where(row => row.PropertyId == id).OrderByDescending(row => row.StartDate)
                .Select(row => new SeasonView(row.Id, row.StartDate, row.EndDate, row.DaysOfWeek, row.NightlyRate, row.Priority, row.RowVersion)).ToListAsync(token),
            await Database.Set<PropertyListing>().Where(row => row.PropertyId == id).OrderBy(row => row.Source)
                .Select(row => new ListingView(row.Id, row.Source, row.Url, row.ExternalReference, row.RowVersion)).ToListAsync(token),
            await files.ListAsync(id, true, token), documents, history);
    }

    internal async Task<Guid> CreateAsync(PropertyWrite input, CancellationToken token)
    {
        access.EnsureManager();
        await using var transaction = await locks.LockAsync("owner:" + input.OwnerId, token);
        await EnsureOwnerAsync(input.OwnerId, token);
        await EnsureTypeAsync(input.TypeId, token);
        var now = clock.GetUtcNow().UtcDateTime;
        var property = new Property { Id = Guid.NewGuid(), OwnerId = input.OwnerId, OrganizationId = access.OrganizationId,
            Reference = "PR-" + Guid.NewGuid().ToString("N").ToUpperInvariant(), CreatedAt = now, UpdatedAt = now };
        property.Apply(input);
        repository.Properties.Add(property);
        await audit.SaveAsync("property_created", "Property", property.Id, token);
        await transaction.CommitAsync(token);
        return property.Id;
    }

    internal async Task UpdateAsync(Guid id, PropertyWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(id, true, token);
        PortfolioVersion.Match(property.RowVersion, input.Version);
        if (property.OwnerId != input.OwnerId) throw new SecurityRequestException(409, "Property ownership transfer is unavailable.");
        if (property.Status == PropertyStatus.Archived) throw new SecurityRequestException(409, "Reactivate this property first.");
        await EnsureTypeAsync(input.TypeId, token);
        property.Apply(input);
        await SaveAsync(property, "property_updated", token);
    }

    internal async Task StatusAsync(Guid id, PropertyStatusWrite input, CancellationToken token)
    {
        var existing = await access.PropertyAsync(id, true, token);
        await using var transaction = await locks.LockAsync("owner:" + existing.OwnerId, token);
        await Database.Entry(existing).ReloadAsync(token);
        PortfolioVersion.Match(existing.RowVersion, input.Version);
        if (!Enum.IsDefined(input.Status)) throw new SecurityRequestException(400, "Invalid property status.");
        if (input.Status != PropertyStatus.Archived) await EnsureOwnerAsync(existing.OwnerId, token);
        existing.Status = input.Status;
        await SaveAsync(existing, "property_status_changed", token);
        await transaction.CommitAsync(token);
    }

    internal Task<List<Option>> TypesAsync(CancellationToken token) => Database.Set<PropertyType>()
        .OrderBy(row => row.Code).Select(row => new Option(row.Id, row.Code)).ToListAsync(token);
    internal Task<List<Option>> AmenitiesAsync(CancellationToken token) => Database.Set<Amenity>()
        .OrderBy(row => row.Code).Select(row => new Option(row.Id, row.Code)).ToListAsync(token);

    internal Task<decimal> DefaultCommissionAsync(CancellationToken token) => GetDefaultAsync(token);

    private async Task<decimal> GetDefaultAsync(CancellationToken token)
        => await Database.Set<OrganizationSettings>().Select(row => (decimal?)row.DefaultCommissionRate).SingleOrDefaultAsync(token) ?? 20m;

    internal static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,
        "Africa/Casablanca").DateTime);

    internal static ContractView DescribeContract(ManagementContract row, FileView? document)
    {
        var today = Today;
        return new(row.Id, row.PropertyId, row.StartDate, row.EndDate, row.Rate,
            row.StartDate > today ? "scheduled" : row.EndDate <= today ? "ended" : "active",
            document?.Name, document?.Id, row.RowVersion);
    }

    internal Task SaveAsync(Property property, string action, CancellationToken token)
    {
        property.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        return audit.SaveAsync(action, "Property", property.Id, token);
    }

    private async Task EnsureOwnerAsync(Guid id, CancellationToken token)
    {
        if (!await repository.Owners.AnyAsync(row => row.Id == id && !row.IsArchived, token))
            throw new SecurityRequestException(400, "Select an active owner profile in this organization.");
    }

    private async Task EnsureTypeAsync(Guid id, CancellationToken token)
    {
        if (!await Database.Set<PropertyType>().AnyAsync(row => row.Id == id, token))
            throw new SecurityRequestException(400, "Select a valid property type.");
    }
}
