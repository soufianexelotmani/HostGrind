using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUser", "identity");
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.ActivatedAt).HasColumnType("datetime2");
        builder.HasIndex(entity => entity.NormalizedEmail).IsUnique();
    }
}

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("ApplicationRole", "identity");
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasData(ReferenceData.Roles);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken", "identity");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.TokenHash).HasMaxLength(64);
        builder.Property(entity => entity.SecurityStampHash).HasMaxLength(64);
        builder.Property(entity => entity.ExpiresAt).HasColumnType("datetime2");
        builder.Property(entity => entity.RevokedAt).HasColumnType("datetime2");
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RefreshToken>().WithMany()
            .HasForeignKey(entity => entity.ReplacedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.TokenHash).IsUnique();
        builder.HasIndex(entity => entity.FamilyId);
        builder.HasIndex(entity => new { entity.UserId, entity.ExpiresAt });
    }
}

internal sealed class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences", "identity", table =>
        {
            table.HasCheckConstraint("CK_UserPreferences_Theme", "[Theme] BETWEEN 0 AND 2");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Language).HasMaxLength(10);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.UserId).IsUnique();
    }
}
