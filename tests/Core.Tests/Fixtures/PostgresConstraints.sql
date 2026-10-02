-- This script creates no lasting data: subtransactions and outer rollback isolate assertions.
BEGIN;
DO $$
DECLARE category_id uuid; product_id uuid; farm_id uuid; raised boolean;
BEGIN
    SELECT "Id" INTO category_id FROM "InventoryCategories" LIMIT 1;
    SELECT "Id" INTO farm_id FROM "Farms" LIMIT 1;
    raised:=false;
    BEGIN
        INSERT INTO "Products" ("Id","SKU","Name","CategoryId","Price","CostPrice","Unit","RequiresPrescription","IsActive")
        SELECT gen_random_uuid(),"SKU",'Duplicate',"CategoryId",1,1,"Unit",false,true FROM "Products" LIMIT 1;
    EXCEPTION WHEN unique_violation THEN raised:=true;
    END;
    IF NOT raised THEN RAISE EXCEPTION 'SKU unique constraint is absent'; END IF;
    raised:=false;
    BEGIN
        DELETE FROM "InventoryCategories" WHERE "Id"=(SELECT "CategoryId" FROM "Products" LIMIT 1);
    EXCEPTION WHEN foreign_key_violation THEN raised:=true;
    END;
    IF NOT raised THEN RAISE EXCEPTION 'Category DELETE Restrict is absent'; END IF;
    raised:=false;
    BEGIN
        INSERT INTO "Users" ("Id","UserName","NormalizedUserName","Email","NormalizedEmail","EmailConfirmed","PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount","FullName","IsSuperuser","IsStaff","IsActive","CreatedAt")
        SELECT gen_random_uuid(),'unique-test-user','UNIQUE-TEST-USER',"Email","NormalizedEmail",false,false,false,false,0,'Test',false,false,true,now() FROM "Users" LIMIT 1;
    EXCEPTION WHEN unique_violation THEN raised:=true;
    END;
    IF NOT raised THEN RAISE EXCEPTION 'Normalized email unique constraint is absent'; END IF;
    INSERT INTO "Products" ("Id","SKU","Name","CategoryId","Price","CostPrice","Unit","RequiresPrescription","IsActive")
    VALUES(gen_random_uuid(),'DEFAULT-TEST','Defaults test',category_id,12.34,10.01,0,false,true) RETURNING "Id" INTO product_id;
    INSERT INTO "FarmInventory" ("Id","FarmId","ProductId") VALUES(gen_random_uuid(),farm_id,product_id);
    IF NOT EXISTS(SELECT 1 FROM "FarmInventory" WHERE "ProductId"=product_id AND "Stock"=0 AND "MinStock"=5 AND "MaxStock"=100 AND "Location"='Main warehouse') THEN
        RAISE EXCEPTION 'Inventory defaults are incorrect';
    END IF;
    raised:=false;
    BEGIN UPDATE "FarmInventory" SET "Stock"=-1 WHERE "ProductId"=product_id;
    EXCEPTION WHEN check_violation THEN raised:=true;
    END;
    IF NOT raised THEN RAISE EXCEPTION 'Nonnegative stock constraint is absent'; END IF;
    IF NOT EXISTS(SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='Price' AND numeric_precision=18 AND numeric_scale=2) THEN
        RAISE EXCEPTION 'Price precision is incorrect';
    END IF;
    IF EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name IN ('MilkProductionRecords','WoolProductionRecords','SlaughterRecords','EggProductionRecords')) THEN
        RAISE EXCEPTION 'Old production tables are still present';
    END IF;
    RAISE NOTICE 'PASS: PostgreSQL unique SKU/email, Category Restrict, stock/defaults, money precision and unified production';
END $$;
ROLLBACK;
