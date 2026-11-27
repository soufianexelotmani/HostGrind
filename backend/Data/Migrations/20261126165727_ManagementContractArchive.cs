using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HostGrid.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManagementContractArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "property",
                table: "ManagementContract",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "property",
                table: "ManagementContract");
        }
    }
}
