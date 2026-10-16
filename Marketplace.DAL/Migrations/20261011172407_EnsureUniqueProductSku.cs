using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketplace.DAL.Migrations
{
    /// <inheritdoc />
    public partial class EnsureUniqueProductSku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_BusinessId",
                table: "Products");

            migrationBuilder.Sql(
                """
                UPDATE [Products]
                SET [Sku] = 'SKU-' + UPPER(REPLACE(CONVERT(nvarchar(36), [Id]), '-', ''))
                WHERE [Sku] IS NULL OR LTRIM(RTRIM([Sku])) = '';

                UPDATE [Products]
                SET [Sku] = LEFT(LTRIM(RTRIM([Sku])), 100);

                ;WITH [RankedSkus] AS
                (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER (
                            PARTITION BY [BusinessId], [Sku]
                            ORDER BY [CreatedAt], [Id]) AS [DuplicateNumber]
                    FROM [Products]
                )
                UPDATE [Product]
                SET [Sku] = LEFT([Product].[Sku], 67)
                    + '-'
                    + UPPER(REPLACE(CONVERT(nvarchar(36), [Product].[Id]), '-', ''))
                FROM [Products] AS [Product]
                INNER JOIN [RankedSkus] AS [Ranked] ON [Ranked].[Id] = [Product].[Id]
                WHERE [Ranked].[DuplicateNumber] > 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Products_BusinessId_Sku",
                table: "Products",
                columns: new[] { "BusinessId", "Sku" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Products_BusinessId_Sku",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BusinessId",
                table: "Products",
                column: "BusinessId");
        }
    }
}
