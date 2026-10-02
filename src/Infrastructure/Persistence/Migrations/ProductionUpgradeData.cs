namespace Infrastructure.Persistence.Migrations;

// Executed inside the EF migration transaction; temporary data is never exposed by the API.
internal static class ProductionUpgradeData
{
    public const string Prepare = """
        DO $$ BEGIN
            IF EXISTS (SELECT 1 FROM "EggProductionRecords") THEN
                RAISE EXCEPTION 'Lot-level egg records require an explicit animal allocation before this migration. No animal will be invented.';
            END IF;
            IF EXISTS (SELECT 1 FROM "MilkProductionRecords" WHERE "Liters" <= 0)
                OR EXISTS (SELECT 1 FROM "WoolProductionRecords" WHERE "FleeceWeightKg" <= 0)
                OR EXISTS (SELECT 1 FROM "SlaughterRecords" WHERE COALESCE("ColdCarcassWeightKg", "CarcassWeightKg", 0) <= 0) THEN
                RAISE EXCEPTION 'Legacy production requires a positive measured yield before migration.';
            END IF;
            IF EXISTS (SELECT "AnimalId" FROM "SlaughterRecords" GROUP BY "AnimalId" HAVING count(*) > 1) THEN
                RAISE EXCEPTION 'An animal has multiple legacy slaughter events; reconcile them before migration.';
            END IF;
        END $$;
        CREATE TEMP TABLE "__LegacyProductCategories" ON COMMIT DROP AS SELECT "Id", "Category" FROM "Products";
        CREATE TEMP TABLE "__LegacyAnimalProduction" (
            "Id" uuid, "FarmId" uuid, "AnimalId" uuid, "OperationId" uuid, "Date" date,
            "ProductType" integer, "Method" integer, "Quantity" numeric(14,4), "Unit" integer,
            "Notes" varchar(1000), "CreatedAt" timestamptz
        ) ON COMMIT DROP;
        INSERT INTO "__LegacyAnimalProduction"
            SELECT "Id", "FarmId", "AnimalId", "Id", "Date", 0, 0, "Liters", 1,
                jsonb_build_object('legacy','MilkProductionRecord','shift',"Shift",'fatPercent',"FatPercent",'proteinPercent',"ProteinPercent",'somaticCellCount',"SomaticCellCount")::text,"CreatedAt"
            FROM "MilkProductionRecords";
        INSERT INTO "__LegacyAnimalProduction"
            SELECT "Id", "FarmId", "AnimalId", "Id", "Date", 1, 1, "FleeceWeightKg", 0,
                jsonb_build_object('legacy','WoolProductionRecord','fiberDiameterMicrons',"FiberDiameterMicrons",'grade',"Grade")::text,"CreatedAt"
            FROM "WoolProductionRecords";
        INSERT INTO "__LegacyAnimalProduction"
            SELECT "Id", "FarmId", "AnimalId", "Id", "Date", 2, 2, COALESCE("ColdCarcassWeightKg","CarcassWeightKg"), 0,
                jsonb_build_object('legacy','SlaughterRecord','liveWeightKg',"LiveWeightKg",'carcassWeightKg',"CarcassWeightKg",'coldCarcassWeightKg',"ColdCarcassWeightKg",'grade',"Grade")::text,"CreatedAt"
            FROM "SlaughterRecords";
        DO $$ BEGIN
            IF EXISTS(SELECT 1 FROM "__LegacyAnimalProduction" p JOIN "Animals" a ON a."Id"=p."AnimalId" WHERE p."FarmId"<>a."FarmId") THEN
                RAISE EXCEPTION 'Legacy production and animal farm ownership are inconsistent.';
            END IF;
        END $$;
        """;

    public const string Restore = """
        INSERT INTO "InventoryCategories" ("Id","Name","Description","IsActive","CreatedAt")
            SELECT md5('legacy-category-' || "Category"::text)::uuid,
                CASE "Category" WHEN 0 THEN 'Medicamentos' WHEN 1 THEN 'Vacunas' WHEN 2 THEN 'Alimentos'
                    WHEN 3 THEN 'Suministros' WHEN 4 THEN 'Material reproductivo' ELSE 'Otros insumos' END,
                'Categoría migrada: revisar precios de los insumos anteriores',true,now()
            FROM "__LegacyProductCategories" GROUP BY "Category";
        UPDATE "Products" p SET "SKU"='LEGACY-' || p."Id"::text,
            "CategoryId"=md5('legacy-category-' || c."Category"::text)::uuid,"IsActive"=false
            FROM "__LegacyProductCategories" c WHERE c."Id"=p."Id";
        INSERT INTO "AnimalProduction" SELECT * FROM "__LegacyAnimalProduction";
        UPDATE "Animals" a SET "Status"=2,"UpdatedAt"=now()
            WHERE EXISTS(SELECT 1 FROM "AnimalProduction" p WHERE p."AnimalId"=a."Id" AND p."Method"=2);
        UPDATE "AnimalPhotos" SET "FileName"=regexp_replace("Url",'^.*/',''),
            "Url"='/api/animals/' || "AnimalId"::text || '/photos/' || "Id"::text || '/content'
            WHERE "Url" LIKE '/uploads/%';
        """;
}
