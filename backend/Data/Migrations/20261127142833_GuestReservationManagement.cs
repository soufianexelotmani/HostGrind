using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HostGrid.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class GuestReservationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @space nvarchar(32) = NCHAR(9)+NCHAR(10)+NCHAR(11)+NCHAR(12)+NCHAR(13)+NCHAR(32)+NCHAR(133)+NCHAR(160)+NCHAR(5760)
                    +NCHAR(8192)+NCHAR(8193)+NCHAR(8194)+NCHAR(8195)+NCHAR(8196)+NCHAR(8197)+NCHAR(8198)+NCHAR(8199)
                    +NCHAR(8200)+NCHAR(8201)+NCHAR(8202)+NCHAR(8232)+NCHAR(8233)+NCHAR(8239)+NCHAR(8287)+NCHAR(12288);
                IF EXISTS (
                    SELECT 1 FROM [booking].[Reservation]
                    CROSS APPLY (VALUES (NULLIF(TRIM(@space FROM [ExternalBookingId]), N'') COLLATE Latin1_General_100_BIN2)) AS value([Reference])
                    WHERE value.[Reference] IS NOT NULL
                    GROUP BY [OrganizationId], [Source], value.[Reference] HAVING COUNT_BIG(*) > 1)
                    THROW 51006, 'Conflicting external references must be resolved before this migration.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Reservation_OrganizationId_PropertyId_Source_ExternalBookingId",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalBookingId",
                schema: "booking",
                table: "Reservation",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.Sql("""
                DECLARE @trim nvarchar(32) = NCHAR(9)+NCHAR(10)+NCHAR(11)+NCHAR(12)+NCHAR(13)+NCHAR(32)+NCHAR(133)+NCHAR(160)+NCHAR(5760)
                    +NCHAR(8192)+NCHAR(8193)+NCHAR(8194)+NCHAR(8195)+NCHAR(8196)+NCHAR(8197)+NCHAR(8198)+NCHAR(8199)
                    +NCHAR(8200)+NCHAR(8201)+NCHAR(8202)+NCHAR(8232)+NCHAR(8233)+NCHAR(8239)+NCHAR(8287)+NCHAR(12288);
                UPDATE [booking].[Reservation]
                SET [ExternalBookingId] = NULLIF(TRIM(@trim FROM [ExternalBookingId]), N'')
                WHERE [ExternalBookingId] IS NOT NULL
                AND (DATALENGTH([ExternalBookingId]) <> DATALENGTH(TRIM(@trim FROM [ExternalBookingId])) OR DATALENGTH([ExternalBookingId]) = 0);
                """);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ConfirmationDate",
                schema: "booking",
                table: "Reservation",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                schema: "booking",
                table: "Reservation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedBy",
                schema: "booking",
                table: "Reservation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "booking",
                table: "Reservation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "HistoricalActualCheckIn",
                schema: "booking",
                table: "Reservation",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "HistoricalActualCheckOut",
                schema: "booking",
                table: "Reservation",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HistoricalEnteredBy",
                schema: "booking",
                table: "Reservation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HistoricalEvidenceId",
                schema: "booking",
                table: "Reservation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoricalReason",
                schema: "booking",
                table: "Reservation",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "booking",
                table: "Reservation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "booking",
                table: "Guest",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailMatch",
                schema: "booking",
                table: "Guest",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneMatch",
                schema: "booking",
                table: "Guest",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "booking",
                table: "Guest",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE g
                SET [EmailMatch] = UPPER(NULLIF(TRIM(g.[Email]), N'')),
                    [PhoneMatch] = CASE WHEN LEN(normalized.[Value]) BETWEEN 4 AND 15 AND
                        normalized.[Value] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%' THEN normalized.[Value] END
                FROM [booking].[Guest] g
                CROSS APPLY (VALUES (TRANSLATE(g.[Phone], N'٠١٢٣٤٥٦٧٨٩۰۱۲۳۴۵۶۷۸۹', N'01234567890123456789'))) translated([Value])
                CROSS APPLY (VALUES (REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(translated.[Value],
                    N' ', N''), NCHAR(160), N''), N'-', N''), N'(', N''), N')', N''), N'.', N''), N'+', N''))) digits([Value])
                CROSS APPLY (VALUES (CASE WHEN digits.[Value] LIKE '00%' THEN SUBSTRING(digits.[Value], 3, 32)
                    WHEN digits.[Value] LIKE '0%' AND LEN(digits.[Value]) = 10 AND TRIM(g.[Phone]) NOT LIKE '+%'
                    THEN N'212' + SUBSTRING(digits.[Value], 2, 32) ELSE digits.[Value] END)) normalized([Value]);
                """);

            migrationBuilder.CreateTable(
                name: "ReservationPriceRevision",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Mode = table.Column<short>(type: "smallint", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PlatformFees = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationPriceRevision", x => x.Id);
                    table.UniqueConstraint("AK_ReservationPriceRevision_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ReservationPriceRevision_ApplicationUser_ActorId",
                        column: x => x.ActorId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationPriceRevision_Reservation_OrganizationId_ReservationId",
                        columns: x => new { x.OrganizationId, x.ReservationId },
                        principalSchema: "booking",
                        principalTable: "Reservation",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationNightPrice",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Night = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManualOverride = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationNightPrice", x => x.Id);
                    table.CheckConstraint("CK_ReservationNightPrice_Amount", "[Amount] >= 0");
                    table.ForeignKey(
                        name: "FK_ReservationNightPrice_ReservationPriceRevision_OrganizationId_RevisionId",
                        columns: x => new { x.OrganizationId, x.RevisionId },
                        principalSchema: "booking",
                        principalTable: "ReservationPriceRevision",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_ConfirmedBy",
                schema: "booking",
                table: "Reservation",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_HistoricalEnteredBy",
                schema: "booking",
                table: "Reservation",
                column: "HistoricalEnteredBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_HistoricalEvidenceId",
                schema: "booking",
                table: "Reservation",
                columns: new[] { "OrganizationId", "HistoricalEvidenceId" });

            migrationBuilder.CreateIndex(
                name: "UX_Reservation_Organization_Source_ExternalBookingId",
                schema: "booking",
                table: "Reservation",
                columns: new[] { "OrganizationId", "Source", "ExternalBookingId" },
                unique: true,
                filter: "[ExternalBookingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Guest_OrganizationId_EmailMatch",
                schema: "booking",
                table: "Guest",
                columns: new[] { "OrganizationId", "EmailMatch" },
                filter: "[EmailMatch] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Guest_OrganizationId_PhoneMatch",
                schema: "booking",
                table: "Guest",
                columns: new[] { "OrganizationId", "PhoneMatch" },
                filter: "[PhoneMatch] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationNightPrice_OrganizationId_RevisionId_Night",
                schema: "booking",
                table: "ReservationNightPrice",
                columns: new[] { "OrganizationId", "RevisionId", "Night" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPriceRevision_ActorId",
                schema: "booking",
                table: "ReservationPriceRevision",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPriceRevision_OrganizationId_ReservationId_Number",
                schema: "booking",
                table: "ReservationPriceRevision",
                columns: new[] { "OrganizationId", "ReservationId", "Number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_ApplicationUser_ConfirmedBy",
                schema: "booking",
                table: "Reservation",
                column: "ConfirmedBy",
                principalSchema: "identity",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_ApplicationUser_HistoricalEnteredBy",
                schema: "booking",
                table: "Reservation",
                column: "HistoricalEnteredBy",
                principalSchema: "identity",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_StoredFile_OrganizationId_HistoricalEvidenceId",
                schema: "booking",
                table: "Reservation",
                columns: new[] { "OrganizationId", "HistoricalEvidenceId" },
                principalSchema: "document",
                principalTable: "StoredFile",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [booking].[ReservationPriceRevision])
                    OR EXISTS (SELECT 1 FROM [booking].[Reservation] WHERE [ConfirmationDate] IS NOT NULL OR [HistoricalEvidenceId] IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM [booking].[Guest] WHERE [CreatedAt] IS NOT NULL OR [UpdatedAt] IS NOT NULL)
                    THROW 51007, 'Cannot downgrade while guest metadata, accepted reservation prices or historical entry data exist.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_ApplicationUser_ConfirmedBy",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_ApplicationUser_HistoricalEnteredBy",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_StoredFile_OrganizationId_HistoricalEvidenceId",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropTable(
                name: "ReservationNightPrice",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ReservationPriceRevision",
                schema: "booking");

            migrationBuilder.DropIndex(
                name: "IX_Reservation_ConfirmedBy",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropIndex(
                name: "IX_Reservation_HistoricalEnteredBy",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropIndex(
                name: "IX_Reservation_OrganizationId_HistoricalEvidenceId",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropIndex(
                name: "UX_Reservation_Organization_Source_ExternalBookingId",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropIndex(
                name: "IX_Guest_OrganizationId_EmailMatch",
                schema: "booking",
                table: "Guest");

            migrationBuilder.DropIndex(
                name: "IX_Guest_OrganizationId_PhoneMatch",
                schema: "booking",
                table: "Guest");

            migrationBuilder.DropColumn(
                name: "ConfirmationDate",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "ConfirmedBy",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "HistoricalActualCheckIn",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "HistoricalActualCheckOut",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "HistoricalEnteredBy",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "HistoricalEvidenceId",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "HistoricalReason",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "booking",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "booking",
                table: "Guest");

            migrationBuilder.DropColumn(
                name: "EmailMatch",
                schema: "booking",
                table: "Guest");

            migrationBuilder.DropColumn(
                name: "PhoneMatch",
                schema: "booking",
                table: "Guest");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "booking",
                table: "Guest");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalBookingId",
                schema: "booking",
                table: "Reservation",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_PropertyId_Source_ExternalBookingId",
                schema: "booking",
                table: "Reservation",
                columns: new[] { "OrganizationId", "PropertyId", "Source", "ExternalBookingId" },
                unique: true,
                filter: "[ExternalBookingId] IS NOT NULL");
        }
    }
}
