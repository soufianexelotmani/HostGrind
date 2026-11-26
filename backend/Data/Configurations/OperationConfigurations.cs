using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Expense;
using HostGrid.Api.Entities.Operation;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class CleaningTaskConfiguration : IEntityTypeConfiguration<CleaningTask>
{
    public void Configure(EntityTypeBuilder<CleaningTask> builder)
    {
        builder.ToTable("CleaningTask", "operation", table =>
        {
            table.HasCheckConstraint("CK_CleaningTask_AutomaticReservation", "[IsAutomatic] = 0 OR [ReservationId] IS NOT NULL");
            table.HasCheckConstraint("CK_CleaningTask_Status", "[Status] BETWEEN 0 AND 3");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.ScheduledAt).HasColumnType("datetime2");
        builder.Property(entity => entity.CompletedAt).HasColumnType("datetime2");
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Reservation>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ReservationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id, entity.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalContact>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContactId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.ReservationId }).IsUnique().HasFilter("[IsAutomatic] = 1");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.ScheduledAt });
    }
}

internal sealed class ChecklistTemplateConfiguration : IEntityTypeConfiguration<ChecklistTemplate>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplate> builder)
    {
        builder.ToTable("ChecklistTemplate", "operation");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ChecklistTemplateItemConfiguration : IEntityTypeConfiguration<ChecklistTemplateItem>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplateItem> builder)
    {
        builder.ToTable("ChecklistTemplateItem", "operation");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Label).HasMaxLength(500);
        builder.HasOne<ChecklistTemplate>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.TemplateId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.TemplateId, entity.SortOrder }).IsUnique();
    }
}

internal sealed class CleaningChecklistItemConfiguration : IEntityTypeConfiguration<CleaningChecklistItem>
{
    public void Configure(EntityTypeBuilder<CleaningChecklistItem> builder)
    {
        builder.ToTable("CleaningChecklistItem", "operation");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Label).HasMaxLength(500);
        builder.HasOne<CleaningTask>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.CleaningTaskId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.CleaningTaskId, entity.SortOrder }).IsUnique();
    }
}

internal sealed class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicket> builder)
    {
        builder.ToTable("MaintenanceTicket", "operation", table =>
        {
            table.HasCheckConstraint("CK_MaintenanceTicket_Priority", "[Priority] BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_MaintenanceTicket_Status", "[Status] BETWEEN 0 AND 3");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Title).HasMaxLength(200);
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.OpenedAt).HasColumnType("datetime2");
        builder.Property(entity => entity.ResolvedAt).HasColumnType("datetime2");
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalContact>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContactId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.Status });
    }
}

internal sealed class ExternalContactConfiguration : IEntityTypeConfiguration<ExternalContact>
{
    public void Configure(EntityTypeBuilder<ExternalContact> builder)
    {
        builder.ToTable("ExternalContact", "operation");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.Property(entity => entity.Phone).HasMaxLength(32);
        builder.Property(entity => entity.Specialty).HasMaxLength(120);
    }
}

internal sealed class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.ToTable("WorkTask", "operation", table =>
        {
            table.HasCheckConstraint("CK_WorkTask_Source", "([Type] = 0 AND [CleaningTaskId] IS NULL AND [MaintenanceTicketId] IS NULL) OR ([Type] = 1 AND [CleaningTaskId] IS NOT NULL AND [MaintenanceTicketId] IS NULL) OR ([Type] = 2 AND [CleaningTaskId] IS NULL AND [MaintenanceTicketId] IS NOT NULL)");
            table.HasCheckConstraint("CK_WorkTask_Type", "[Type] BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_WorkTask_Status", "[Status] BETWEEN 0 AND 3");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Title).HasMaxLength(200);
        builder.Property(entity => entity.DueAt).HasColumnType("datetime2");
        builder.Property(entity => entity.CompletedAt).HasColumnType("datetime2");
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CleaningTask>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.CleaningTaskId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id, entity.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceTicket>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.MaintenanceTicketId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id, entity.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalContact>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ContactId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.Status, entity.DueAt });
    }
}

internal sealed class OperationAttachmentConfiguration : IEntityTypeConfiguration<OperationAttachment>
{
    public void Configure(EntityTypeBuilder<OperationAttachment> builder)
    {
        builder.ToTable("OperationAttachment", "operation");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkTask>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.WorkTaskId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.FileId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.WorkTaskId, entity.FileId }).IsUnique();
    }
}

internal sealed class OperationExpenseLinkConfiguration : IEntityTypeConfiguration<OperationExpenseLink>
{
    public void Configure(EntityTypeBuilder<OperationExpenseLink> builder)
    {
        builder.ToTable("OperationExpenseLink", "operation");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkTask>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.WorkTaskId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Expense>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ExpenseId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.WorkTaskId, entity.ExpenseId }).IsUnique();
    }
}
