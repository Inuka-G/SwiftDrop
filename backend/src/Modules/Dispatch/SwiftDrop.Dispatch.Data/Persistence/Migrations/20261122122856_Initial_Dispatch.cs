using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftDrop.Dispatch.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Dispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dispatch");

            migrationBuilder.CreateTable(
                name: "courier_profiles",
                schema: "dispatch",
                columns: table => new
                {
                    CourierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    VehicleType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LicensePlate = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courier_profiles", x => x.CourierId);
                });

            migrationBuilder.CreateTable(
                name: "parcel_assignments",
                schema: "dispatch",
                columns: table => new
                {
                    ParcelId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<long>(type: "bigint", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parcel_assignments", x => x.ParcelId);
                });

            migrationBuilder.CreateTable(
                name: "processed_events",
                schema: "dispatch",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_events", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_courier_profiles_ZoneId_IsAvailable",
                schema: "dispatch",
                table: "courier_profiles",
                columns: new[] { "ZoneId", "IsAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_parcel_assignments_CourierId_State",
                schema: "dispatch",
                table: "parcel_assignments",
                columns: new[] { "CourierId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_parcel_assignments_State_ZoneId_CreatedAt",
                schema: "dispatch",
                table: "parcel_assignments",
                columns: new[] { "State", "ZoneId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "courier_profiles",
                schema: "dispatch");

            migrationBuilder.DropTable(
                name: "parcel_assignments",
                schema: "dispatch");

            migrationBuilder.DropTable(
                name: "processed_events",
                schema: "dispatch");
        }
    }
}
