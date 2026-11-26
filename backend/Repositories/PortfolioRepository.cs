using HostGrid.Api.Data;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Repositories;

public sealed class PortfolioRepository(HostGridDbContext database)
{
    internal HostGridDbContext Database => database;
    internal DbSet<Owner> Owners => database.Set<Owner>();
    internal DbSet<Property> Properties => database.Set<Property>();

    internal IQueryable<OwnerView> OwnerRows()
        => Owners.AsNoTracking().Select(owner => new OwnerView(owner.Id, owner.Reference, owner.Name, owner.Email,
            owner.Phone, owner.UserId, owner.IsArchived,
            owner.IsActive && database.Set<OrganizationMember>().Any(member => member.UserId == owner.UserId &&
                member.IsActive && member.Role == OrganizationRole.Owner) &&
                database.Users.Any(user => user.Id == owner.UserId && (user.IsActive || !user.EmailConfirmed)),
            database.Users.Any(user => user.Id == owner.UserId && user.EmailConfirmed), owner.RowVersion));

    internal IQueryable<PropertySummary> PropertyRows(IQueryable<Property> properties)
        => from property in properties.AsNoTracking()
           join owner in Owners.AsNoTracking() on property.OwnerId equals owner.Id
           join type in database.Set<PropertyType>().AsNoTracking() on property.TypeId equals type.Id
           select new PropertySummary(property.Id, property.Reference, property.Name, property.City, owner.Id, owner.Name,
               type.Id, type.Code, property.Status, property.OperationalStatus, property.BaseNightlyRate,
               (from photo in database.Set<PropertyPhoto>()
                join file in database.Set<StoredFile>() on photo.FileId equals file.Id
                where photo.PropertyId == property.Id && file.ArchivedAt == null && file.ScanStatus != FileScanStatus.Rejected
                orderby photo.SortOrder, photo.Id
                select (Guid?)photo.Id).FirstOrDefault());

    internal static async Task<Page<T>> PageAsync<T>(IQueryable<T> source, int page, int size, CancellationToken token)
    {
        page = Math.Clamp(page, 1, 100000);
        size = Math.Clamp(size, 1, 100);
        var total = await source.CountAsync(token);
        return new(await source.Skip((page - 1) * size).Take(size).ToListAsync(token), total, page, size);
    }
}
