using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDisputeAndAddReturnPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM PostSaleRequests WHERE Type = 1;");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PostSaleRequests");

            migrationBuilder.CreateTable(
                name: "PostSaleRequestImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostSaleRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSaleRequestImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostSaleRequestImages_PostSaleRequests_PostSaleRequestId",
                        column: x => x.PostSaleRequestId,
                        principalTable: "PostSaleRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostSaleRequestImages_PostSaleRequestId",
                table: "PostSaleRequestImages",
                column: "PostSaleRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostSaleRequestImages");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "PostSaleRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
