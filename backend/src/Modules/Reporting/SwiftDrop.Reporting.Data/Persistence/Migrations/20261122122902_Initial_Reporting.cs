using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftDrop.Reporting.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Reporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "daily_statistics",
                schema: "reporting",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedCount = table.Column<int>(type: "integer", nullable: false),
                    DeliveredCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_statistics", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "processed_events",
                schema: "reporting",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_events", x => x.EventId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_statistics",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "processed_events",
                schema: "reporting");
        }
    }
}
