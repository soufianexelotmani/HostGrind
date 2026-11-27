using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HostGrid.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BookingAvailabilityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionRateSnapshot",
                schema: "booking",
                table: "Reservation",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionAmount",
                schema: "booking",
                table: "Reservation",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailabilityReleasedAt",
                schema: "booking",
                table: "Reservation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AvailabilityReleasedBy",
                schema: "booking",
                table: "Reservation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AvailabilityReleasedFrom",
                schema: "booking",
                table: "Reservation",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_AvailabilityReleasedBy",
                schema: "booking",
                table: "Reservation",
                column: "AvailabilityReleasedBy");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservation_AvailabilityRelease",
                schema: "booking",
                table: "Reservation",
                sql: "([AvailabilityReleasedFrom] IS NULL AND [AvailabilityReleasedAt] IS NULL AND [AvailabilityReleasedBy] IS NULL) OR ([AvailabilityReleasedFrom] IS NOT NULL AND [AvailabilityReleasedAt] IS NOT NULL AND [AvailabilityReleasedBy] IS NOT NULL AND [AvailabilityReleasedFrom] >= [CheckInDate] AND [AvailabilityReleasedFrom] < [CheckOutDate] AND ([Status] = 5 AND [ActualCheckInAt] IS NULL OR [Status] = 3 AND [ActualCheckOutAt] IS NOT NULL))");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_ApplicationUser_AvailabilityReleasedBy",
                schema: "booking",
                table: "Reservation",
                column: "AvailabilityReleasedBy",
                principalSchema: "identity",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [booking].[Reservation]
                    WHERE [CommissionRateSnapshot] IS NULL OR [CommissionAmount] IS NULL OR [AvailabilityReleasedFrom] IS NOT NULL)
                    THROW 51004, 'Cannot downgrade while reservation holds or release history exist.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_ApplicationUser_AvailabilityReleasedBy",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropIndex(
                name: "IX_Reservation_AvailabilityReleasedBy",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservation_AvailabilityRelease",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "AvailabilityReleasedAt",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "AvailabilityReleasedBy",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "AvailabilityReleasedFrom",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionRateSnapshot",
                schema: "booking",
                table: "Reservation",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CommissionAmount",
                schema: "booking",
                table: "Reservation",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
