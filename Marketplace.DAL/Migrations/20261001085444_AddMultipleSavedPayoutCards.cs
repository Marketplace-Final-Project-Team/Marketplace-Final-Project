using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleSavedPayoutCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SavedPayoutCardNumber",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SavedPayoutRecipientName",
                table: "Businesses");

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduledPayoutCardId",
                table: "Businesses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SavedPayoutCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPayoutCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedPayoutCards_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_ScheduledPayoutCardId",
                table: "Businesses",
                column: "ScheduledPayoutCardId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPayoutCards_BusinessId",
                table: "SavedPayoutCards",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_SavedPayoutCards_ScheduledPayoutCardId",
                table: "Businesses",
                column: "ScheduledPayoutCardId",
                principalTable: "SavedPayoutCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Businesses_SavedPayoutCards_ScheduledPayoutCardId",
                table: "Businesses");

            migrationBuilder.DropTable(
                name: "SavedPayoutCards");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_ScheduledPayoutCardId",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ScheduledPayoutCardId",
                table: "Businesses");

            migrationBuilder.AddColumn<string>(
                name: "SavedPayoutCardNumber",
                table: "Businesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavedPayoutRecipientName",
                table: "Businesses",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
