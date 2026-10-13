# Relational data dictionary

Generated from the migrated PostgreSQL schema. Regenerate the diagrams first, then run node scripts/erd/data-dictionary.cjs.

Source: `db/schema.sql`; SHA-256: `5598c9dfa01239be370b02c077db16bc42bae19b61c2c54ff0160aeefe21084b`.

Every table below is present in the physical schema. A modeled table does not imply that its complete business workflow has been implemented.

## AlertRules

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| Type | integer | No |  |  |
| ThresholdValue | numeric(12,4) | Yes |  |  |
| ThresholdDays | integer | Yes |  |  |
| IsEnabled | boolean | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |


## Alerts

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| Type | integer | No |  |  |
| Severity | integer | No |  |  |
| Message | character varying(500) | No |  |  |
| RelatedEntityType | character varying(100) | Yes |  |  |
| RelatedEntityId | uuid | Yes |  |  |
| DueDate | date | Yes |  |  |
| IsResolved | boolean | No |  |  |
| ResolvedAt | timestamp with time zone | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |


## AnimalMovements

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| AnimalId | uuid | No | FK: Animals(Id) |  |
| FromPaddockId | uuid | Yes | FK: Paddocks(Id) |  |
| ToPaddockId | uuid | Yes | FK: Paddocks(Id) |  |
| FromLotId | uuid | Yes | FK: Lots(Id) |  |
| ToLotId | uuid | Yes | FK: Lots(Id) |  |
| Date | date | No |  |  |
| Reason | character varying(300) | Yes |  |  |
| UserId | uuid | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_AnimalMovements_Animals_AnimalId`: AnimalId -> Animals(Id); delete behavior: `CASCADE`.
- `FK_AnimalMovements_Lots_FromLotId`: FromLotId -> Lots(Id); delete behavior: `RESTRICT`.
- `FK_AnimalMovements_Lots_ToLotId`: ToLotId -> Lots(Id); delete behavior: `RESTRICT`.
- `FK_AnimalMovements_Paddocks_FromPaddockId`: FromPaddockId -> Paddocks(Id); delete behavior: `RESTRICT`.
- `FK_AnimalMovements_Paddocks_ToPaddockId`: ToPaddockId -> Paddocks(Id); delete behavior: `RESTRICT`.

## AnimalPhotos

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| AnimalId | uuid | No | FK: Animals(Id) |  |
| Url | character varying(500) | No |  |  |
| FileName | character varying(255) | No |  |  |
| ContentType | character varying(100) | Yes |  |  |
| SizeBytes | bigint | Yes |  |  |
| UploadedByUserId | uuid | Yes |  |  |
| UploadedAt | timestamp with time zone | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_AnimalPhotos_Animals_AnimalId`: AnimalId -> Animals(Id); delete behavior: `CASCADE`.

## AnimalProduction

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id) |  |
| AnimalId | uuid | No | FK: Animals(Id) |  |
| OperationId | uuid | No | UNIQUE: IX_AnimalProduction_OperationId_ProductType |  |
| Date | date | No |  |  |
| ProductType | integer | No | UNIQUE: IX_AnimalProduction_OperationId_ProductType |  |
| Method | integer | No |  |  |
| Quantity | numeric(14,4) | No |  |  |
| Unit | integer | No |  |  |
| Notes | character varying(1000) | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- CHECK: `CONSTRAINT "CK_AnimalProduction_Quantity" CHECK (("Quantity" > (0)::numeric))`
- `FK_AnimalProduction_Animals_AnimalId`: AnimalId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_AnimalProduction_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `RESTRICT`.

## Animals

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id); UNIQUE: IX_Animals_FarmId_InternalTag; UNIQUE: IX_Animals_FarmId_OfficialId; UNIQUE: IX_Animals_FarmId_Rfid |  |
| SpeciesId | uuid | No | FK: Species(Id) |  |
| BreedId | uuid | Yes | FK: Breeds(Id) |  |
| LotId | uuid | Yes | FK: Lots(Id) |  |
| PaddockId | uuid | Yes | FK: Paddocks(Id) |  |
| InternalTag | character varying(50) | No | UNIQUE: IX_Animals_FarmId_InternalTag |  |
| OfficialId | character varying(50) | Yes | UNIQUE: IX_Animals_FarmId_OfficialId |  |
| Rfid | character varying(50) | Yes | UNIQUE: IX_Animals_FarmId_Rfid |  |
| Name | character varying(100) | Yes |  |  |
| Sex | integer | No |  |  |
| BirthDate | date | Yes |  |  |
| BirthWeightKg | numeric(14,4) | Yes |  |  |
| Color | character varying(50) | Yes |  |  |
| Markings | character varying(200) | Yes |  |  |
| Status | integer | No |  |  |
| Origin | integer | No |  |  |
| Purpose | integer | No |  |  |
| HealthStatus | integer | No |  |  |
| DamId | uuid | Yes | FK: Animals(Id) |  |
| SireId | uuid | Yes | FK: Animals(Id) |  |
| Notes | character varying(2000) | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| UpdatedAt | timestamp with time zone | No |  | now() |
| TargetDailyGainKg | numeric(8,4) | Yes |  |  |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_Animals_Animals_DamId`: DamId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_Animals_Animals_SireId`: SireId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_Animals_Breeds_BreedId`: BreedId -> Breeds(Id); delete behavior: `RESTRICT`.
- `FK_Animals_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `RESTRICT`.
- `FK_Animals_Lots_LotId`: LotId -> Lots(Id); delete behavior: `RESTRICT`.
- `FK_Animals_Paddocks_PaddockId`: PaddockId -> Paddocks(Id); delete behavior: `RESTRICT`.
- `FK_Animals_Species_SpeciesId`: SpeciesId -> Species(Id); delete behavior: `RESTRICT`.

## Attachments

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| OwnerType | character varying(100) | No |  |  |
| OwnerId | uuid | No |  |  |
| FileName | character varying(255) | No |  |  |
| Url | character varying(500) | No |  |  |
| ContentType | character varying(100) | Yes |  |  |
| SizeBytes | bigint | Yes |  |  |
| UploadedByUserId | uuid | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |


## AuditLogs

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| UserId | uuid | Yes |  |  |
| FarmId | uuid | Yes |  |  |
| Action | character varying(50) | No |  |  |
| EntityName | character varying(150) | No |  |  |
| EntityId | character varying(100) | Yes |  |  |
| OldValues | jsonb | Yes |  |  |
| NewValues | jsonb | Yes |  |  |
| IpAddress | character varying(64) | Yes |  |  |
| OccurredAt | timestamp with time zone | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |


## Breeds

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| SpeciesId | uuid | No | FK: Species(Id); UNIQUE: IX_Breeds_SpeciesId_Name |  |
| Name | character varying(100) | No | UNIQUE: IX_Breeds_SpeciesId_Name |  |
| Purpose | integer | No |  |  |
| Origin | character varying(100) | Yes |  |  |
| IsActive | boolean | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_Breeds_Species_SpeciesId`: SpeciesId -> Species(Id); delete behavior: `RESTRICT`.

## Diseases

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| Name | character varying(150) | No |  |  |
| Description | character varying(1000) | Yes |  |  |
| IsNotifiable | boolean | No |  |  |
| SpeciesId | uuid | Yes | FK: Species(Id) |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_Diseases_Species_SpeciesId`: SpeciesId -> Species(Id); delete behavior: `RESTRICT`.

## ExchangeRates

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| EffectiveDate | date | No |  |  |
| BolivarsPerDollar | numeric(18,6) | No |  |  |
| RecordedByUserId | uuid | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| EntryMethod | character varying(20) | No |  | 'manual'::character varying |
| IsDeleted | boolean | No |  | false |
| PublishedAt | timestamp with time zone | Yes |  |  |
| Source | character varying(400) | No |  | 'https://www.bcv.org.ve/'::character varying |


## FarmInventory

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id); UNIQUE: IX_FarmInventory_FarmId_ProductId |  |
| ProductId | uuid | No | FK: Products(Id); UNIQUE: IX_FarmInventory_FarmId_ProductId |  |
| Stock | numeric(14,4) | No |  | 0.0 |
| MinStock | numeric(14,4) | No |  | 5.0 |
| MaxStock | numeric(14,4) | No |  | 100.0 |
| Location | character varying(150) | No |  | 'Main warehouse'::character varying |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |
| Version | bigint | No |  | 0 |

- CHECK: `CONSTRAINT "CK_FarmInventory_Stock" CHECK ((("Stock" >= (0)::numeric) AND ("MinStock" >= (0)::numeric) AND ("MaxStock" > "MinStock")))`
- `FK_FarmInventory_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `RESTRICT`.
- `FK_FarmInventory_Products_ProductId`: ProductId -> Products(Id); delete behavior: `RESTRICT`.

## Farms

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| Name | character varying(150) | No |  |  |
| Code | character varying(20) | No | UNIQUE: IX_Farms_Code |  |
| Address | character varying(300) | Yes |  |  |
| Phone | character varying(50) | Yes |  |  |
| Email | character varying(200) | Yes |  |  |
| IsActive | boolean | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |


## FeedingRecords

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| LotId | uuid | No | FK: Lots(Id) |  |
| RationId | uuid | No | FK: Rations(Id) |  |
| Date | date | No |  |  |
| QuantityKg | numeric(10,3) | No |  |  |
| Cost | numeric(18,2) | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_FeedingRecords_Lots_LotId`: LotId -> Lots(Id); delete behavior: `CASCADE`.
- `FK_FeedingRecords_Rations_RationId`: RationId -> Rations(Id); delete behavior: `RESTRICT`.

## HealthEvents

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id) |  |
| AnimalId | uuid | No | FK: Animals(Id) |  |
| Date | date | No |  |  |
| UserId | uuid | Yes |  |  |
| Notes | character varying(1000) | Yes |  |  |
| Cost | numeric(18,2) | Yes |  |  |
| EventType | character varying(13) | No |  |  |
| ProductId | uuid | Yes | FK: Products(Id) |  |
| Dose | numeric(10,3) | Yes |  |  |
| DiseaseId | uuid | Yes | FK: Diseases(Id) |  |
| Severity | character varying(50) | Yes |  |  |
| IsContagious | boolean | Yes |  |  |
| Cause | character varying(300) | Yes |  |  |
| NecropsyNotes | character varying(1000) | Yes |  |  |
| StartDate | date | Yes |  |  |
| EndDate | date | Yes |  |  |
| Reason | character varying(500) | Yes |  |  |
| Treatment_ProductId | uuid | Yes | FK: Products(Id) |  |
| ProductBatchId | uuid | Yes | FK: ProductBatches(Id) |  |
| Treatment_Dose | numeric(10,3) | Yes |  |  |
| Route | integer | Yes |  |  |
| Treatment_StartDate | date | Yes |  |  |
| Treatment_EndDate | date | Yes |  |  |
| WithdrawalDays | integer | Yes |  |  |
| WithdrawalEndDate | date | Yes |  |  |
| Vaccination_ProductId | uuid | Yes | FK: Products(Id) |  |
| Vaccination_ProductBatchId | uuid | Yes | FK: ProductBatches(Id) |  |
| Vaccination_Dose | numeric(10,3) | Yes |  |  |
| NextDueDate | date | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_HealthEvents_Animals_AnimalId`: AnimalId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_HealthEvents_Diseases_DiseaseId`: DiseaseId -> Diseases(Id); delete behavior: `RESTRICT`.
- `FK_HealthEvents_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `RESTRICT`.
- `FK_HealthEvents_ProductBatches_ProductBatchId`: ProductBatchId -> ProductBatches(Id); delete behavior: `RESTRICT`.
- `FK_HealthEvents_ProductBatches_Vaccination_ProductBatchId`: Vaccination_ProductBatchId -> ProductBatches(Id); delete behavior: `RESTRICT`.
- `FK_HealthEvents_Products_ProductId`: ProductId -> Products(Id); delete behavior: `RESTRICT`.
- `FK_HealthEvents_Products_Treatment_ProductId`: Treatment_ProductId -> Products(Id); delete behavior: `RESTRICT`.
- `FK_HealthEvents_Products_Vaccination_ProductId`: Vaccination_ProductId -> Products(Id); delete behavior: `RESTRICT`.

## HealthStatusChanges

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| AnimalId | uuid | No | FK: Animals(Id) |  |
| PreviousStatus | integer | No |  |  |
| NewStatus | integer | No |  |  |
| ChangedAt | timestamp with time zone | No |  |  |
| Reason | character varying(500) | Yes |  |  |
| UserId | uuid | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_HealthStatusChanges_Animals_AnimalId`: AnimalId -> Animals(Id); delete behavior: `CASCADE`.

## InventoryCategories

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| Name | character varying(100) | No | UNIQUE: IX_InventoryCategories_Name |  |
| Description | character varying(500) | Yes |  |  |
| IsActive | boolean | No |  | true |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |


## Lots

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id); UNIQUE: IX_Lots_FarmId_Name |  |
| SpeciesId | uuid | No | FK: Species(Id) |  |
| PaddockId | uuid | Yes | FK: Paddocks(Id) |  |
| Name | character varying(150) | No | UNIQUE: IX_Lots_FarmId_Name |  |
| Purpose | integer | No |  |  |
| IsActive | boolean | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_Lots_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `RESTRICT`.
- `FK_Lots_Paddocks_PaddockId`: PaddockId -> Paddocks(Id); delete behavior: `RESTRICT`.
- `FK_Lots_Species_SpeciesId`: SpeciesId -> Species(Id); delete behavior: `RESTRICT`.

## Paddocks

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id); UNIQUE: IX_Paddocks_FarmId_Name |  |
| Name | character varying(150) | No | UNIQUE: IX_Paddocks_FarmId_Name |  |
| Code | character varying(30) | Yes |  |  |
| AreaHectares | numeric(14,4) | Yes |  |  |
| Capacity | integer | Yes |  |  |
| IsActive | boolean | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| MapHeight | numeric(7,4) | Yes |  |  |
| MapWidth | numeric(7,4) | Yes |  |  |
| MapX | numeric(7,4) | Yes |  |  |
| MapY | numeric(7,4) | Yes |  |  |
| MaxStayDays | integer | Yes |  |  |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_Paddocks_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `RESTRICT`.

## Permissions

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| Name | character varying(150) | No | UNIQUE: IX_Permissions_Name_GuardName |  |
| GuardName | character varying(100) | No | UNIQUE: IX_Permissions_Name_GuardName | 'web'::character varying |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |


## ProductBatches

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id) |  |
| ProductId | uuid | No | FK: Products(Id) |  |
| SupplierId | uuid | Yes | FK: Suppliers(Id) |  |
| BatchNumber | character varying(50) | Yes |  |  |
| ExpirationDate | date | Yes |  |  |
| InitialQuantity | numeric(12,3) | No |  |  |
| UnitCost | numeric(18,2) | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_ProductBatches_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `CASCADE`.
- `FK_ProductBatches_Products_ProductId`: ProductId -> Products(Id); delete behavior: `RESTRICT`.
- `FK_ProductBatches_Suppliers_SupplierId`: SupplierId -> Suppliers(Id); delete behavior: `RESTRICT`.

## Products

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| Name | character varying(150) | No |  |  |
| Unit | integer | No |  | 2 |
| WithdrawalDays | integer | Yes |  |  |
| RequiresPrescription | boolean | No |  |  |
| IsActive | boolean | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| Brand | character varying(100) | No |  | 'Generic'::character varying |
| CategoryId | uuid | No | FK: InventoryCategories(Id) | '00000000-0000-0000-0000-000000000000'::uuid |
| CostPrice | numeric(18,2) | No |  | 0.0 |
| Price | numeric(18,2) | No |  | 0.0 |
| SKU | character varying(50) | No | UNIQUE: IX_Products_SKU | ''::character varying |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- CHECK: `CONSTRAINT "CK_Products_Prices" CHECK (((NOT "IsActive") OR (("Price" > (0)::numeric) AND ("CostPrice" > (0)::numeric))))`
- `FK_Products_InventoryCategories_CategoryId`: CategoryId -> InventoryCategories(Id); delete behavior: `RESTRICT`.

## RationIngredients

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| RationId | uuid | No | FK: Rations(Id) |  |
| ProductId | uuid | No | FK: Products(Id) |  |
| Quantity | numeric(10,3) | No |  |  |
| Unit | integer | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_RationIngredients_Products_ProductId`: ProductId -> Products(Id); delete behavior: `RESTRICT`.
- `FK_RationIngredients_Rations_RationId`: RationId -> Rations(Id); delete behavior: `CASCADE`.

## Rations

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id) |  |
| SpeciesId | uuid | Yes | FK: Species(Id) |  |
| Name | character varying(150) | No |  |  |
| Purpose | character varying(200) | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_Rations_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `CASCADE`.
- `FK_Rations_Species_SpeciesId`: SpeciesId -> Species(Id); delete behavior: `RESTRICT`.

## RefreshTokens

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| Token | character varying(256) | No | UNIQUE: IX_RefreshTokens_Token |  |
| UserId | uuid | No | FK: Users(Id) |  |
| CreatedAt | timestamp with time zone | No |  |  |
| ExpiresAt | timestamp with time zone | No |  |  |
| RevokedAt | timestamp with time zone | Yes |  |  |

- `FK_RefreshTokens_Users_UserId`: UserId -> Users(Id); delete behavior: `CASCADE`.

## ReproductiveEvents

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id) |  |
| DamId | uuid | No | FK: Animals(Id) |  |
| Date | date | No |  |  |
| UserId | uuid | Yes |  |  |
| Notes | character varying(1000) | Yes |  |  |
| EventType | character varying(21) | No |  |  |
| Reason | character varying(500) | Yes |  |  |
| OffspringCount | integer | Yes |  |  |
| StillbornCount | integer | Yes |  |  |
| Difficulty | integer | Yes |  |  |
| Heat_Method | character varying(50) | Yes |  |  |
| SemenBatchId | uuid | Yes | FK: SemenBatches(Id) |  |
| Insemination_SireId | uuid | Yes | FK: Animals(Id) |  |
| Insemination_TechnicianUserId | uuid | Yes |  |  |
| Mating_Method | integer | Yes |  |  |
| SireId | uuid | Yes | FK: Animals(Id) |  |
| TechnicianUserId | uuid | Yes |  |  |
| Result | integer | Yes |  |  |
| Method | character varying(50) | Yes |  |  |
| ExpectedCalvingDate | date | Yes |  |  |
| OffspringId | uuid | Yes | FK: Animals(Id) |  |
| WeightKg | numeric(8,2) | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_ReproductiveEvents_Animals_DamId`: DamId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_ReproductiveEvents_Animals_Insemination_SireId`: Insemination_SireId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_ReproductiveEvents_Animals_OffspringId`: OffspringId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_ReproductiveEvents_Animals_SireId`: SireId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_ReproductiveEvents_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `RESTRICT`.
- `FK_ReproductiveEvents_SemenBatches_SemenBatchId`: SemenBatchId -> SemenBatches(Id); delete behavior: `RESTRICT`.

## RoleClaims

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | integer | No | PK |  |
| RoleId | uuid | No | FK: Roles(Id) |  |
| ClaimType | text | Yes |  |  |
| ClaimValue | text | Yes |  |  |

- `FK_RoleClaims_Roles_RoleId`: RoleId -> Roles(Id); delete behavior: `CASCADE`.

## RolePermissions

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| RoleId | uuid | No | PK; FK: Roles(Id) |  |
| PermissionId | uuid | No | PK; FK: Permissions(Id) |  |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_RolePermissions_Permissions_PermissionId`: PermissionId -> Permissions(Id); delete behavior: `CASCADE`.
- `FK_RolePermissions_Roles_RoleId`: RoleId -> Roles(Id); delete behavior: `CASCADE`.

## Roles

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| GuardName | character varying(100) | No |  | 'web'::character varying |
| Description | character varying(300) | Yes |  |  |
| Name | character varying(256) | Yes |  |  |
| NormalizedName | character varying(256) | Yes | UNIQUE: RoleNameIndex |  |
| ConcurrencyStamp | text | Yes |  |  |


## SemenBatches

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No | FK: Farms(Id) |  |
| BreedId | uuid | Yes | FK: Breeds(Id) |  |
| SireName | character varying(150) | Yes |  |  |
| SupplierId | uuid | Yes | FK: Suppliers(Id) |  |
| BatchNumber | character varying(50) | Yes |  |  |
| StrawCount | integer | Yes |  |  |
| StorageTank | character varying(50) | Yes |  |  |
| ExpirationDate | date | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_SemenBatches_Breeds_BreedId`: BreedId -> Breeds(Id); delete behavior: `RESTRICT`.
- `FK_SemenBatches_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `CASCADE`.
- `FK_SemenBatches_Suppliers_SupplierId`: SupplierId -> Suppliers(Id); delete behavior: `RESTRICT`.

## Species

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| Name | character varying(100) | No | UNIQUE: IX_Species_Name |  |
| Code | character varying(20) | No | UNIQUE: IX_Species_Code |  |
| Purpose | integer | No |  |  |
| GestationDays | integer | Yes |  |  |
| IsActive | boolean | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |


## StockMovements

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| ProductId | uuid | No | FK: Products(Id) |  |
| ProductBatchId | uuid | Yes | FK: ProductBatches(Id) |  |
| Type | integer | No |  |  |
| Quantity | numeric(14,4) | No |  |  |
| Reason | character varying(300) | Yes |  |  |
| Date | date | No |  |  |
| ReferenceType | character varying(100) | Yes |  |  |
| ReferenceId | uuid | Yes |  |  |
| UserId | uuid | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_StockMovements_ProductBatches_ProductBatchId`: ProductBatchId -> ProductBatches(Id); delete behavior: `RESTRICT`.
- `FK_StockMovements_Products_ProductId`: ProductId -> Products(Id); delete behavior: `RESTRICT`.

## Suppliers

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| Name | character varying(150) | No |  |  |
| ContactName | character varying(150) | Yes |  |  |
| Phone | character varying(50) | Yes |  |  |
| Email | character varying(200) | Yes |  |  |
| Address | character varying(300) | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |


## Tasks

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| Title | character varying(200) | No |  |  |
| Description | character varying(2000) | Yes |  |  |
| Type | integer | No |  |  |
| DueDate | date | Yes |  |  |
| Priority | integer | No |  |  |
| Status | integer | No |  |  |
| AssignedToUserId | uuid | Yes |  |  |
| RelatedAnimalId | uuid | Yes | FK: Animals(Id) |  |
| RelatedLotId | uuid | Yes | FK: Lots(Id) |  |
| CreatedByUserId | uuid | Yes |  |  |
| CompletedAt | timestamp with time zone | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_Tasks_Animals_RelatedAnimalId`: RelatedAnimalId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_Tasks_Lots_RelatedLotId`: RelatedLotId -> Lots(Id); delete behavior: `RESTRICT`.

## Transactions

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| Type | integer | No |  |  |
| Category | integer | No |  |  |
| Amount | numeric(18,2) | No |  |  |
| Currency | character varying(3) | No |  |  |
| Date | date | No |  |  |
| Description | character varying(500) | Yes |  |  |
| Counterparty | character varying(200) | Yes |  |  |
| AnimalId | uuid | Yes | FK: Animals(Id) |  |
| LotId | uuid | Yes | FK: Lots(Id) |  |
| ProductId | uuid | Yes | FK: Products(Id) |  |
| UserId | uuid | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_Transactions_Animals_AnimalId`: AnimalId -> Animals(Id); delete behavior: `RESTRICT`.
- `FK_Transactions_Lots_LotId`: LotId -> Lots(Id); delete behavior: `RESTRICT`.
- `FK_Transactions_Products_ProductId`: ProductId -> Products(Id); delete behavior: `RESTRICT`.

## UserClaims

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | integer | No | PK |  |
| UserId | uuid | No | FK: Users(Id) |  |
| ClaimType | text | Yes |  |  |
| ClaimValue | text | Yes |  |  |

- `FK_UserClaims_Users_UserId`: UserId -> Users(Id); delete behavior: `CASCADE`.

## UserFarms

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| UserId | uuid | No | FK: Users(Id); UNIQUE: IX_UserFarms_UserId_FarmId |  |
| FarmId | uuid | No | FK: Farms(Id); UNIQUE: IX_UserFarms_UserId_FarmId |  |
| IsDefault | boolean | No |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_UserFarms_Farms_FarmId`: FarmId -> Farms(Id); delete behavior: `CASCADE`.
- `FK_UserFarms_Users_UserId`: UserId -> Users(Id); delete behavior: `CASCADE`.

## UserLogins

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| LoginProvider | text | No | PK |  |
| ProviderKey | text | No | PK |  |
| ProviderDisplayName | text | Yes |  |  |
| UserId | uuid | No | FK: Users(Id) |  |

- `FK_UserLogins_Users_UserId`: UserId -> Users(Id); delete behavior: `CASCADE`.

## UserPermissions

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| UserId | uuid | No | PK; FK: Users(Id) |  |
| PermissionId | uuid | No | PK; FK: Permissions(Id) |  |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_UserPermissions_Permissions_PermissionId`: PermissionId -> Permissions(Id); delete behavior: `CASCADE`.
- `FK_UserPermissions_Users_UserId`: UserId -> Users(Id); delete behavior: `CASCADE`.

## UserRoles

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| UserId | uuid | No | PK; FK: Users(Id) |  |
| RoleId | uuid | No | PK; FK: Roles(Id) |  |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_UserRoles_Roles_RoleId`: RoleId -> Roles(Id); delete behavior: `CASCADE`.
- `FK_UserRoles_Users_UserId`: UserId -> Users(Id); delete behavior: `CASCADE`.

## UserTokens

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| UserId | uuid | No | PK; FK: Users(Id) |  |
| LoginProvider | text | No | PK |  |
| Name | text | No | PK |  |
| Value | text | Yes |  |  |

- `FK_UserTokens_Users_UserId`: UserId -> Users(Id); delete behavior: `CASCADE`.

## Users

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FullName | character varying(200) | No |  |  |
| IsSuperuser | boolean | No |  |  |
| IsStaff | boolean | No |  |  |
| IsActive | boolean | No |  |  |
| CreatedAt | timestamp with time zone | No |  |  |
| UserName | character varying(256) | Yes |  |  |
| NormalizedUserName | character varying(256) | Yes | UNIQUE: UserNameIndex |  |
| Email | character varying(256) | Yes |  |  |
| NormalizedEmail | character varying(256) | Yes | UNIQUE: EmailIndex |  |
| EmailConfirmed | boolean | No |  |  |
| PasswordHash | text | Yes |  |  |
| SecurityStamp | text | Yes |  |  |
| ConcurrencyStamp | text | Yes |  |  |
| PhoneNumber | text | Yes |  |  |
| PhoneNumberConfirmed | boolean | No |  |  |
| TwoFactorEnabled | boolean | No |  |  |
| LockoutEnd | timestamp with time zone | Yes |  |  |
| LockoutEnabled | boolean | No |  |  |
| AccessFailedCount | integer | No |  |  |


## WeightRecords

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| Id | uuid | No | PK |  |
| FarmId | uuid | No |  |  |
| AnimalId | uuid | No | FK: Animals(Id) |  |
| Date | date | No |  |  |
| WeightKg | numeric(8,2) | No |  |  |
| BodyConditionScore | numeric(4,2) | Yes |  |  |
| RecordedByUserId | uuid | Yes |  |  |
| Notes | character varying(500) | Yes |  |  |
| CreatedAt | timestamp with time zone | No |  | now() |
| DeletedAt | timestamp with time zone | Yes |  |  |
| DeletedByUserId | uuid | Yes |  |  |
| IsDeleted | boolean | No |  | false |

- `FK_WeightRecords_Animals_AnimalId`: AnimalId -> Animals(Id); delete behavior: `RESTRICT`.

## __EFMigrationsHistory

| Column | PostgreSQL type | Nullable | Key / constraint | Default |
| --- | --- | --- | --- | --- |
| MigrationId | character varying(150) | No | PK |  |
| ProductVersion | character varying(32) | No |  |  |
