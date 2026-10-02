using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManagedInventoryCategorySeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Model-managed categories use fixed IDs in new databases. Preserve the IDs
            // and product references of categories already created by the runtime seed.
            migrationBuilder.Sql("""
                INSERT INTO "InventoryCategories" ("Id", "CreatedAt", "Description", "IsActive", "Name")
                VALUES
                  ('a1100000-0000-4000-8000-000000000001', '2026-01-01 00:00:00+00', 'Insumos de la operación ganadera', true, 'Alimentación animal'),
                  ('a1100000-0000-4000-8000-000000000002', '2026-01-01 00:00:00+00', 'Insumos de la operación ganadera', true, 'Sanidad animal')
                ON CONFLICT ("Name") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "InventoryCategories",
                keyColumn: "Id",
                keyValue: new Guid("a1100000-0000-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "InventoryCategories",
                keyColumn: "Id",
                keyValue: new Guid("a1100000-0000-4000-8000-000000000002"));
        }
    }
}
