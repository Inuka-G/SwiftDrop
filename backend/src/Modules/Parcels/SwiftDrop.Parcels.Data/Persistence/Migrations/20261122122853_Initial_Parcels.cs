using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftDrop.Parcels.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Parcels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "parcels");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "parcels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "parcels",
                schema: "parcels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickupAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PickupContactName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PickupContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DropoffAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RecipientPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    SizeCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstimatedFee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    FinalFee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parcels", x => x.Id);
                    table.CheckConstraint("CK_parcels_single_owner", "(\"CustomerId\" IS NULL AND \"MerchantId\" IS NOT NULL) OR (\"CustomerId\" IS NOT NULL AND \"MerchantId\" IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "delivery_proofs",
                schema: "parcels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhotoKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhotoContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_proofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_delivery_proofs_parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalSchema: "parcels",
                        principalTable: "parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "failure_reports",
                schema: "parcels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhotoKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhotoContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_failure_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_failure_reports_parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalSchema: "parcels",
                        principalTable: "parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "status_history",
                schema: "parcels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParcelId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedByActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByActorType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "User"),
                    ChangedByRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_status_history_parcels_ParcelId",
                        column: x => x.ParcelId,
                        principalSchema: "parcels",
                        principalTable: "parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_proofs_ParcelId",
                schema: "parcels",
                table: "delivery_proofs",
                column: "ParcelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_failure_reports_ParcelId",
                schema: "parcels",
                table: "failure_reports",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAtUtc_CreatedAtUtc",
                schema: "parcels",
                table: "outbox_messages",
                columns: new[] { "ProcessedAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_parcels_CourierId_CurrentStatus",
                schema: "parcels",
                table: "parcels",
                columns: new[] { "CourierId", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_parcels_CustomerId",
                schema: "parcels",
                table: "parcels",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_parcels_MerchantId",
                schema: "parcels",
                table: "parcels",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_parcels_TrackingNumber",
                schema: "parcels",
                table: "parcels",
                column: "TrackingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_status_history_ParcelId_CreatedAtUtc",
                schema: "parcels",
                table: "status_history",
                columns: new[] { "ParcelId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery_proofs",
                schema: "parcels");

            migrationBuilder.DropTable(
                name: "failure_reports",
                schema: "parcels");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "parcels");

            migrationBuilder.DropTable(
                name: "status_history",
                schema: "parcels");

            migrationBuilder.DropTable(
                name: "parcels",
                schema: "parcels");
        }
    }
}
