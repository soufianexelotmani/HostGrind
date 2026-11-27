using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class PropertyTypeConfiguration : IEntityTypeConfiguration<PropertyType>
{
    public void Configure(EntityTypeBuilder<PropertyType> builder)
    {
        builder.ToTable("PropertyType", "property");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Code).HasMaxLength(32);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasData(ReferenceData.PropertyTypes);
    }
}

internal sealed class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.HasData(new[] { "Wifi", "AirConditioning", "Television", "Kitchen", "Refrigerator",
            "WashingMachine", "Parking", "Elevator", "Balcony" }.Select((code, index) => new Amenity
            {
                Id = Guid.Parse($"50000000-0000-0000-0000-{index + 1:000000000000}"), Code = code
            }));
        builder.ToTable("Amenity", "property");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.Code).HasMaxLength(64);
        builder.HasIndex(entity => entity.Code).IsUnique();
    }
}

internal sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.Property(entity => entity.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.ToTable("Property", "property", table =>
        {
            table.HasCheckConstraint("CK_Property_Capacity", "[MaxGuests] > 0 AND [Bedrooms] >= 0 AND [Bathrooms] >= 0");
            table.HasCheckConstraint("CK_Property_Coordinates", "[Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180");
            table.HasCheckConstraint("CK_Property_BaseNightlyRate", "[BaseNightlyRate] >= 0");
            table.HasCheckConstraint("CK_Property_Status", "[Status] BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_Property_OperationalStatus", "[OperationalStatus] BETWEEN 0 AND 4");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Reference).HasMaxLength(40);
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.Property(entity => entity.Description).HasMaxLength(4000);
        builder.Property(entity => entity.Address).HasMaxLength(500);
        builder.Property(entity => entity.City).HasMaxLength(120);
        builder.Property(entity => entity.Latitude).HasPrecision(9, 6);
        builder.Property(entity => entity.Longitude).HasPrecision(9, 6);
        builder.Property(entity => entity.BaseNightlyRate).HasPrecision(18, 2);
        builder.HasOne<Owner>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.OwnerId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PropertyType>().WithMany()
            .HasForeignKey(entity => entity.TypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Reference }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.OwnerId });
    }
}

internal sealed class PropertyAmenityConfiguration : IEntityTypeConfiguration<PropertyAmenity>
{
    public void Configure(EntityTypeBuilder<PropertyAmenity> builder)
    {
        builder.ToTable("PropertyAmenity", "property");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Amenity>().WithMany()
            .HasForeignKey(entity => entity.AmenityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.AmenityId }).IsUnique();
    }
}

internal sealed class PropertyPhotoConfiguration : IEntityTypeConfiguration<PropertyPhoto>
{
    public void Configure(EntityTypeBuilder<PropertyPhoto> builder)
    {
        builder.ToTable("PropertyPhoto", "property");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.FileId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.FileId }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.SortOrder });
    }
}

internal sealed class PropertyInventoryItemConfiguration : IEntityTypeConfiguration<PropertyInventoryItem>
{
    public void Configure(EntityTypeBuilder<PropertyInventoryItem> builder)
    {
        builder.ToTable("PropertyInventoryItem", "property", table =>
        {
            table.HasCheckConstraint("CK_PropertyInventoryItem_Quantity", "[Quantity] >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.Property(entity => entity.Condition).HasMaxLength(120);
        builder.Property(entity => entity.Notes).HasMaxLength(1000);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.Name }).IsUnique();
    }
}

internal sealed class ManagementContractConfiguration : IEntityTypeConfiguration<ManagementContract>
{
    public void Configure(EntityTypeBuilder<ManagementContract> builder)
    {
        builder.ToTable("ManagementContract", "property", table =>
        {
            table.HasCheckConstraint("CK_ManagementContract_Dates", "[EndDate] > [StartDate]");
            table.HasCheckConstraint("CK_ManagementContract_Rate", "[Rate] BETWEEN 0 AND 100");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Rate).HasPrecision(5, 2);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Owner>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.OwnerId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.FileId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.StartDate });
    }
}

internal sealed class CommissionRateHistoryConfiguration : IEntityTypeConfiguration<CommissionRateHistory>
{
    public void Configure(EntityTypeBuilder<CommissionRateHistory> builder)
    {
        builder.ToTable("CommissionRateHistory", "property", table =>
        {
            table.HasCheckConstraint("CK_CommissionRateHistory_Dates", "[EffectiveTo] > [EffectiveFrom]");
            table.HasCheckConstraint("CK_CommissionRateHistory_Rate", "[Rate] BETWEEN 0 AND 100");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Rate).HasPrecision(5, 2);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.EffectiveFrom }).IsUnique();
    }
}

internal sealed class SeasonalRateConfiguration : IEntityTypeConfiguration<SeasonalRate>
{
    public void Configure(EntityTypeBuilder<SeasonalRate> builder)
    {
        builder.ToTable("SeasonalRate", "property", table =>
        {
            table.HasCheckConstraint("CK_SeasonalRate_Dates", "[EndDate] > [StartDate] AND [DaysOfWeek] BETWEEN 1 AND 127");
            table.HasCheckConstraint("CK_SeasonalRate_NightlyRate", "[NightlyRate] >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.NightlyRate).HasPrecision(18, 2);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.StartDate, entity.EndDate });
    }
}

internal sealed class PropertyListingConfiguration : IEntityTypeConfiguration<PropertyListing>
{
    public void Configure(EntityTypeBuilder<PropertyListing> builder)
    {
        builder.ToTable("PropertyListing", "property", table =>
        {
            table.HasCheckConstraint("CK_PropertyListing_Source", "[Source] BETWEEN 0 AND 2");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Url).HasMaxLength(2048);
        builder.Property(entity => entity.ExternalReference).HasMaxLength(200);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.Source }).IsUnique();
    }
}
