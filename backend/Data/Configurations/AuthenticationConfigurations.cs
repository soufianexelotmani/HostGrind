using HostGrid.Api.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class LoginChallengeConfiguration : IEntityTypeConfiguration<LoginChallenge>
{
    public void Configure(EntityTypeBuilder<LoginChallenge> builder)
    {
        builder.ToTable("LoginChallenge", "identity");
        builder.Property(row => row.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(row => row.TokenHash).HasMaxLength(64);
        builder.Property(row => row.SecurityStamp).HasMaxLength(256);
        builder.HasIndex(row => row.TokenHash).IsUnique();
        builder.HasIndex(row => row.UserId).IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(row => row.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvent", "system");
        builder.Property(row => row.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(row => row.Action).HasMaxLength(80);
        builder.Property(row => row.CorrelationId).HasMaxLength(100);
        builder.HasIndex(row => row.OccurredAt);
    }
}
