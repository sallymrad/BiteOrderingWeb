using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiteOrderWeb.Migrations
{
    /// <inheritdoc />
    public partial class Rej : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowAgain",
                table: "OrderRejections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowAgain",
                table: "OrderRejections");
        }
    }
}
