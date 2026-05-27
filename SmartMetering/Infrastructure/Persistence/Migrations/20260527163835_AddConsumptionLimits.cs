using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMetering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumptionLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsumptionLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastAlertedMonth = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumptionLimits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionLimits_UserId",
                table: "ConsumptionLimits",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumptionLimits");
        }
    }
}
