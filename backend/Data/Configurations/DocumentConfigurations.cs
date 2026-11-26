using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFile", "document", table =>
        {
            table.HasCheckConstraint("CK_StoredFile_Size", "[SizeBytes] > 0");
            table.HasCheckConstraint("CK_StoredFile_ScanStatus", "[ScanStatus] BETWEEN 0 AND 2");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.StorageKey).HasMaxLength(200);
        builder.Property(entity => entity.OriginalName).HasMaxLength(255);
        builder.Property(entity => entity.ContentType).HasMaxLength(127);
        builder.Property(entity => entity.ContentHash).HasMaxLength(64);
        builder.Property(entity => entity.UploadedAt).HasColumnType("datetime2");
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.UploaderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.StorageKey }).IsUnique();
    }
}

internal sealed class PropertyDocumentConfiguration : IEntityTypeConfiguration<PropertyDocument>
{
    public void Configure(EntityTypeBuilder<PropertyDocument> builder)
    {
        builder.ToTable("PropertyDocument", "document", table =>
        {
            table.HasCheckConstraint("CK_PropertyDocument_Type", "[Type] BETWEEN 0 AND 1");
        });
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
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.FileId, entity.Type }).IsUnique();
    }
}
