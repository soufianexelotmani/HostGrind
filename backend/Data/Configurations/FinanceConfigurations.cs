using HostGrid.Api.Entities.Finance;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class MonthlyClosureConfiguration : IEntityTypeConfiguration<MonthlyClosure>
{
    public void Configure(EntityTypeBuilder<MonthlyClosure> builder)
    {
        builder.ToTable("MonthlyClosure", "finance", table =>
        {
            table.HasTrigger("ProtectClosedMonthlyClosure");
            table.UseSqlOutputClause(false);
            table.HasCheckConstraint("CK_MonthlyClosure_Period", "[Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12");
            table.HasCheckConstraint("CK_MonthlyClosure_ClosedMetadata", "([Status] = 0 AND [ClosedAt] IS NULL AND [ClosedById] IS NULL) OR ([Status] = 1 AND [ClosedAt] IS NOT NULL AND [ClosedById] IS NOT NULL)");
            table.HasCheckConstraint("CK_MonthlyClosure_Profit", "[OwnerProfit] = [NetPlatformRevenue] - [CommissionAmount] - [OwnerExpenses]");
            table.HasCheckConstraint("CK_MonthlyClosure_GrossAmount", "[GrossAmount] >= 0");
            table.HasCheckConstraint("CK_MonthlyClosure_PlatformFees", "[PlatformFees] >= 0");
            table.HasCheckConstraint("CK_MonthlyClosure_NetPlatformRevenue", "[NetPlatformRevenue] >= 0");
            table.HasCheckConstraint("CK_MonthlyClosure_CommissionAmount", "[CommissionAmount] >= 0");
            table.HasCheckConstraint("CK_MonthlyClosure_OwnerExpenses", "[OwnerExpenses] >= 0");
            table.HasCheckConstraint("CK_MonthlyClosure_Advances", "[Advances] >= 0");
            table.HasCheckConstraint("CK_MonthlyClosure_OwnerDirectPayments", "[OwnerDirectPayments] >= 0");
            table.HasCheckConstraint("CK_MonthlyClosure_Status", "[Status] BETWEEN 0 AND 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.GrossAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.PlatformFees).HasPrecision(18, 2);
        builder.Property(entity => entity.NetPlatformRevenue).HasPrecision(18, 2);
        builder.Property(entity => entity.CommissionAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.OwnerExpenses).HasPrecision(18, 2);
        builder.Property(entity => entity.OwnerProfit).HasPrecision(18, 2);
        builder.Property(entity => entity.Advances).HasPrecision(18, 2);
        builder.Property(entity => entity.OwnerDirectPayments).HasPrecision(18, 2);
        builder.Property(entity => entity.OpeningSettlementBalance).HasPrecision(18, 2);
        builder.Property(entity => entity.PayoutDueAtClosing).HasPrecision(18, 2);
        builder.Property(entity => entity.ClosedAt).HasColumnType("datetime2");
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Owner>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.OwnerId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.ClosedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.Year, entity.Month }).IsUnique();
    }
}

internal sealed class MonthlyClosureLineConfiguration : IEntityTypeConfiguration<MonthlyClosureLine>
{
    public void Configure(EntityTypeBuilder<MonthlyClosureLine> builder)
    {
        builder.ToTable("MonthlyClosureLine", "finance", table =>
        {
            table.HasTrigger("ProtectClosedMonthlyClosureLines");
            table.UseSqlOutputClause(false);
            table.HasCheckConstraint("CK_MonthlyClosureLine_Period", "DAY([PeriodStart]) = 1");
            table.HasCheckConstraint("CK_MonthlyClosureLine_SourceType", "[SourceType] BETWEEN 0 AND 10");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.HasOne<MonthlyClosure>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ClosureId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.SourceType, entity.SourceId, entity.PeriodStart }).IsUnique();
    }
}

internal sealed class CommissionAdjustmentConfiguration : IEntityTypeConfiguration<CommissionAdjustment>
{
    public void Configure(EntityTypeBuilder<CommissionAdjustment> builder)
    {
        builder.ToTable("CommissionAdjustment", "finance", table =>
        {
            table.HasCheckConstraint("CK_CommissionAdjustment_Periods", "DAY([OriginPeriod]) = 1 AND DAY([TargetPeriod]) = 1 AND [OriginPeriod] < [TargetPeriod]");
            table.HasCheckConstraint("CK_CommissionAdjustment_SourceType", "[SourceType] BETWEEN 0 AND 10");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Reason).HasMaxLength(1000);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.TargetPeriod });
    }
}

internal sealed class FinancialAdjustmentConfiguration : IEntityTypeConfiguration<FinancialAdjustment>
{
    public void Configure(EntityTypeBuilder<FinancialAdjustment> builder)
    {
        builder.ToTable("FinancialAdjustment", "finance", table =>
        {
            table.HasCheckConstraint("CK_FinancialAdjustment_Periods", "DAY([OriginPeriod]) = 1 AND DAY([TargetPeriod]) = 1 AND [OriginPeriod] < [TargetPeriod]");
            table.HasCheckConstraint("CK_FinancialAdjustment_SourceType", "[SourceType] BETWEEN 0 AND 10");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Reason).HasMaxLength(1000);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.TargetPeriod });
    }
}

internal sealed class OwnerPayoutConfiguration : IEntityTypeConfiguration<OwnerPayout>
{
    public void Configure(EntityTypeBuilder<OwnerPayout> builder)
    {
        builder.ToTable("OwnerPayout", "finance", table =>
        {
            table.HasCheckConstraint("CK_OwnerPayout_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_OwnerPayout_Method", "[Method] BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_OwnerPayout_Status", "[Status] BETWEEN 0 AND 2");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.Reference).HasMaxLength(100);
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(100);
        builder.HasOne<Owner>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.OwnerId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Reference }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.IdempotencyKey }).IsUnique();
    }
}

internal sealed class OwnerPayoutAllocationConfiguration : IEntityTypeConfiguration<OwnerPayoutAllocation>
{
    public void Configure(EntityTypeBuilder<OwnerPayoutAllocation> builder)
    {
        builder.ToTable("OwnerPayoutAllocation", "finance", table =>
        {
            table.HasCheckConstraint("CK_OwnerPayoutAllocation_Amount", "[Amount] > 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.HasOne<OwnerPayout>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PayoutId, entity.OwnerId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id, entity.OwnerId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MonthlyClosure>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ClosureId, entity.OwnerId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id, entity.OwnerId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PayoutId, entity.ClosureId }).IsUnique();
    }
}

internal sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        builder.ToTable("FinancialTransaction", "finance", table =>
        {
            table.HasCheckConstraint("CK_FinancialTransaction_Type", "[Type] BETWEEN 0 AND 5");
            table.HasCheckConstraint("CK_FinancialTransaction_SourceType", "[SourceType] BETWEEN 0 AND 10");
            table.HasCheckConstraint("CK_FinancialTransaction_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_FinancialTransaction_Payer", "[Payer] BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_FinancialTransaction_Receiver", "[Receiver] BETWEEN 0 AND 4");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(100);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.IdempotencyKey }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Type, entity.SourceType, entity.SourceId }).IsUnique();
    }
}
