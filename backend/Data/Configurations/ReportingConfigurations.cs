using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Entities.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Report", "reporting", table =>
        {
            table.HasCheckConstraint("CK_Report_Type", "[Type] BETWEEN 0 AND 6");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2");
        builder.HasOne<OrganizationMember>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.UserId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.UserId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Owner>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.OwnerId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.FileId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.UserId, entity.CreatedAt });
    }
}

internal sealed class DashboardPreferenceConfiguration : IEntityTypeConfiguration<DashboardPreference>
{
    public void Configure(EntityTypeBuilder<DashboardPreference> builder)
    {
        builder.ToTable("DashboardPreference", "reporting");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.HasOne<OrganizationMember>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.UserId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.UserId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.UserId }).IsUnique();
    }
}
