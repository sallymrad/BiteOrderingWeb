using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiteOrderWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryZoneToArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryPrice",
                table: "Areas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryPrice",
                table: "Areas");
        }
    }
}
