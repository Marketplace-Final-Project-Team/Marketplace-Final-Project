using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddItemLevelReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReturned",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderItemId",
                table: "PostSaleRequests",
                type: "uniqueidentifier",
                nullable: true);

            // Existing pre-item-level requests are backfilled to the (only) order item they
            // referred to, since this table only ever held single-item test orders so far.
            migrationBuilder.Sql(@"
                UPDATE psr
                SET psr.OrderItemId = oi.Id
                FROM PostSaleRequests psr
                CROSS APPLY (
                    SELECT TOP 1 Id FROM OrderItems WHERE OrderId = psr.OrderId
                ) oi
                WHERE psr.OrderItemId IS NULL;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderItemId",
                table: "PostSaleRequests",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostSaleRequests_OrderItemId",
                table: "PostSaleRequests",
                column: "OrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostSaleRequests_OrderItems_OrderItemId",
                table: "PostSaleRequests",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostSaleRequests_OrderItems_OrderItemId",
                table: "PostSaleRequests");

            migrationBuilder.DropIndex(
                name: "IX_PostSaleRequests_OrderItemId",
                table: "PostSaleRequests");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "PostSaleRequests");

            migrationBuilder.DropColumn(
                name: "IsReturned",
                table: "OrderItems");
        }
    }
}
