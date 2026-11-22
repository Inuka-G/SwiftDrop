using Microsoft.EntityFrameworkCore.Migrations;

namespace SwiftDrop.Pricing.Data.Persistence.PostgresMigrations;

public partial class InitializePostgresPricing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("pricing");
        // Some Docker volumes already have Pricing tables created by the old SQLite
        // migration and moved into this schema. PostgreSQL must adopt those rows in place.
        migrationBuilder.Sql("""
            SET LOCAL TIME ZONE 'UTC';
            DO $migration$
            BEGIN
                IF to_regclass('public.zones') IS NOT NULL
                    OR to_regclass('public.pricing_rules') IS NOT NULL THEN
                    RAISE EXCEPTION 'Pricing tables found in public. Reconcile them before applying the PostgreSQL Pricing migration.';
                END IF;

                IF to_regclass('pricing.zones') IS NULL AND to_regclass('pricing.pricing_rules') IS NULL THEN
                    CREATE TABLE pricing.zones (
                        "Id" uuid NOT NULL,
                        "ZoneName" varchar(100) NOT NULL,
                        "Description" varchar(500),
                        "IsActive" boolean NOT NULL,
                        "CreatedAtUtc" timestamptz NOT NULL,
                        CONSTRAINT "PK_zones" PRIMARY KEY ("Id")
                    );
                    CREATE TABLE pricing.pricing_rules (
                        "Id" uuid NOT NULL,
                        "ZoneId" uuid NOT NULL,
                        "BaseRate" numeric(10,2) NOT NULL,
                        "AdditionalKgRate" numeric(10,2) NOT NULL,
                        "ServiceType" varchar(20) NOT NULL,
                        "IsActive" boolean NOT NULL,
                        "CreatedAtUtc" timestamptz NOT NULL,
                        CONSTRAINT "PK_pricing_rules" PRIMARY KEY ("Id"),
                        CONSTRAINT "FK_pricing_rules_zones_ZoneId"
                            FOREIGN KEY ("ZoneId") REFERENCES pricing.zones ("Id") ON DELETE RESTRICT
                    );
                    CREATE UNIQUE INDEX "IX_zones_ZoneName" ON pricing.zones ("ZoneName");
                    CREATE INDEX "IX_pricing_rules_ZoneId" ON pricing.pricing_rules ("ZoneId");
                ELSIF to_regclass('pricing.zones') IS NULL OR to_regclass('pricing.pricing_rules') IS NULL THEN
                    RAISE EXCEPTION 'Incomplete PostgreSQL Pricing schema. Reconcile it before applying the migration.';
                ELSE
                    LOCK TABLE pricing.zones, pricing.pricing_rules IN ACCESS EXCLUSIVE MODE;
                    IF EXISTS (
                        SELECT 1 FROM pricing.zones
                        WHERE length("ZoneName") > 100 OR length("Description") > 500
                    ) OR EXISTS (
                        SELECT 1 FROM pricing.pricing_rules
                        WHERE length("ServiceType") > 20
                            OR "BaseRate"::numeric <> "BaseRate"::numeric(10,2)
                            OR "AdditionalKgRate"::numeric <> "AdditionalKgRate"::numeric(10,2)
                    ) THEN
                        RAISE EXCEPTION 'Pricing data exceeds the PostgreSQL model limits. No data was converted.';
                    END IF;

                    ALTER TABLE pricing.pricing_rules DROP CONSTRAINT "FK_pricing_rules_zones_ZoneId";
                    ALTER TABLE pricing.zones
                        ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid,
                        ALTER COLUMN "ZoneName" TYPE varchar(100),
                        ALTER COLUMN "Description" TYPE varchar(500),
                        ALTER COLUMN "IsActive" TYPE boolean USING "IsActive"::text::boolean,
                        ALTER COLUMN "CreatedAtUtc" TYPE timestamptz USING "CreatedAtUtc"::timestamptz;
                    ALTER TABLE pricing.pricing_rules
                        ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid,
                        ALTER COLUMN "ZoneId" TYPE uuid USING "ZoneId"::uuid,
                        ALTER COLUMN "BaseRate" TYPE numeric(10,2) USING "BaseRate"::numeric(10,2),
                        ALTER COLUMN "AdditionalKgRate" TYPE numeric(10,2) USING "AdditionalKgRate"::numeric(10,2),
                        ALTER COLUMN "ServiceType" TYPE varchar(20),
                        ALTER COLUMN "IsActive" TYPE boolean USING "IsActive"::text::boolean,
                        ALTER COLUMN "CreatedAtUtc" TYPE timestamptz USING "CreatedAtUtc"::timestamptz,
                        ADD CONSTRAINT "FK_pricing_rules_zones_ZoneId"
                            FOREIGN KEY ("ZoneId") REFERENCES pricing.zones ("Id") ON DELETE RESTRICT;
                END IF;
            END
            $migration$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("This migration can adopt existing Pricing data. Restore a backup to roll it back.");
}
