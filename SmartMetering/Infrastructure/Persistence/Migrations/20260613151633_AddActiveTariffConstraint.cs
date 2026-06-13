using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMetering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveTariffConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TariffModels_IsActive",
                table: "TariffModels");

            migrationBuilder.Sql("""
                WITH ActiveTariffs AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (
                               ORDER BY COALESCE(ActivatedAtUtc, CreatedAtUtc) DESC, CreatedAtUtc DESC
                           ) AS RowNumber
                    FROM TariffModels
                    WHERE IsActive = 1
                )
                UPDATE TariffModels
                SET IsActive = 0
                WHERE Id IN (
                    SELECT Id
                    FROM ActiveTariffs
                    WHERE RowNumber > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TariffModels_IsActive",
                table: "TariffModels",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TariffModels_IsActive",
                table: "TariffModels");

            migrationBuilder.CreateIndex(
                name: "IX_TariffModels_IsActive",
                table: "TariffModels",
                column: "IsActive");
        }
    }
}
