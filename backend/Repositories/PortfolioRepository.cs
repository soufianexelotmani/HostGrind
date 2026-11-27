using HostGrid.Api.Data;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Finance;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using HostGrid.Api.Integrations.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HostGrid.Api.Repositories;

public sealed class PortfolioRepository(HostGridDbContext database, IOptions<PropertyStorageOptions> storage, IHostEnvironment environment)
{
    private readonly bool readQuarantined = storage.Value.CanReadQuarantined(environment);
    internal HostGridDbContext Database => database;
    internal DbSet<Owner> Owners => database.Set<Owner>();
    internal DbSet<Property> Properties => database.Set<Property>();

    internal IQueryable<CommissionRateHistory> CommissionRates(Guid propertyId) =>
        database.Set<CommissionRateHistory>().Where(row => row.PropertyId == propertyId);

    internal readonly record struct ContractPeriod(DateOnly Start, DateOnly? End);

    internal Task<bool> HasClosedPeriodAsync(Guid propertyId, ContractPeriod period, CancellationToken token)
    {
        var firstYear = (short)period.Start.Year;
        var firstMonth = (short)period.Start.Month;
        var lastDay = period.End?.AddDays(-1) ?? DateOnly.MaxValue;
        var lastYear = (short)lastDay.Year;
        var lastMonth = (short)lastDay.Month;
        return database.Set<MonthlyClosure>().AnyAsync(row => row.PropertyId == propertyId &&
            row.Status == ClosureStatus.Closed &&
            (row.Year > firstYear || row.Year == firstYear && row.Month >= firstMonth) &&
            (row.Year < lastYear || row.Year == lastYear && row.Month <= lastMonth), token);
    }

    // Member assignments let EF compose filters and ordering over the projected fields.
    internal IQueryable<OwnerView> OwnerRows()
        => Owners.AsNoTracking().Select(owner => new OwnerView
        {
            Id = owner.Id,
            Reference = owner.Reference,
            Name = owner.Name,
            Email = owner.Email,
            Phone = owner.Phone,
            UserId = owner.UserId,
            IsArchived = owner.IsArchived,
            AccessEnabled = owner.IsActive && database.Set<OrganizationMember>().Any(member => member.UserId == owner.UserId &&
                member.IsActive && member.Role == OrganizationRole.Owner) &&
                database.Users.Any(user => user.Id == owner.UserId && (user.IsActive || !user.EmailConfirmed)),
            Activated = database.Users.Any(user => user.Id == owner.UserId && user.EmailConfirmed),
            Version = owner.RowVersion
        });

    internal IQueryable<PropertySummary> PropertyRows(IQueryable<Property> properties)
        => from property in properties.AsNoTracking()
           join owner in Owners.AsNoTracking() on property.OwnerId equals owner.Id
           join type in database.Set<PropertyType>().AsNoTracking() on property.TypeId equals type.Id
           select new PropertySummary
           {
               Id = property.Id,
               Reference = property.Reference,
               Name = property.Name,
               City = property.City,
               OwnerId = owner.Id,
               OwnerName = owner.Name,
               TypeId = type.Id,
               Type = type.Code,
               Status = property.Status,
               OperationalStatus = property.OperationalStatus,
               BaseNightlyRate = property.BaseNightlyRate,
               CoverPhotoId = (from photo in database.Set<PropertyPhoto>()
                join file in database.Set<StoredFile>() on photo.FileId equals file.Id
                where photo.PropertyId == property.Id && file.ArchivedAt == null &&
                    (file.ScanStatus == FileScanStatus.Clean || readQuarantined && file.ScanStatus == FileScanStatus.Quarantined)
                orderby photo.SortOrder, photo.Id
                select (Guid?)photo.Id).FirstOrDefault()
           };

    internal static async Task<Page<T>> PageAsync<T>(IQueryable<T> source, int page, int size, CancellationToken token)
    {
        page = Math.Clamp(page, 1, 100000);
        size = Math.Clamp(size, 1, 100);
        var total = await source.CountAsync(token);
        return new(await source.Skip((page - 1) * size).Take(size).ToListAsync(token), total, page, size);
    }
}
