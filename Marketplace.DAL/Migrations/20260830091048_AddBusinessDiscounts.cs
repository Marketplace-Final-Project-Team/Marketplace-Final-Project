using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "Discounts",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<decimal>(
                name: "Percent",
                table: "Discounts",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                table: "Discounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_BusinessId",
                table: "Discounts",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Discounts_Businesses_BusinessId",
                table: "Discounts",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discounts_Businesses_BusinessId",
                table: "Discounts");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_BusinessId",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Discounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "Discounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Percent",
                table: "Discounts",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
