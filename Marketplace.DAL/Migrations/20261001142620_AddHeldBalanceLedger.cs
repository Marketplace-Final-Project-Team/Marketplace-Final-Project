using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddHeldBalanceLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HeldAmount",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBalanceReleased",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "HeldBalance",
                table: "Businesses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeldAmount",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsBalanceReleased",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "HeldBalance",
                table: "Businesses");
        }
    }
}
