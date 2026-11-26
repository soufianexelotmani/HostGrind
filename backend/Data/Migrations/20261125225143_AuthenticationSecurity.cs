using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HostGrid.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuthenticationSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MfaVerified",
                schema: "identity",
                table: "RefreshToken",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStampHash",
                schema: "identity",
                table: "RefreshToken",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoginChallenge",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginChallenge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginChallenge_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvent",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginChallenge_TokenHash",
                schema: "identity",
                table: "LoginChallenge",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginChallenge_UserId",
                schema: "identity",
                table: "LoginChallenge",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvent_OccurredAt",
                schema: "system",
                table: "SecurityEvent",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginChallenge",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "SecurityEvent",
                schema: "system");

            migrationBuilder.DropColumn(
                name: "MfaVerified",
                schema: "identity",
                table: "RefreshToken");

            migrationBuilder.DropColumn(
                name: "SecurityStampHash",
                schema: "identity",
                table: "RefreshToken");
        }
    }
}
