using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HostGrid.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CalendarBlockLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AvailabilityReleasedFrom",
                schema: "booking",
                table: "CalendarBlock",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "booking",
                table: "CalendarBlock",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "booking",
                table: "CalendarBlock",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                schema: "booking",
                table: "CalendarBlock",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Category",
                schema: "booking",
                table: "CalendarBlock",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "booking",
                table: "CalendarBlock",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                schema: "booking",
                table: "CalendarBlock",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "booking",
                table: "CalendarBlock",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "booking",
                table: "CalendarBlock",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                schema: "booking",
                table: "CalendarBlock",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBlock_CancelledByUserId",
                schema: "booking",
                table: "CalendarBlock",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBlock_CreatedByUserId",
                schema: "booking",
                table: "CalendarBlock",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBlock_UpdatedByUserId",
                schema: "booking",
                table: "CalendarBlock",
                column: "UpdatedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CalendarBlock_Category",
                schema: "booking",
                table: "CalendarBlock",
                sql: "[Category] BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CalendarBlock_Release",
                schema: "booking",
                table: "CalendarBlock",
                sql: "[AvailabilityReleasedFrom] IS NULL OR ([Status] = 1 AND [AvailabilityReleasedFrom] >= [StartDate] AND [AvailabilityReleasedFrom] < [EndDate])");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarBlock_ApplicationUser_CancelledByUserId",
                schema: "booking",
                table: "CalendarBlock",
                column: "CancelledByUserId",
                principalSchema: "identity",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarBlock_ApplicationUser_CreatedByUserId",
                schema: "booking",
                table: "CalendarBlock",
                column: "CreatedByUserId",
                principalSchema: "identity",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarBlock_ApplicationUser_UpdatedByUserId",
                schema: "booking",
                table: "CalendarBlock",
                column: "UpdatedByUserId",
                principalSchema: "identity",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBlock_ApplicationUser_CancelledByUserId",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBlock_ApplicationUser_CreatedByUserId",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarBlock_ApplicationUser_UpdatedByUserId",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropIndex(
                name: "IX_CalendarBlock_CancelledByUserId",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropIndex(
                name: "IX_CalendarBlock_CreatedByUserId",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropIndex(
                name: "IX_CalendarBlock_UpdatedByUserId",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CalendarBlock_Category",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CalendarBlock_Release",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "AvailabilityReleasedFrom",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "booking",
                table: "CalendarBlock");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                schema: "booking",
                table: "CalendarBlock");
        }
    }
}
