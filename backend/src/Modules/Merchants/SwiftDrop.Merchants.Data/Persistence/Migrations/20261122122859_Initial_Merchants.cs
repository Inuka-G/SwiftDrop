using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftDrop.Merchants.Data.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Merchants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "merchants");

            migrationBuilder.CreateTable(
                name: "api_keys",
                schema: "merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "client_credentials",
                schema: "merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientSecretHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_credentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "merchants",
                schema: "merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BusinessRegNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    WebhookUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebhookSecret = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_KeyHash",
                schema: "merchants",
                table: "api_keys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_MerchantId",
                schema: "merchants",
                table: "api_keys",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_client_credentials_ClientId",
                schema: "merchants",
                table: "client_credentials",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_credentials_MerchantId",
                schema: "merchants",
                table: "client_credentials",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_merchants_BusinessRegNumber",
                schema: "merchants",
                table: "merchants",
                column: "BusinessRegNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_merchants_Status",
                schema: "merchants",
                table: "merchants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_merchants_UserId",
                schema: "merchants",
                table: "merchants",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_keys",
                schema: "merchants");

            migrationBuilder.DropTable(
                name: "client_credentials",
                schema: "merchants");

            migrationBuilder.DropTable(
                name: "merchants",
                schema: "merchants");
        }
    }
}
