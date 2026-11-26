using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog", "system");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Action).HasMaxLength(120);
        builder.Property(entity => entity.ResourceType).HasMaxLength(80);
        builder.Property(entity => entity.CorrelationId).HasMaxLength(100);
        builder.Property(entity => entity.OccurredAt).HasColumnType("datetime2");
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.OccurredAt });
    }
}

internal sealed class BackgroundJobExecutionConfiguration : IEntityTypeConfiguration<BackgroundJobExecution>
{
    public void Configure(EntityTypeBuilder<BackgroundJobExecution> builder)
    {
        builder.ToTable("BackgroundJobExecution", "system", table =>
        {
            table.HasCheckConstraint("CK_BackgroundJobExecution_Lease", "[Attempts] >= 0 AND (([LeaseId] IS NULL AND [LeaseUntil] IS NULL) OR ([LeaseId] IS NOT NULL AND [LeaseUntil] IS NOT NULL))");
            table.HasCheckConstraint("CK_BackgroundJobExecution_Status", "[Status] BETWEEN 0 AND 3");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.JobKey).HasMaxLength(180);
        builder.Property(entity => entity.NextAttemptAt).HasColumnType("datetime2");
        builder.Property(entity => entity.LeaseUntil).HasColumnType("datetime2");
        builder.Property(entity => entity.CompletedAt).HasColumnType("datetime2");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.JobKey }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Status, entity.NextAttemptAt });
    }
}
