-- Read-only evidence: execute after migrations and seeding.
SELECT version();
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name;
SELECT tablename,indexname,indexdef FROM pg_indexes WHERE schemaname='public'
 AND tablename IN ('Products','Users','InventoryCategories','FarmInventory','AnimalProduction') ORDER BY tablename,indexname;
SELECT table_name,column_name,data_type,numeric_precision,numeric_scale,column_default
 FROM information_schema.columns WHERE table_schema='public'
 AND (table_name='Products' OR table_name='FarmInventory') ORDER BY table_name,ordinal_position;
SELECT conrelid::regclass AS table_name,conname,pg_get_constraintdef(oid) AS definition
 FROM pg_constraint WHERE contype IN ('f','c') AND connamespace='public'::regnamespace
 AND conrelid IN ('"Products"'::regclass,'"FarmInventory"'::regclass,'"AnimalProduction"'::regclass,'"Animals"'::regclass) ORDER BY table_name,conname;
SELECT c."Name" AS category,p."SKU",p."Name",p."Price",p."CostPrice",p."Brand",p."Unit",i."Stock",i."MinStock",i."MaxStock",i."Location",f."Code" AS farm
 FROM "Products" p JOIN "InventoryCategories" c ON c."Id"=p."CategoryId"
 JOIN "FarmInventory" i ON i."ProductId"=p."Id" JOIN "Farms" f ON f."Id"=i."FarmId" ORDER BY p."SKU";
SELECT a."InternalTag",p."ProductType",p."Method",p."Date",p."Quantity",p."Unit",p."OperationId"
 FROM "AnimalProduction" p JOIN "Animals" a ON a."Id"=p."AnimalId" ORDER BY a."InternalTag",p."Date",p."ProductType";
SELECT u."UserName",u."Email",u."IsActive",r."Name" AS role FROM "Users" u
 JOIN "UserRoles" ur ON ur."UserId"=u."Id" JOIN "Roles" r ON r."Id"=ur."RoleId" ORDER BY u."UserName",r."Name";
SELECT "EntityName","Action",count(*) AS operations FROM "AuditLogs" GROUP BY "EntityName","Action" ORDER BY "EntityName","Action";
