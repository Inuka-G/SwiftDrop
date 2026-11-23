using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftDrop.Pricing.Data.Persistence.Migrations;

public partial class AddUniquePricingRulePerZoneServiceType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_pricing_rules_ZoneId_ServiceType",
            table: "pricing_rules",
            columns: new[] { "ZoneId", "ServiceType" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_pricing_rules_ZoneId_ServiceType",
            table: "pricing_rules");
    }
}
