using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMetering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualReadingsAndPhase10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailSent",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAtUtc",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ManualReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeclaredTotalEnergyKwh = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginalImageBlobName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    OptimizedImageBlobName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualReadings_SmartMeters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "SmartMeters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManualReadings_Users_ConsumerId",
                        column: x => x.ConsumerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualReadings_ConsumerId",
                table: "ManualReadings",
                column: "ConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualReadings_MeterId_Status",
                table: "ManualReadings",
                columns: new[] { "MeterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualReadings_Status",
                table: "ManualReadings",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualReadings");

            migrationBuilder.DropColumn(
                name: "EmailSent",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaidAtUtc",
                table: "Invoices");
        }
    }
}
