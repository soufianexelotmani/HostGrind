using HostGrid.Api.Entities.Communication;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class OwnerRequestConfiguration : IEntityTypeConfiguration<OwnerRequest>
{
    public void Configure(EntityTypeBuilder<OwnerRequest> builder)
    {
        builder.ToTable("OwnerRequest", "communication", table =>
        {
            table.HasCheckConstraint("CK_OwnerRequest_Type", "[Type] BETWEEN 0 AND 5");
            table.HasCheckConstraint("CK_OwnerRequest_Status", "[Status] BETWEEN 0 AND 5");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2");
        builder.HasOne<Owner>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.OwnerId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.OwnerId, entity.Status });
    }
}

internal sealed class OwnerRequestMessageConfiguration : IEntityTypeConfiguration<OwnerRequestMessage>
{
    public void Configure(EntityTypeBuilder<OwnerRequestMessage> builder)
    {
        builder.ToTable("OwnerRequestMessage", "communication");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Body).HasMaxLength(4000);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2");
        builder.HasOne<OwnerRequest>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.RequestId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.RequestId, entity.CreatedAt });
    }
}

internal sealed class OwnerRequestStatusHistoryConfiguration : IEntityTypeConfiguration<OwnerRequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<OwnerRequestStatusHistory> builder)
    {
        builder.ToTable("OwnerRequestStatusHistory", "communication", table =>
        {
            table.HasCheckConstraint("CK_OwnerRequestStatusHistory_OldStatus", "[OldStatus] BETWEEN 0 AND 5");
            table.HasCheckConstraint("CK_OwnerRequestStatusHistory_NewStatus", "[NewStatus] BETWEEN 0 AND 5");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.ChangedAt).HasColumnType("datetime2");
        builder.HasOne<OwnerRequest>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.RequestId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.RequestId, entity.ChangedAt });
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notification", "communication");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Type).HasMaxLength(80);
        builder.Property(entity => entity.MessageKey).HasMaxLength(120);
        builder.Property(entity => entity.ReadAt).HasColumnType("datetime2");
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2");
        builder.HasOne<OrganizationMember>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.UserId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.UserId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.UserId, entity.ReadAt, entity.CreatedAt });
    }
}

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreference", "communication");
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

internal sealed class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutbox>
{
    public void Configure(EntityTypeBuilder<EmailOutbox> builder)
    {
        builder.ToTable("EmailOutbox", "communication", table =>
        {
            table.HasCheckConstraint("CK_EmailOutbox_Lease", "[Attempts] >= 0 AND (([LeaseId] IS NULL AND [LeaseUntil] IS NULL) OR ([LeaseId] IS NOT NULL AND [LeaseUntil] IS NOT NULL))");
            table.HasCheckConstraint("CK_EmailOutbox_Status", "[Status] BETWEEN 0 AND 3");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Recipient).HasMaxLength(254);
        builder.Property(entity => entity.Template).HasMaxLength(120);
        builder.Property(entity => entity.NextAttemptAt).HasColumnType("datetime2");
        builder.Property(entity => entity.LeaseUntil).HasColumnType("datetime2");
        builder.Property(entity => entity.SentAt).HasColumnType("datetime2");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Status, entity.NextAttemptAt });
    }
}
