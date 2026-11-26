using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HostGrid.Api.Data.Configurations;

internal sealed class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.ToTable("Guest", "booking");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.Property(entity => entity.Phone).HasMaxLength(32);
        builder.Property(entity => entity.Email).HasMaxLength(254);
    }
}

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservation", "booking", table =>
        {
            table.HasCheckConstraint("CK_Reservation_Dates", "[CheckOutDate] > [CheckInDate]");
            table.HasCheckConstraint("CK_Reservation_Guests", "[Adults] > 0 AND [Children] >= 0");
            table.HasCheckConstraint("CK_Reservation_RevenueInput", "([RevenueInputMode] = 2 AND [GrossAmount] IS NULL AND [PlatformFees] IS NULL AND [PlatformFeeRate] IS NULL) OR ([RevenueInputMode] IN (0, 1) AND [GrossAmount] IS NOT NULL AND [PlatformFees] IS NOT NULL AND [GrossAmount] - [PlatformFees] = [NetPlatformRevenue] AND (([RevenueInputMode] = 0 AND [PlatformFeeRate] IS NULL) OR ([RevenueInputMode] = 1 AND [PlatformFeeRate] IS NOT NULL)))");
            table.HasCheckConstraint("CK_Reservation_ActualStay", "[ActualCheckOutAt] IS NULL OR ([ActualCheckInAt] IS NOT NULL AND [ActualCheckOutAt] >= [ActualCheckInAt])");
            table.HasCheckConstraint("CK_Reservation_Source", "[Source] BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_Reservation_GrossAmount", "[GrossAmount] >= 0");
            table.HasCheckConstraint("CK_Reservation_PlatformFees", "[PlatformFees] >= 0");
            table.HasCheckConstraint("CK_Reservation_PlatformFeeRate", "[PlatformFeeRate] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_Reservation_RevenueInputMode", "[RevenueInputMode] BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_Reservation_NetPlatformRevenue", "[NetPlatformRevenue] >= 0");
            table.HasCheckConstraint("CK_Reservation_CommissionRateSnapshot", "[CommissionRateSnapshot] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_Reservation_CommissionAmount", "[CommissionAmount] >= 0");
            table.HasCheckConstraint("CK_Reservation_Status", "[Status] BETWEEN 0 AND 5");
            table.HasCheckConstraint("CK_Reservation_PaymentStatus", "[PaymentStatus] BETWEEN 0 AND 4");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Reference).HasMaxLength(40);
        builder.Property(entity => entity.ExternalBookingId).HasMaxLength(200);
        builder.Property(entity => entity.GrossAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.PlatformFees).HasPrecision(18, 2);
        builder.Property(entity => entity.PlatformFeeRate).HasPrecision(5, 2);
        builder.Property(entity => entity.NetPlatformRevenue).HasPrecision(18, 2);
        builder.Property(entity => entity.CommissionRateSnapshot).HasPrecision(5, 2);
        builder.Property(entity => entity.CommissionAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.ActualCheckInAt).HasColumnType("datetime2");
        builder.Property(entity => entity.ActualCheckOutAt).HasColumnType("datetime2");
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Guest>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.GuestId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Reference }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.Source, entity.ExternalBookingId }).IsUnique().HasFilter("[ExternalBookingId] IS NOT NULL");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.CheckInDate, entity.CheckOutDate });
    }
}

internal sealed class ReservationPaymentConfiguration : IEntityTypeConfiguration<ReservationPayment>
{
    public void Configure(EntityTypeBuilder<ReservationPayment> builder)
    {
        builder.ToTable("ReservationPayment", "booking", table =>
        {
            table.HasCheckConstraint("CK_ReservationPayment_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_ReservationPayment_Receiver", "[Receiver] BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_ReservationPayment_Method", "[Method] BETWEEN 0 AND 3");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.Reference).HasMaxLength(100);
        builder.HasOne<Reservation>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ReservationId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReservationCancellationConfiguration : IEntityTypeConfiguration<ReservationCancellation>
{
    public void Configure(EntityTypeBuilder<ReservationCancellation> builder)
    {
        builder.ToTable("ReservationCancellation", "booking", table =>
        {
            table.HasCheckConstraint("CK_ReservationCancellation_RetainedFee", "[RetainedFee] >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Reason).HasMaxLength(1000);
        builder.Property(entity => entity.CancelledAt).HasColumnType("datetime2");
        builder.Property(entity => entity.RetainedFee).HasPrecision(18, 2);
        builder.HasOne<Reservation>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ReservationId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.ReservationId }).IsUnique();
    }
}

internal sealed class ReservationRefundConfiguration : IEntityTypeConfiguration<ReservationRefund>
{
    public void Configure(EntityTypeBuilder<ReservationRefund> builder)
    {
        builder.ToTable("ReservationRefund", "booking", table =>
        {
            table.HasCheckConstraint("CK_ReservationRefund_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_ReservationRefund_Payer", "[Payer] BETWEEN 0 AND 4");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.Reference).HasMaxLength(100);
        builder.HasOne<Reservation>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ReservationId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReservationPayment>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PaymentId, entity.ReservationId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id, entity.ReservationId }).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReservationStatusHistoryConfiguration : IEntityTypeConfiguration<ReservationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ReservationStatusHistory> builder)
    {
        builder.ToTable("ReservationStatusHistory", "booking", table =>
        {
            table.HasCheckConstraint("CK_ReservationStatusHistory_OldStatus", "[OldStatus] BETWEEN 0 AND 5");
            table.HasCheckConstraint("CK_ReservationStatusHistory_NewStatus", "[NewStatus] BETWEEN 0 AND 5");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.ChangedAt).HasColumnType("datetime2");
        builder.HasOne<Reservation>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ReservationId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(entity => entity.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CalendarBlockConfiguration : IEntityTypeConfiguration<CalendarBlock>
{
    public void Configure(EntityTypeBuilder<CalendarBlock> builder)
    {
        builder.ToTable("CalendarBlock", "booking", table =>
        {
            table.HasCheckConstraint("CK_CalendarBlock_Dates", "[EndDate] > [StartDate]");
            table.HasCheckConstraint("CK_CalendarBlock_Status", "[Status] BETWEEN 0 AND 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.HasOne<Property>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.StartDate, entity.EndDate });
    }
}

internal sealed class ReservationNightConfiguration : IEntityTypeConfiguration<ReservationNight>
{
    public void Configure(EntityTypeBuilder<ReservationNight> builder)
    {
        builder.ToTable("ReservationNight", "booking", table =>
        {
            table.HasCheckConstraint("CK_ReservationNight_NetAmount", "[NetAmount] >= 0");
            table.HasCheckConstraint("CK_ReservationNight_CommissionAmount", "[CommissionAmount] >= 0");
            table.HasCheckConstraint("CK_ReservationNight_CommissionRateSnapshot", "[CommissionRateSnapshot] BETWEEN 0 AND 100");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.HasAlternateKey(entity => new { entity.OrganizationId, entity.Id });
        builder.Property(entity => entity.OrganizationId).IsConcurrencyToken();
        builder.HasOne<Organization>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(entity => entity.NetAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.CommissionAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.CommissionRateSnapshot).HasPrecision(5, 2);
        builder.HasOne<Reservation>().WithMany()
            .HasForeignKey(entity => new { entity.OrganizationId, entity.ReservationId, entity.PropertyId })
            .HasPrincipalKey(entity => new { entity.OrganizationId, entity.Id, entity.PropertyId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.ReservationId, entity.NightDate }).IsUnique();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.PropertyId, entity.NightDate });
    }
}
