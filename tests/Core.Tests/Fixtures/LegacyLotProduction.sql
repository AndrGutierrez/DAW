-- Add after LegacyProduction.sql, before attempting the unified migration.
-- Lot totals cannot be attributed to an individual animal without a business decision.
INSERT INTO "Lots" ("Id", "FarmId", "SpeciesId", "Name", "Purpose", "IsActive")
VALUES ('66666666-6666-4666-8666-666666666666', '11111111-1111-4111-8111-111111111111',
        '22222222-2222-4222-8222-222222222222', 'Historical lot totals', 3, true);
INSERT INTO "EggProductionRecords" ("Id", "FarmId", "LotId", "Date", "TotalEggs", "BrokenEggs")
VALUES ('77777777-7777-4777-8777-777777777777', '11111111-1111-4111-8111-111111111111',
        '66666666-6666-4666-8666-666666666666', '2026-09-20', 12, 1);
