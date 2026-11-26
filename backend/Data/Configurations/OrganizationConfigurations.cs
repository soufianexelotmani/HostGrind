using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organization", "organization");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Reference).HasMaxLength(40);
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.HasIndex(entity => entity.Reference).IsUnique();
    }
}

internal sealed class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("OrganizationMember", "organization", table =>
        {
            table.HasCheckConstraint("CK_OrganizationMember_Role", "[Role] BETWEEN 0 AND 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.UserId }).IsUnique();
    }
}

internal sealed class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationSettings> builder)
    {
        builder.ToTable("OrganizationSettings", "organization", table =>
        {
            table.HasCheckConstraint("CK_OrganizationSettings_DefaultCommissionRate", "[DefaultCommissionRate] BETWEEN 0 AND 100");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.BrandName).HasMaxLength(200);
        builder.Property(entity => entity.Language).HasMaxLength(10);
        builder.Property(entity => entity.DefaultCommissionRate).HasPrecision(5, 2);
        builder.Property(entity => entity.DefaultCommissionRate).HasDefaultValue(20m);
        builder.HasOne<StoredFile>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.LogoFileId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.OrganizationId).IsUnique();
    }
}

internal sealed class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("Owner", "organization");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Reference).HasMaxLength(40);
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.Property(entity => entity.Email).HasMaxLength(254);
        builder.Property(entity => entity.Phone).HasMaxLength(32);
        builder.HasOne<OrganizationMember>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.UserId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.UserId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.UserId }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Reference }).IsUnique();
    }
}
