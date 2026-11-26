using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Expense;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategory", "expense");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Name).HasMaxLength(120);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Name }).IsUnique();
    }
}

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expense", "expense", table =>
        {
            table.HasCheckConstraint("CK_Expense_RecurringOccurrence", "([RecurringTemplateId] IS NULL AND [OccurrenceDate] IS NULL) OR ([RecurringTemplateId] IS NOT NULL AND [OccurrenceDate] IS NOT NULL)");
            table.HasCheckConstraint("CK_Expense_Amount", "[Amount] >= 0");
            table.HasCheckConstraint("CK_Expense_Status", "[Status] BETWEEN 0 AND 2");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Reference).HasMaxLength(40);
        builder.Property(entity => entity.Title).HasMaxLength(200);
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.InvoiceNumber).HasMaxLength(100);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExpenseCategory>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.CategoryId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RecurringExpenseTemplate>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.RecurringTemplateId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id, entity.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Reference }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.ExpenseDate });
        builder.HasIndex(entity => new { entity.OrganizationId, entity.RecurringTemplateId, entity.OccurrenceDate }).IsUnique().HasFilter("[RecurringTemplateId] IS NOT NULL");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Status, entity.DueDate });
    }
}

internal sealed class ExpensePaymentConfiguration : IEntityTypeConfiguration<ExpensePayment>
{
    public void Configure(EntityTypeBuilder<ExpensePayment> builder)
    {
        builder.ToTable("ExpensePayment", "expense", table =>
        {
            table.HasCheckConstraint("CK_ExpensePayment_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_ExpensePayment_Payer", "[Payer] BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_ExpensePayment_Method", "[Method] BETWEEN 0 AND 3");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.Reference).HasMaxLength(100);
        builder.HasOne<Expense>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ExpenseId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExpenseAttachmentConfiguration : IEntityTypeConfiguration<ExpenseAttachment>
{
    public void Configure(EntityTypeBuilder<ExpenseAttachment> builder)
    {
        builder.ToTable("ExpenseAttachment", "expense");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Expense>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ExpenseId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.FileId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.ExpenseId, entity.FileId }).IsUnique();
    }
}

internal sealed class RecurringExpenseTemplateConfiguration : IEntityTypeConfiguration<RecurringExpenseTemplate>
{
    public void Configure(EntityTypeBuilder<RecurringExpenseTemplate> builder)
    {
        builder.ToTable("RecurringExpenseTemplate", "expense", table =>
        {
            table.HasCheckConstraint("CK_RecurringExpenseTemplate_Amount", "[Amount] >= 0");
            table.HasCheckConstraint("CK_RecurringExpenseTemplate_Cadence", "[Cadence] BETWEEN 0 AND 3");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Title).HasMaxLength(200);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExpenseCategory>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.CategoryId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.IsActive, entity.NextDueDate });
    }
}

internal sealed class ExpenseReimbursementConfiguration : IEntityTypeConfiguration<ExpenseReimbursement>
{
    public void Configure(EntityTypeBuilder<ExpenseReimbursement> builder)
    {
        builder.ToTable("ExpenseReimbursement", "expense", table =>
        {
            table.HasCheckConstraint("CK_ExpenseReimbursement_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_ExpenseReimbursement_Beneficiary", "[Beneficiary] BETWEEN 0 AND 4");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.Reference).HasMaxLength(100);
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(100);
        builder.HasOne<ExpensePayment>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ExpensePaymentId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.IdempotencyKey }).IsUnique();
    }
}
