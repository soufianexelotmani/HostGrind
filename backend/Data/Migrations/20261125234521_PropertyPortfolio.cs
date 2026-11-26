using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HostGrid.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PropertyPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Owner_OrganizationId_UserId",
                schema: "organization",
                table: "Owner");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "document",
                table: "StoredFile",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "property",
                table: "Property",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "property",
                table: "Property",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "organization",
                table: "Owner",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "organization",
                table: "Owner",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                schema: "property",
                table: "Amenity",
                columns: new[] { "Id", "Code" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000001"), "Wifi" },
                    { new Guid("50000000-0000-0000-0000-000000000002"), "AirConditioning" },
                    { new Guid("50000000-0000-0000-0000-000000000003"), "Television" },
                    { new Guid("50000000-0000-0000-0000-000000000004"), "Kitchen" },
                    { new Guid("50000000-0000-0000-0000-000000000005"), "Refrigerator" },
                    { new Guid("50000000-0000-0000-0000-000000000006"), "WashingMachine" },
                    { new Guid("50000000-0000-0000-0000-000000000007"), "Parking" },
                    { new Guid("50000000-0000-0000-0000-000000000008"), "Elevator" },
                    { new Guid("50000000-0000-0000-0000-000000000009"), "Balcony" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Owner_OrganizationId_UserId",
                schema: "organization",
                table: "Owner",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Owner_OrganizationId_UserId",
                schema: "organization",
                table: "Owner");

            migrationBuilder.DeleteData(
                schema: "property",
                table: "Amenity",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "property",
                table: "Amenity",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "property",
                table: "Amenity",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "property",
                table: "Amenity",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "property",
                table: "Amenity",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "property",
                table: "Amenity",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "property",
                table: "Amenity",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "property",
                table: "Amenity",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "property",
                table: "Amenity",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000009"));

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "document",
                table: "StoredFile");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "property",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "property",
                table: "Property");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "organization",
                table: "Owner");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "organization",
                table: "Owner",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Owner_OrganizationId_UserId",
                schema: "organization",
                table: "Owner",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);
        }
    }
}
