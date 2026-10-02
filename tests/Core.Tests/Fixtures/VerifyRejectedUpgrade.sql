-- Run after the unified migration rejects historical lot-level egg data.
DO $$
BEGIN
    IF to_regclass('public."AnimalProduction"') IS NOT NULL THEN
        RAISE EXCEPTION 'Rejected migration created the new production table';
    END IF;
    IF (SELECT count(*) FROM "__EFMigrationsHistory") <> 2 THEN
        RAISE EXCEPTION 'Rejected migration changed migration history';
    END IF;
    IF (SELECT count(*) FROM "EggProductionRecords") <> 1
       OR (SELECT "TotalEggs" FROM "EggProductionRecords" LIMIT 1) <> 12
       OR (SELECT count(*) FROM "MilkProductionRecords") <> 1
       OR (SELECT count(*) FROM "WoolProductionRecords") <> 1
       OR (SELECT count(*) FROM "SlaughterRecords") <> 1 THEN
        RAISE EXCEPTION 'Rejected migration changed historical yields';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "Products" WHERE "Id" = '44444444-4444-4444-8444-444444444444' AND "Category" = 2) THEN
        RAISE EXCEPTION 'Rejected migration changed the historical product';
    END IF;
END $$;
