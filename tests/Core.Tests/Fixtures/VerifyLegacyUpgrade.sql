DO $$ BEGIN
    IF (SELECT count(*) FROM "AnimalProduction" WHERE "AnimalId" IN ('33333333-3333-4333-8333-333333333333','33333333-3333-4333-8333-333333333334'))<>3 THEN
        RAISE EXCEPTION 'Legacy production records were not preserved';
    END IF;
    IF NOT EXISTS(SELECT 1 FROM "AnimalProduction" WHERE "Id"='55555555-5555-4555-8555-555555555551' AND "Quantity"=12.50 AND ("Notes"::jsonb->>'fatPercent')::numeric=3.20) THEN
        RAISE EXCEPTION 'Milk yield or metadata changed';
    END IF;
    IF NOT EXISTS(SELECT 1 FROM "AnimalProduction" WHERE "Id"='55555555-5555-4555-8555-555555555552' AND "Quantity"=3.25) THEN
        RAISE EXCEPTION 'Wool yield changed';
    END IF;
    IF NOT EXISTS(SELECT 1 FROM "Animals" WHERE "Id"='33333333-3333-4333-8333-333333333333' AND "Status"=2) THEN
        RAISE EXCEPTION 'Slaughtered animal is still active';
    END IF;
    IF NOT EXISTS(SELECT 1 FROM "Products" p JOIN "InventoryCategories" c ON c."Id"=p."CategoryId" WHERE p."Id"='44444444-4444-4444-8444-444444444444' AND p."SKU"='LEGACY-44444444-4444-4444-8444-444444444444' AND NOT p."IsActive" AND p."Price"=0 AND c."Name"='Alimentos') THEN
        RAISE EXCEPTION 'Legacy inventory review state is incorrect';
    END IF;
    RAISE NOTICE 'PASS: nonempty legacy migration preserves yields, UUIDs, metadata and inventory';
END $$;
