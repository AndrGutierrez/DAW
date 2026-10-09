using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeletionAndBcvReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WeightRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "WeightRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WeightRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserRoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "UserRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserPermissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "UserPermissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserPermissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserFarms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "UserFarms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserFarms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Suppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Suppliers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StockMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StockMovements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Species",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Species",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Species",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SemenBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "SemenBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SemenBatches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RolePermissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "RolePermissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RolePermissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ReproductiveEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ReproductiveEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ReproductiveEvents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Rations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Rations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Rations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RationIngredients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "RationIngredients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RationIngredients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ProductBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ProductBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductBatches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Paddocks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Paddocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Paddocks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Lots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Lots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Lots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InventoryCategories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "InventoryCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InventoryCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "HealthStatusChanges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "HealthStatusChanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "HealthStatusChanges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "HealthEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "HealthEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "HealthEvents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "FeedingRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "FeedingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FeedingRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Farms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Farms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Farms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "FarmInventory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "FarmInventory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FarmInventory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ExchangeRates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ExchangeRates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntryMethod",
                table: "ExchangeRates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ExchangeRates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "ExchangeRates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ExchangeRates",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "https://www.bcv.org.ve/");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Diseases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Diseases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Diseases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Breeds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Breeds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Breeds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Attachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Attachments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Animals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Animals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Animals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AnimalProduction",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "AnimalProduction",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AnimalProduction",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AnimalPhotos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "AnimalPhotos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AnimalPhotos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AnimalMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "AnimalMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AnimalMovements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AlertRules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "AlertRules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AlertRules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "InventoryCategories",
                keyColumn: "Id",
                keyValue: new Guid("a1100000-0000-4000-8000-000000000001"),
                columns: new[] { "DeletedAt", "DeletedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "InventoryCategories",
                keyColumn: "Id",
                keyValue: new Guid("a1100000-0000-4000-8000-000000000002"),
                columns: new[] { "DeletedAt", "DeletedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_WeightRecords_IsDeleted_DeletedAt",
                table: "WeightRecords",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFarms_IsDeleted_DeletedAt",
                table: "UserFarms",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_IsDeleted_DeletedAt",
                table: "Transactions",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_IsDeleted_DeletedAt",
                table: "Tasks",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_IsDeleted_DeletedAt",
                table: "Suppliers",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_IsDeleted_DeletedAt",
                table: "StockMovements",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Species_IsDeleted_DeletedAt",
                table: "Species",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SemenBatches_IsDeleted_DeletedAt",
                table: "SemenBatches",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReproductiveEvents_IsDeleted_DeletedAt",
                table: "ReproductiveEvents",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Rations_IsDeleted_DeletedAt",
                table: "Rations",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RationIngredients_IsDeleted_DeletedAt",
                table: "RationIngredients",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_DeletedAt",
                table: "Products",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_IsDeleted_DeletedAt",
                table: "ProductBatches",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Paddocks_IsDeleted_DeletedAt",
                table: "Paddocks",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Lots_IsDeleted_DeletedAt",
                table: "Lots",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_IsDeleted_DeletedAt",
                table: "InventoryCategories",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthStatusChanges_IsDeleted_DeletedAt",
                table: "HealthStatusChanges",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthEvents_IsDeleted_DeletedAt",
                table: "HealthEvents",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedingRecords_IsDeleted_DeletedAt",
                table: "FeedingRecords",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Farms_IsDeleted_DeletedAt",
                table: "Farms",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmInventory_IsDeleted_DeletedAt",
                table: "FarmInventory",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_IsDeleted_DeletedAt",
                table: "ExchangeRates",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_IsDeleted_DeletedAt",
                table: "Diseases",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Breeds_IsDeleted_DeletedAt",
                table: "Breeds",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_IsDeleted_DeletedAt",
                table: "Attachments",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Animals_IsDeleted_DeletedAt",
                table: "Animals",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalProduction_IsDeleted_DeletedAt",
                table: "AnimalProduction",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalPhotos_IsDeleted_DeletedAt",
                table: "AnimalPhotos",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalMovements_IsDeleted_DeletedAt",
                table: "AnimalMovements",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsDeleted_DeletedAt",
                table: "Alerts",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_IsDeleted_DeletedAt",
                table: "AlertRules",
                columns: new[] { "IsDeleted", "DeletedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeightRecords_IsDeleted_DeletedAt",
                table: "WeightRecords");

            migrationBuilder.DropIndex(
                name: "IX_UserFarms_IsDeleted_DeletedAt",
                table: "UserFarms");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_IsDeleted_DeletedAt",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_IsDeleted_DeletedAt",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_IsDeleted_DeletedAt",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_IsDeleted_DeletedAt",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Species_IsDeleted_DeletedAt",
                table: "Species");

            migrationBuilder.DropIndex(
                name: "IX_SemenBatches_IsDeleted_DeletedAt",
                table: "SemenBatches");

            migrationBuilder.DropIndex(
                name: "IX_ReproductiveEvents_IsDeleted_DeletedAt",
                table: "ReproductiveEvents");

            migrationBuilder.DropIndex(
                name: "IX_Rations_IsDeleted_DeletedAt",
                table: "Rations");

            migrationBuilder.DropIndex(
                name: "IX_RationIngredients_IsDeleted_DeletedAt",
                table: "RationIngredients");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_DeletedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductBatches_IsDeleted_DeletedAt",
                table: "ProductBatches");

            migrationBuilder.DropIndex(
                name: "IX_Paddocks_IsDeleted_DeletedAt",
                table: "Paddocks");

            migrationBuilder.DropIndex(
                name: "IX_Lots_IsDeleted_DeletedAt",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_InventoryCategories_IsDeleted_DeletedAt",
                table: "InventoryCategories");

            migrationBuilder.DropIndex(
                name: "IX_HealthStatusChanges_IsDeleted_DeletedAt",
                table: "HealthStatusChanges");

            migrationBuilder.DropIndex(
                name: "IX_HealthEvents_IsDeleted_DeletedAt",
                table: "HealthEvents");

            migrationBuilder.DropIndex(
                name: "IX_FeedingRecords_IsDeleted_DeletedAt",
                table: "FeedingRecords");

            migrationBuilder.DropIndex(
                name: "IX_Farms_IsDeleted_DeletedAt",
                table: "Farms");

            migrationBuilder.DropIndex(
                name: "IX_FarmInventory_IsDeleted_DeletedAt",
                table: "FarmInventory");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_IsDeleted_DeletedAt",
                table: "ExchangeRates");

            migrationBuilder.DropIndex(
                name: "IX_Diseases_IsDeleted_DeletedAt",
                table: "Diseases");

            migrationBuilder.DropIndex(
                name: "IX_Breeds_IsDeleted_DeletedAt",
                table: "Breeds");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_IsDeleted_DeletedAt",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Animals_IsDeleted_DeletedAt",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_AnimalProduction_IsDeleted_DeletedAt",
                table: "AnimalProduction");

            migrationBuilder.DropIndex(
                name: "IX_AnimalPhotos_IsDeleted_DeletedAt",
                table: "AnimalPhotos");

            migrationBuilder.DropIndex(
                name: "IX_AnimalMovements_IsDeleted_DeletedAt",
                table: "AnimalMovements");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_IsDeleted_DeletedAt",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_AlertRules_IsDeleted_DeletedAt",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WeightRecords");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "WeightRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WeightRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserFarms");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserFarms");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserFarms");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Species");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Species");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Species");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SemenBatches");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SemenBatches");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SemenBatches");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ReproductiveEvents");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ReproductiveEvents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ReproductiveEvents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Rations");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Rations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Rations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RationIngredients");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RationIngredients");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RationIngredients");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductBatches");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Paddocks");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Paddocks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Paddocks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InventoryCategories");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "InventoryCategories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InventoryCategories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "HealthStatusChanges");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HealthStatusChanges");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "HealthStatusChanges");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "HealthEvents");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HealthEvents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "HealthEvents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FeedingRecords");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "FeedingRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FeedingRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FarmInventory");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "FarmInventory");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FarmInventory");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ExchangeRates");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ExchangeRates");

            migrationBuilder.DropColumn(
                name: "EntryMethod",
                table: "ExchangeRates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ExchangeRates");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "ExchangeRates");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ExchangeRates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Breeds");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Breeds");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Breeds");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AnimalProduction");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AnimalProduction");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AnimalProduction");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AnimalPhotos");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AnimalPhotos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AnimalPhotos");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AnimalMovements");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AnimalMovements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AnimalMovements");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AlertRules");
        }
    }
}
