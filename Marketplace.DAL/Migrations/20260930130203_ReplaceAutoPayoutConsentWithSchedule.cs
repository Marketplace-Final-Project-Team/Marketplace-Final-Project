using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAutoPayoutConsentWithSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastScheduledPayoutAt",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScheduledPayoutAmount",
                table: "Businesses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduledPayoutDayOfMonth",
                table: "Businesses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduledPayoutEnabled",
                table: "Businesses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastScheduledPayoutAt",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ScheduledPayoutAmount",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ScheduledPayoutDayOfMonth",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ScheduledPayoutEnabled",
                table: "Businesses");
        }
    }
}
