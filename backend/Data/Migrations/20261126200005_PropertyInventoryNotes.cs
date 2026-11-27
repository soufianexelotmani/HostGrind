using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HostGrid.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PropertyInventoryNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "property",
                table: "PropertyInventoryItem",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "property",
                table: "PropertyInventoryItem");
        }
    }
}
