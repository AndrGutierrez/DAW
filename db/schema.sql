--
-- PostgreSQL database dump
--


-- Dumped from database version 17.11
-- Dumped by pg_dump version 17.11

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

-- *not* creating schema, since initdb creates it


--
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA public IS '';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: AlertRules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AlertRules" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "Type" integer NOT NULL,
    "ThresholdValue" numeric(12,2),
    "ThresholdDays" integer,
    "IsEnabled" boolean NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Alerts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Alerts" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "Type" integer NOT NULL,
    "Severity" integer NOT NULL,
    "Message" character varying(500) NOT NULL,
    "RelatedEntityType" character varying(100),
    "RelatedEntityId" uuid,
    "DueDate" date,
    "IsResolved" boolean NOT NULL,
    "ResolvedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: AnimalMovements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AnimalMovements" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "AnimalId" uuid NOT NULL,
    "FromPaddockId" uuid,
    "ToPaddockId" uuid,
    "FromLotId" uuid,
    "ToLotId" uuid,
    "Date" date NOT NULL,
    "Reason" character varying(300),
    "UserId" uuid,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: AnimalPhotos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AnimalPhotos" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "AnimalId" uuid NOT NULL,
    "Url" character varying(500) NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "ContentType" character varying(100),
    "SizeBytes" bigint,
    "UploadedByUserId" uuid,
    "UploadedAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Animals; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Animals" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "SpeciesId" uuid NOT NULL,
    "BreedId" uuid,
    "LotId" uuid,
    "PaddockId" uuid,
    "InternalTag" character varying(50) NOT NULL,
    "OfficialId" character varying(50),
    "Rfid" character varying(50),
    "Name" character varying(100),
    "Sex" integer NOT NULL,
    "BirthDate" date,
    "BirthWeightKg" numeric(14,4),
    "Color" character varying(50),
    "Markings" character varying(200),
    "Status" integer NOT NULL,
    "Origin" integer NOT NULL,
    "Purpose" integer NOT NULL,
    "HealthStatus" integer NOT NULL,
    "DamId" uuid,
    "SireId" uuid,
    "Notes" character varying(2000),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "UpdatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Attachments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Attachments" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "OwnerType" character varying(100) NOT NULL,
    "OwnerId" uuid NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "Url" character varying(500) NOT NULL,
    "ContentType" character varying(100),
    "SizeBytes" bigint,
    "UploadedByUserId" uuid,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: AuditLogs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AuditLogs" (
    "Id" uuid NOT NULL,
    "UserId" uuid,
    "FarmId" uuid,
    "Action" character varying(50) NOT NULL,
    "EntityName" character varying(150) NOT NULL,
    "EntityId" character varying(100),
    "OldValues" jsonb,
    "NewValues" jsonb,
    "IpAddress" character varying(64),
    "OccurredAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Breeds; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Breeds" (
    "Id" uuid NOT NULL,
    "SpeciesId" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Purpose" integer NOT NULL,
    "Origin" character varying(100),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Diseases; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Diseases" (
    "Id" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "Description" character varying(1000),
    "IsNotifiable" boolean NOT NULL,
    "SpeciesId" uuid,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: EggProductionRecords; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."EggProductionRecords" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "LotId" uuid NOT NULL,
    "Date" date NOT NULL,
    "TotalEggs" integer NOT NULL,
    "BrokenEggs" integer NOT NULL,
    "AverageWeightGrams" numeric(8,2),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Farms; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Farms" (
    "Id" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "Code" character varying(20) NOT NULL,
    "Address" character varying(300),
    "Phone" character varying(50),
    "Email" character varying(200),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: FeedingRecords; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."FeedingRecords" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "LotId" uuid NOT NULL,
    "RationId" uuid NOT NULL,
    "Date" date NOT NULL,
    "QuantityKg" numeric(10,3) NOT NULL,
    "Cost" numeric(12,2),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: HealthEvents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."HealthEvents" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "AnimalId" uuid NOT NULL,
    "Date" date NOT NULL,
    "UserId" uuid,
    "Notes" character varying(1000),
    "Cost" numeric(12,2),
    "EventType" character varying(13) NOT NULL,
    "ProductId" uuid,
    "Dose" numeric(10,3),
    "DiseaseId" uuid,
    "Severity" character varying(50),
    "IsContagious" boolean,
    "Cause" character varying(300),
    "NecropsyNotes" character varying(1000),
    "StartDate" date,
    "EndDate" date,
    "Reason" character varying(500),
    "Treatment_ProductId" uuid,
    "ProductBatchId" uuid,
    "Treatment_Dose" numeric(10,3),
    "Route" integer,
    "Treatment_StartDate" date,
    "Treatment_EndDate" date,
    "WithdrawalDays" integer,
    "WithdrawalEndDate" date,
    "Vaccination_ProductId" uuid,
    "Vaccination_ProductBatchId" uuid,
    "Vaccination_Dose" numeric(10,3),
    "NextDueDate" date,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: HealthStatusChanges; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."HealthStatusChanges" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "AnimalId" uuid NOT NULL,
    "PreviousStatus" integer NOT NULL,
    "NewStatus" integer NOT NULL,
    "ChangedAt" timestamp with time zone NOT NULL,
    "Reason" character varying(500),
    "UserId" uuid,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Lots; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Lots" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "SpeciesId" uuid NOT NULL,
    "PaddockId" uuid,
    "Name" character varying(150) NOT NULL,
    "Purpose" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: MilkProductionRecords; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."MilkProductionRecords" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "AnimalId" uuid NOT NULL,
    "Date" date NOT NULL,
    "Shift" integer NOT NULL,
    "Liters" numeric(8,2) NOT NULL,
    "FatPercent" numeric(5,2),
    "ProteinPercent" numeric(5,2),
    "SomaticCellCount" integer,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Paddocks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Paddocks" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "Code" character varying(30),
    "AreaHectares" numeric(14,4),
    "Capacity" integer,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Permissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Permissions" (
    "Id" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "GuardName" character varying(100) DEFAULT 'web'::character varying NOT NULL
);


--
-- Name: ProductBatches; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ProductBatches" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "SupplierId" uuid,
    "BatchNumber" character varying(50),
    "ExpirationDate" date,
    "InitialQuantity" numeric(12,3) NOT NULL,
    "UnitCost" numeric(12,4),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Products; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Products" (
    "Id" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "Category" integer NOT NULL,
    "Unit" integer NOT NULL,
    "WithdrawalDays" integer,
    "RequiresPrescription" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: RationIngredients; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RationIngredients" (
    "Id" uuid NOT NULL,
    "RationId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" numeric(10,3) NOT NULL,
    "Unit" integer NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Rations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Rations" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "SpeciesId" uuid,
    "Name" character varying(150) NOT NULL,
    "Purpose" character varying(200),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: RefreshTokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RefreshTokens" (
    "Id" uuid NOT NULL,
    "Token" character varying(256) NOT NULL,
    "UserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "RevokedAt" timestamp with time zone
);


--
-- Name: ReproductiveEvents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ReproductiveEvents" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "DamId" uuid NOT NULL,
    "Date" date NOT NULL,
    "UserId" uuid,
    "Notes" character varying(1000),
    "EventType" character varying(21) NOT NULL,
    "Reason" character varying(500),
    "OffspringCount" integer,
    "StillbornCount" integer,
    "Difficulty" integer,
    "Heat_Method" character varying(50),
    "SemenBatchId" uuid,
    "Insemination_SireId" uuid,
    "Insemination_TechnicianUserId" uuid,
    "Mating_Method" integer,
    "SireId" uuid,
    "TechnicianUserId" uuid,
    "Result" integer,
    "Method" character varying(50),
    "ExpectedCalvingDate" date,
    "OffspringId" uuid,
    "WeightKg" numeric(8,2),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: RoleClaims; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RoleClaims" (
    "Id" integer NOT NULL,
    "RoleId" uuid NOT NULL,
    "ClaimType" text,
    "ClaimValue" text
);


--
-- Name: RoleClaims_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."RoleClaims" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."RoleClaims_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: RolePermissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RolePermissions" (
    "RoleId" uuid NOT NULL,
    "PermissionId" uuid NOT NULL
);


--
-- Name: Roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Roles" (
    "Id" uuid NOT NULL,
    "GuardName" character varying(100) DEFAULT 'web'::character varying NOT NULL,
    "Description" character varying(300),
    "Name" character varying(256),
    "NormalizedName" character varying(256),
    "ConcurrencyStamp" text
);


--
-- Name: SemenBatches; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SemenBatches" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "BreedId" uuid,
    "SireName" character varying(150),
    "SupplierId" uuid,
    "BatchNumber" character varying(50),
    "StrawCount" integer,
    "StorageTank" character varying(50),
    "ExpirationDate" date,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: SlaughterRecords; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SlaughterRecords" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "AnimalId" uuid NOT NULL,
    "Date" date NOT NULL,
    "LiveWeightKg" numeric(8,2),
    "CarcassWeightKg" numeric(8,2),
    "ColdCarcassWeightKg" numeric(8,2),
    "Grade" character varying(50),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Species; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Species" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(20) NOT NULL,
    "Purpose" integer NOT NULL,
    "GestationDays" integer,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: StockMovements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."StockMovements" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "ProductBatchId" uuid,
    "Type" integer NOT NULL,
    "Quantity" numeric(12,3) NOT NULL,
    "Reason" character varying(300),
    "Date" date NOT NULL,
    "ReferenceType" character varying(100),
    "ReferenceId" uuid,
    "UserId" uuid,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Suppliers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Suppliers" (
    "Id" uuid NOT NULL,
    "Name" character varying(150) NOT NULL,
    "ContactName" character varying(150),
    "Phone" character varying(50),
    "Email" character varying(200),
    "Address" character varying(300),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Tasks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Tasks" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Description" character varying(2000),
    "Type" integer NOT NULL,
    "DueDate" date,
    "Priority" integer NOT NULL,
    "Status" integer NOT NULL,
    "AssignedToUserId" uuid,
    "RelatedAnimalId" uuid,
    "RelatedLotId" uuid,
    "CreatedByUserId" uuid,
    "CompletedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Transactions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Transactions" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "Type" integer NOT NULL,
    "Category" integer NOT NULL,
    "Amount" numeric(14,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Date" date NOT NULL,
    "Description" character varying(500),
    "Counterparty" character varying(200),
    "AnimalId" uuid,
    "LotId" uuid,
    "ProductId" uuid,
    "UserId" uuid,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: UserClaims; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserClaims" (
    "Id" integer NOT NULL,
    "UserId" uuid NOT NULL,
    "ClaimType" text,
    "ClaimValue" text
);


--
-- Name: UserClaims_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."UserClaims" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."UserClaims_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: UserFarms; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserFarms" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "IsDefault" boolean NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: UserLogins; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserLogins" (
    "LoginProvider" text NOT NULL,
    "ProviderKey" text NOT NULL,
    "ProviderDisplayName" text,
    "UserId" uuid NOT NULL
);


--
-- Name: UserPermissions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserPermissions" (
    "UserId" uuid NOT NULL,
    "PermissionId" uuid NOT NULL
);


--
-- Name: UserRoles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserRoles" (
    "UserId" uuid NOT NULL,
    "RoleId" uuid NOT NULL
);


--
-- Name: UserTokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserTokens" (
    "UserId" uuid NOT NULL,
    "LoginProvider" text NOT NULL,
    "Name" text NOT NULL,
    "Value" text
);


--
-- Name: Users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Users" (
    "Id" uuid NOT NULL,
    "FullName" character varying(200) NOT NULL,
    "IsSuperuser" boolean NOT NULL,
    "IsStaff" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UserName" character varying(256),
    "NormalizedUserName" character varying(256),
    "Email" character varying(256),
    "NormalizedEmail" character varying(256),
    "EmailConfirmed" boolean NOT NULL,
    "PasswordHash" text,
    "SecurityStamp" text,
    "ConcurrencyStamp" text,
    "PhoneNumber" text,
    "PhoneNumberConfirmed" boolean NOT NULL,
    "TwoFactorEnabled" boolean NOT NULL,
    "LockoutEnd" timestamp with time zone,
    "LockoutEnabled" boolean NOT NULL,
    "AccessFailedCount" integer NOT NULL
);


--
-- Name: WeightRecords; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."WeightRecords" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "AnimalId" uuid NOT NULL,
    "Date" date NOT NULL,
    "WeightKg" numeric(8,2) NOT NULL,
    "BodyConditionScore" numeric(4,2),
    "RecordedByUserId" uuid,
    "Notes" character varying(500),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: WoolProductionRecords; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."WoolProductionRecords" (
    "Id" uuid NOT NULL,
    "FarmId" uuid NOT NULL,
    "AnimalId" uuid NOT NULL,
    "Date" date NOT NULL,
    "FleeceWeightKg" numeric(8,2) NOT NULL,
    "FiberDiameterMicrons" numeric(8,2),
    "Grade" character varying(50),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: AlertRules PK_AlertRules; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AlertRules"
    ADD CONSTRAINT "PK_AlertRules" PRIMARY KEY ("Id");


--
-- Name: Alerts PK_Alerts; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Alerts"
    ADD CONSTRAINT "PK_Alerts" PRIMARY KEY ("Id");


--
-- Name: AnimalMovements PK_AnimalMovements; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AnimalMovements"
    ADD CONSTRAINT "PK_AnimalMovements" PRIMARY KEY ("Id");


--
-- Name: AnimalPhotos PK_AnimalPhotos; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AnimalPhotos"
    ADD CONSTRAINT "PK_AnimalPhotos" PRIMARY KEY ("Id");


--
-- Name: Animals PK_Animals; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Animals"
    ADD CONSTRAINT "PK_Animals" PRIMARY KEY ("Id");


--
-- Name: Attachments PK_Attachments; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Attachments"
    ADD CONSTRAINT "PK_Attachments" PRIMARY KEY ("Id");


--
-- Name: AuditLogs PK_AuditLogs; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AuditLogs"
    ADD CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id");


--
-- Name: Breeds PK_Breeds; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Breeds"
    ADD CONSTRAINT "PK_Breeds" PRIMARY KEY ("Id");


--
-- Name: Diseases PK_Diseases; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Diseases"
    ADD CONSTRAINT "PK_Diseases" PRIMARY KEY ("Id");


--
-- Name: EggProductionRecords PK_EggProductionRecords; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EggProductionRecords"
    ADD CONSTRAINT "PK_EggProductionRecords" PRIMARY KEY ("Id");


--
-- Name: Farms PK_Farms; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Farms"
    ADD CONSTRAINT "PK_Farms" PRIMARY KEY ("Id");


--
-- Name: FeedingRecords PK_FeedingRecords; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FeedingRecords"
    ADD CONSTRAINT "PK_FeedingRecords" PRIMARY KEY ("Id");


--
-- Name: HealthEvents PK_HealthEvents; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthEvents"
    ADD CONSTRAINT "PK_HealthEvents" PRIMARY KEY ("Id");


--
-- Name: HealthStatusChanges PK_HealthStatusChanges; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthStatusChanges"
    ADD CONSTRAINT "PK_HealthStatusChanges" PRIMARY KEY ("Id");


--
-- Name: Lots PK_Lots; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Lots"
    ADD CONSTRAINT "PK_Lots" PRIMARY KEY ("Id");


--
-- Name: MilkProductionRecords PK_MilkProductionRecords; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MilkProductionRecords"
    ADD CONSTRAINT "PK_MilkProductionRecords" PRIMARY KEY ("Id");


--
-- Name: Paddocks PK_Paddocks; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Paddocks"
    ADD CONSTRAINT "PK_Paddocks" PRIMARY KEY ("Id");


--
-- Name: Permissions PK_Permissions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Permissions"
    ADD CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id");


--
-- Name: ProductBatches PK_ProductBatches; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProductBatches"
    ADD CONSTRAINT "PK_ProductBatches" PRIMARY KEY ("Id");


--
-- Name: Products PK_Products; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Products"
    ADD CONSTRAINT "PK_Products" PRIMARY KEY ("Id");


--
-- Name: RationIngredients PK_RationIngredients; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RationIngredients"
    ADD CONSTRAINT "PK_RationIngredients" PRIMARY KEY ("Id");


--
-- Name: Rations PK_Rations; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Rations"
    ADD CONSTRAINT "PK_Rations" PRIMARY KEY ("Id");


--
-- Name: RefreshTokens PK_RefreshTokens; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id");


--
-- Name: ReproductiveEvents PK_ReproductiveEvents; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReproductiveEvents"
    ADD CONSTRAINT "PK_ReproductiveEvents" PRIMARY KEY ("Id");


--
-- Name: RoleClaims PK_RoleClaims; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RoleClaims"
    ADD CONSTRAINT "PK_RoleClaims" PRIMARY KEY ("Id");


--
-- Name: RolePermissions PK_RolePermissions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RolePermissions"
    ADD CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("RoleId", "PermissionId");


--
-- Name: Roles PK_Roles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Roles"
    ADD CONSTRAINT "PK_Roles" PRIMARY KEY ("Id");


--
-- Name: SemenBatches PK_SemenBatches; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SemenBatches"
    ADD CONSTRAINT "PK_SemenBatches" PRIMARY KEY ("Id");


--
-- Name: SlaughterRecords PK_SlaughterRecords; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SlaughterRecords"
    ADD CONSTRAINT "PK_SlaughterRecords" PRIMARY KEY ("Id");


--
-- Name: Species PK_Species; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Species"
    ADD CONSTRAINT "PK_Species" PRIMARY KEY ("Id");


--
-- Name: StockMovements PK_StockMovements; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "PK_StockMovements" PRIMARY KEY ("Id");


--
-- Name: Suppliers PK_Suppliers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Suppliers"
    ADD CONSTRAINT "PK_Suppliers" PRIMARY KEY ("Id");


--
-- Name: Tasks PK_Tasks; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Tasks"
    ADD CONSTRAINT "PK_Tasks" PRIMARY KEY ("Id");


--
-- Name: Transactions PK_Transactions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "PK_Transactions" PRIMARY KEY ("Id");


--
-- Name: UserClaims PK_UserClaims; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserClaims"
    ADD CONSTRAINT "PK_UserClaims" PRIMARY KEY ("Id");


--
-- Name: UserFarms PK_UserFarms; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserFarms"
    ADD CONSTRAINT "PK_UserFarms" PRIMARY KEY ("Id");


--
-- Name: UserLogins PK_UserLogins; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserLogins"
    ADD CONSTRAINT "PK_UserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey");


--
-- Name: UserPermissions PK_UserPermissions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserPermissions"
    ADD CONSTRAINT "PK_UserPermissions" PRIMARY KEY ("UserId", "PermissionId");


--
-- Name: UserRoles PK_UserRoles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserRoles"
    ADD CONSTRAINT "PK_UserRoles" PRIMARY KEY ("UserId", "RoleId");


--
-- Name: UserTokens PK_UserTokens; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserTokens"
    ADD CONSTRAINT "PK_UserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name");


--
-- Name: Users PK_Users; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");


--
-- Name: WeightRecords PK_WeightRecords; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."WeightRecords"
    ADD CONSTRAINT "PK_WeightRecords" PRIMARY KEY ("Id");


--
-- Name: WoolProductionRecords PK_WoolProductionRecords; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."WoolProductionRecords"
    ADD CONSTRAINT "PK_WoolProductionRecords" PRIMARY KEY ("Id");


--
-- Name: EmailIndex; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "EmailIndex" ON public."Users" USING btree ("NormalizedEmail");


--
-- Name: IX_AlertRules_FarmId_Type; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AlertRules_FarmId_Type" ON public."AlertRules" USING btree ("FarmId", "Type");


--
-- Name: IX_Alerts_FarmId_IsResolved_Severity; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Alerts_FarmId_IsResolved_Severity" ON public."Alerts" USING btree ("FarmId", "IsResolved", "Severity");


--
-- Name: IX_AnimalMovements_AnimalId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AnimalMovements_AnimalId_Date" ON public."AnimalMovements" USING btree ("AnimalId", "Date");


--
-- Name: IX_AnimalMovements_FromLotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AnimalMovements_FromLotId" ON public."AnimalMovements" USING btree ("FromLotId");


--
-- Name: IX_AnimalMovements_FromPaddockId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AnimalMovements_FromPaddockId" ON public."AnimalMovements" USING btree ("FromPaddockId");


--
-- Name: IX_AnimalMovements_ToLotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AnimalMovements_ToLotId" ON public."AnimalMovements" USING btree ("ToLotId");


--
-- Name: IX_AnimalMovements_ToPaddockId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AnimalMovements_ToPaddockId" ON public."AnimalMovements" USING btree ("ToPaddockId");


--
-- Name: IX_AnimalPhotos_AnimalId_UploadedAt; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AnimalPhotos_AnimalId_UploadedAt" ON public."AnimalPhotos" USING btree ("AnimalId", "UploadedAt");


--
-- Name: IX_Animals_BreedId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Animals_BreedId" ON public."Animals" USING btree ("BreedId");


--
-- Name: IX_Animals_DamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Animals_DamId" ON public."Animals" USING btree ("DamId");


--
-- Name: IX_Animals_FarmId_InternalTag; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Animals_FarmId_InternalTag" ON public."Animals" USING btree ("FarmId", "InternalTag");


--
-- Name: IX_Animals_FarmId_OfficialId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Animals_FarmId_OfficialId" ON public."Animals" USING btree ("FarmId", "OfficialId");


--
-- Name: IX_Animals_FarmId_Rfid; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Animals_FarmId_Rfid" ON public."Animals" USING btree ("FarmId", "Rfid");


--
-- Name: IX_Animals_LotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Animals_LotId" ON public."Animals" USING btree ("LotId");


--
-- Name: IX_Animals_PaddockId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Animals_PaddockId" ON public."Animals" USING btree ("PaddockId");


--
-- Name: IX_Animals_SireId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Animals_SireId" ON public."Animals" USING btree ("SireId");


--
-- Name: IX_Animals_SpeciesId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Animals_SpeciesId" ON public."Animals" USING btree ("SpeciesId");


--
-- Name: IX_Attachments_OwnerType_OwnerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Attachments_OwnerType_OwnerId" ON public."Attachments" USING btree ("OwnerType", "OwnerId");


--
-- Name: IX_AuditLogs_EntityName_EntityId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AuditLogs_EntityName_EntityId" ON public."AuditLogs" USING btree ("EntityName", "EntityId");


--
-- Name: IX_Breeds_SpeciesId_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Breeds_SpeciesId_Name" ON public."Breeds" USING btree ("SpeciesId", "Name");


--
-- Name: IX_Diseases_SpeciesId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Diseases_SpeciesId" ON public."Diseases" USING btree ("SpeciesId");


--
-- Name: IX_EggProductionRecords_LotId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EggProductionRecords_LotId_Date" ON public."EggProductionRecords" USING btree ("LotId", "Date");


--
-- Name: IX_Farms_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Farms_Code" ON public."Farms" USING btree ("Code");


--
-- Name: IX_FeedingRecords_LotId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_FeedingRecords_LotId_Date" ON public."FeedingRecords" USING btree ("LotId", "Date");


--
-- Name: IX_FeedingRecords_RationId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_FeedingRecords_RationId" ON public."FeedingRecords" USING btree ("RationId");


--
-- Name: IX_HealthEvents_AnimalId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_HealthEvents_AnimalId_Date" ON public."HealthEvents" USING btree ("AnimalId", "Date");


--
-- Name: IX_HealthEvents_DiseaseId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_HealthEvents_DiseaseId" ON public."HealthEvents" USING btree ("DiseaseId");


--
-- Name: IX_HealthEvents_FarmId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_HealthEvents_FarmId" ON public."HealthEvents" USING btree ("FarmId");


--
-- Name: IX_HealthEvents_ProductBatchId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_HealthEvents_ProductBatchId" ON public."HealthEvents" USING btree ("ProductBatchId");


--
-- Name: IX_HealthEvents_ProductId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_HealthEvents_ProductId" ON public."HealthEvents" USING btree ("ProductId");


--
-- Name: IX_HealthEvents_Treatment_ProductId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_HealthEvents_Treatment_ProductId" ON public."HealthEvents" USING btree ("Treatment_ProductId");


--
-- Name: IX_HealthEvents_Vaccination_ProductBatchId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_HealthEvents_Vaccination_ProductBatchId" ON public."HealthEvents" USING btree ("Vaccination_ProductBatchId");


--
-- Name: IX_HealthEvents_Vaccination_ProductId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_HealthEvents_Vaccination_ProductId" ON public."HealthEvents" USING btree ("Vaccination_ProductId");


--
-- Name: IX_HealthStatusChanges_AnimalId_ChangedAt; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_HealthStatusChanges_AnimalId_ChangedAt" ON public."HealthStatusChanges" USING btree ("AnimalId", "ChangedAt");


--
-- Name: IX_Lots_FarmId_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Lots_FarmId_Name" ON public."Lots" USING btree ("FarmId", "Name");


--
-- Name: IX_Lots_PaddockId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Lots_PaddockId" ON public."Lots" USING btree ("PaddockId");


--
-- Name: IX_Lots_SpeciesId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Lots_SpeciesId" ON public."Lots" USING btree ("SpeciesId");


--
-- Name: IX_MilkProductionRecords_AnimalId_Date_Shift; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MilkProductionRecords_AnimalId_Date_Shift" ON public."MilkProductionRecords" USING btree ("AnimalId", "Date", "Shift");


--
-- Name: IX_Paddocks_FarmId_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Paddocks_FarmId_Name" ON public."Paddocks" USING btree ("FarmId", "Name");


--
-- Name: IX_Permissions_Name_GuardName; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Permissions_Name_GuardName" ON public."Permissions" USING btree ("Name", "GuardName");


--
-- Name: IX_ProductBatches_FarmId_ProductId_BatchNumber; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ProductBatches_FarmId_ProductId_BatchNumber" ON public."ProductBatches" USING btree ("FarmId", "ProductId", "BatchNumber");


--
-- Name: IX_ProductBatches_ProductId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ProductBatches_ProductId" ON public."ProductBatches" USING btree ("ProductId");


--
-- Name: IX_ProductBatches_SupplierId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ProductBatches_SupplierId" ON public."ProductBatches" USING btree ("SupplierId");


--
-- Name: IX_Products_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Products_Name" ON public."Products" USING btree ("Name");


--
-- Name: IX_RationIngredients_ProductId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RationIngredients_ProductId" ON public."RationIngredients" USING btree ("ProductId");


--
-- Name: IX_RationIngredients_RationId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RationIngredients_RationId" ON public."RationIngredients" USING btree ("RationId");


--
-- Name: IX_Rations_FarmId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Rations_FarmId" ON public."Rations" USING btree ("FarmId");


--
-- Name: IX_Rations_SpeciesId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Rations_SpeciesId" ON public."Rations" USING btree ("SpeciesId");


--
-- Name: IX_RefreshTokens_Token; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON public."RefreshTokens" USING btree ("Token");


--
-- Name: IX_RefreshTokens_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RefreshTokens_UserId" ON public."RefreshTokens" USING btree ("UserId");


--
-- Name: IX_ReproductiveEvents_DamId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ReproductiveEvents_DamId_Date" ON public."ReproductiveEvents" USING btree ("DamId", "Date");


--
-- Name: IX_ReproductiveEvents_FarmId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ReproductiveEvents_FarmId" ON public."ReproductiveEvents" USING btree ("FarmId");


--
-- Name: IX_ReproductiveEvents_Insemination_SireId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ReproductiveEvents_Insemination_SireId" ON public."ReproductiveEvents" USING btree ("Insemination_SireId");


--
-- Name: IX_ReproductiveEvents_OffspringId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ReproductiveEvents_OffspringId" ON public."ReproductiveEvents" USING btree ("OffspringId");


--
-- Name: IX_ReproductiveEvents_SemenBatchId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ReproductiveEvents_SemenBatchId" ON public."ReproductiveEvents" USING btree ("SemenBatchId");


--
-- Name: IX_ReproductiveEvents_SireId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ReproductiveEvents_SireId" ON public."ReproductiveEvents" USING btree ("SireId");


--
-- Name: IX_RoleClaims_RoleId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RoleClaims_RoleId" ON public."RoleClaims" USING btree ("RoleId");


--
-- Name: IX_RolePermissions_PermissionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RolePermissions_PermissionId" ON public."RolePermissions" USING btree ("PermissionId");


--
-- Name: IX_SemenBatches_BreedId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SemenBatches_BreedId" ON public."SemenBatches" USING btree ("BreedId");


--
-- Name: IX_SemenBatches_FarmId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SemenBatches_FarmId" ON public."SemenBatches" USING btree ("FarmId");


--
-- Name: IX_SemenBatches_SupplierId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SemenBatches_SupplierId" ON public."SemenBatches" USING btree ("SupplierId");


--
-- Name: IX_SlaughterRecords_AnimalId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SlaughterRecords_AnimalId_Date" ON public."SlaughterRecords" USING btree ("AnimalId", "Date");


--
-- Name: IX_Species_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Species_Code" ON public."Species" USING btree ("Code");


--
-- Name: IX_Species_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Species_Name" ON public."Species" USING btree ("Name");


--
-- Name: IX_StockMovements_FarmId_ProductId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_StockMovements_FarmId_ProductId_Date" ON public."StockMovements" USING btree ("FarmId", "ProductId", "Date");


--
-- Name: IX_StockMovements_ProductBatchId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_StockMovements_ProductBatchId" ON public."StockMovements" USING btree ("ProductBatchId");


--
-- Name: IX_StockMovements_ProductId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_StockMovements_ProductId" ON public."StockMovements" USING btree ("ProductId");


--
-- Name: IX_Tasks_FarmId_Status_DueDate; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tasks_FarmId_Status_DueDate" ON public."Tasks" USING btree ("FarmId", "Status", "DueDate");


--
-- Name: IX_Tasks_RelatedAnimalId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tasks_RelatedAnimalId" ON public."Tasks" USING btree ("RelatedAnimalId");


--
-- Name: IX_Tasks_RelatedLotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tasks_RelatedLotId" ON public."Tasks" USING btree ("RelatedLotId");


--
-- Name: IX_Transactions_AnimalId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Transactions_AnimalId" ON public."Transactions" USING btree ("AnimalId");


--
-- Name: IX_Transactions_FarmId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Transactions_FarmId_Date" ON public."Transactions" USING btree ("FarmId", "Date");


--
-- Name: IX_Transactions_LotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Transactions_LotId" ON public."Transactions" USING btree ("LotId");


--
-- Name: IX_Transactions_ProductId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Transactions_ProductId" ON public."Transactions" USING btree ("ProductId");


--
-- Name: IX_UserClaims_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_UserClaims_UserId" ON public."UserClaims" USING btree ("UserId");


--
-- Name: IX_UserFarms_FarmId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_UserFarms_FarmId" ON public."UserFarms" USING btree ("FarmId");


--
-- Name: IX_UserFarms_UserId_FarmId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_UserFarms_UserId_FarmId" ON public."UserFarms" USING btree ("UserId", "FarmId");


--
-- Name: IX_UserLogins_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_UserLogins_UserId" ON public."UserLogins" USING btree ("UserId");


--
-- Name: IX_UserPermissions_PermissionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_UserPermissions_PermissionId" ON public."UserPermissions" USING btree ("PermissionId");


--
-- Name: IX_UserRoles_RoleId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_UserRoles_RoleId" ON public."UserRoles" USING btree ("RoleId");


--
-- Name: IX_WeightRecords_AnimalId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_WeightRecords_AnimalId_Date" ON public."WeightRecords" USING btree ("AnimalId", "Date");


--
-- Name: IX_WoolProductionRecords_AnimalId_Date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_WoolProductionRecords_AnimalId_Date" ON public."WoolProductionRecords" USING btree ("AnimalId", "Date");


--
-- Name: RoleNameIndex; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "RoleNameIndex" ON public."Roles" USING btree ("NormalizedName");


--
-- Name: UserNameIndex; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "UserNameIndex" ON public."Users" USING btree ("NormalizedUserName");


--
-- Name: AnimalMovements FK_AnimalMovements_Animals_AnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AnimalMovements"
    ADD CONSTRAINT "FK_AnimalMovements_Animals_AnimalId" FOREIGN KEY ("AnimalId") REFERENCES public."Animals"("Id") ON DELETE CASCADE;


--
-- Name: AnimalMovements FK_AnimalMovements_Lots_FromLotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AnimalMovements"
    ADD CONSTRAINT "FK_AnimalMovements_Lots_FromLotId" FOREIGN KEY ("FromLotId") REFERENCES public."Lots"("Id") ON DELETE RESTRICT;


--
-- Name: AnimalMovements FK_AnimalMovements_Lots_ToLotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AnimalMovements"
    ADD CONSTRAINT "FK_AnimalMovements_Lots_ToLotId" FOREIGN KEY ("ToLotId") REFERENCES public."Lots"("Id") ON DELETE RESTRICT;


--
-- Name: AnimalMovements FK_AnimalMovements_Paddocks_FromPaddockId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AnimalMovements"
    ADD CONSTRAINT "FK_AnimalMovements_Paddocks_FromPaddockId" FOREIGN KEY ("FromPaddockId") REFERENCES public."Paddocks"("Id") ON DELETE RESTRICT;


--
-- Name: AnimalMovements FK_AnimalMovements_Paddocks_ToPaddockId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AnimalMovements"
    ADD CONSTRAINT "FK_AnimalMovements_Paddocks_ToPaddockId" FOREIGN KEY ("ToPaddockId") REFERENCES public."Paddocks"("Id") ON DELETE RESTRICT;


--
-- Name: AnimalPhotos FK_AnimalPhotos_Animals_AnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AnimalPhotos"
    ADD CONSTRAINT "FK_AnimalPhotos_Animals_AnimalId" FOREIGN KEY ("AnimalId") REFERENCES public."Animals"("Id") ON DELETE CASCADE;


--
-- Name: Animals FK_Animals_Animals_DamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Animals"
    ADD CONSTRAINT "FK_Animals_Animals_DamId" FOREIGN KEY ("DamId") REFERENCES public."Animals"("Id") ON DELETE RESTRICT;


--
-- Name: Animals FK_Animals_Animals_SireId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Animals"
    ADD CONSTRAINT "FK_Animals_Animals_SireId" FOREIGN KEY ("SireId") REFERENCES public."Animals"("Id") ON DELETE RESTRICT;


--
-- Name: Animals FK_Animals_Breeds_BreedId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Animals"
    ADD CONSTRAINT "FK_Animals_Breeds_BreedId" FOREIGN KEY ("BreedId") REFERENCES public."Breeds"("Id") ON DELETE RESTRICT;


--
-- Name: Animals FK_Animals_Farms_FarmId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Animals"
    ADD CONSTRAINT "FK_Animals_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES public."Farms"("Id") ON DELETE CASCADE;


--
-- Name: Animals FK_Animals_Lots_LotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Animals"
    ADD CONSTRAINT "FK_Animals_Lots_LotId" FOREIGN KEY ("LotId") REFERENCES public."Lots"("Id") ON DELETE RESTRICT;


--
-- Name: Animals FK_Animals_Paddocks_PaddockId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Animals"
    ADD CONSTRAINT "FK_Animals_Paddocks_PaddockId" FOREIGN KEY ("PaddockId") REFERENCES public."Paddocks"("Id") ON DELETE RESTRICT;


--
-- Name: Animals FK_Animals_Species_SpeciesId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Animals"
    ADD CONSTRAINT "FK_Animals_Species_SpeciesId" FOREIGN KEY ("SpeciesId") REFERENCES public."Species"("Id") ON DELETE RESTRICT;


--
-- Name: Breeds FK_Breeds_Species_SpeciesId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Breeds"
    ADD CONSTRAINT "FK_Breeds_Species_SpeciesId" FOREIGN KEY ("SpeciesId") REFERENCES public."Species"("Id") ON DELETE RESTRICT;


--
-- Name: Diseases FK_Diseases_Species_SpeciesId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Diseases"
    ADD CONSTRAINT "FK_Diseases_Species_SpeciesId" FOREIGN KEY ("SpeciesId") REFERENCES public."Species"("Id") ON DELETE RESTRICT;


--
-- Name: EggProductionRecords FK_EggProductionRecords_Lots_LotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EggProductionRecords"
    ADD CONSTRAINT "FK_EggProductionRecords_Lots_LotId" FOREIGN KEY ("LotId") REFERENCES public."Lots"("Id") ON DELETE CASCADE;


--
-- Name: FeedingRecords FK_FeedingRecords_Lots_LotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FeedingRecords"
    ADD CONSTRAINT "FK_FeedingRecords_Lots_LotId" FOREIGN KEY ("LotId") REFERENCES public."Lots"("Id") ON DELETE CASCADE;


--
-- Name: FeedingRecords FK_FeedingRecords_Rations_RationId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FeedingRecords"
    ADD CONSTRAINT "FK_FeedingRecords_Rations_RationId" FOREIGN KEY ("RationId") REFERENCES public."Rations"("Id") ON DELETE RESTRICT;


--
-- Name: HealthEvents FK_HealthEvents_Animals_AnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthEvents"
    ADD CONSTRAINT "FK_HealthEvents_Animals_AnimalId" FOREIGN KEY ("AnimalId") REFERENCES public."Animals"("Id") ON DELETE RESTRICT;


--
-- Name: HealthEvents FK_HealthEvents_Diseases_DiseaseId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthEvents"
    ADD CONSTRAINT "FK_HealthEvents_Diseases_DiseaseId" FOREIGN KEY ("DiseaseId") REFERENCES public."Diseases"("Id") ON DELETE RESTRICT;


--
-- Name: HealthEvents FK_HealthEvents_Farms_FarmId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthEvents"
    ADD CONSTRAINT "FK_HealthEvents_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES public."Farms"("Id") ON DELETE RESTRICT;


--
-- Name: HealthEvents FK_HealthEvents_ProductBatches_ProductBatchId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthEvents"
    ADD CONSTRAINT "FK_HealthEvents_ProductBatches_ProductBatchId" FOREIGN KEY ("ProductBatchId") REFERENCES public."ProductBatches"("Id") ON DELETE RESTRICT;


--
-- Name: HealthEvents FK_HealthEvents_ProductBatches_Vaccination_ProductBatchId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthEvents"
    ADD CONSTRAINT "FK_HealthEvents_ProductBatches_Vaccination_ProductBatchId" FOREIGN KEY ("Vaccination_ProductBatchId") REFERENCES public."ProductBatches"("Id") ON DELETE RESTRICT;


--
-- Name: HealthEvents FK_HealthEvents_Products_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthEvents"
    ADD CONSTRAINT "FK_HealthEvents_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: HealthEvents FK_HealthEvents_Products_Treatment_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthEvents"
    ADD CONSTRAINT "FK_HealthEvents_Products_Treatment_ProductId" FOREIGN KEY ("Treatment_ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: HealthEvents FK_HealthEvents_Products_Vaccination_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthEvents"
    ADD CONSTRAINT "FK_HealthEvents_Products_Vaccination_ProductId" FOREIGN KEY ("Vaccination_ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: HealthStatusChanges FK_HealthStatusChanges_Animals_AnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."HealthStatusChanges"
    ADD CONSTRAINT "FK_HealthStatusChanges_Animals_AnimalId" FOREIGN KEY ("AnimalId") REFERENCES public."Animals"("Id") ON DELETE CASCADE;


--
-- Name: Lots FK_Lots_Farms_FarmId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Lots"
    ADD CONSTRAINT "FK_Lots_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES public."Farms"("Id") ON DELETE CASCADE;


--
-- Name: Lots FK_Lots_Paddocks_PaddockId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Lots"
    ADD CONSTRAINT "FK_Lots_Paddocks_PaddockId" FOREIGN KEY ("PaddockId") REFERENCES public."Paddocks"("Id") ON DELETE RESTRICT;


--
-- Name: Lots FK_Lots_Species_SpeciesId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Lots"
    ADD CONSTRAINT "FK_Lots_Species_SpeciesId" FOREIGN KEY ("SpeciesId") REFERENCES public."Species"("Id") ON DELETE RESTRICT;


--
-- Name: MilkProductionRecords FK_MilkProductionRecords_Animals_AnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MilkProductionRecords"
    ADD CONSTRAINT "FK_MilkProductionRecords_Animals_AnimalId" FOREIGN KEY ("AnimalId") REFERENCES public."Animals"("Id") ON DELETE CASCADE;


--
-- Name: Paddocks FK_Paddocks_Farms_FarmId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Paddocks"
    ADD CONSTRAINT "FK_Paddocks_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES public."Farms"("Id") ON DELETE CASCADE;


--
-- Name: ProductBatches FK_ProductBatches_Farms_FarmId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProductBatches"
    ADD CONSTRAINT "FK_ProductBatches_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES public."Farms"("Id") ON DELETE CASCADE;


--
-- Name: ProductBatches FK_ProductBatches_Products_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProductBatches"
    ADD CONSTRAINT "FK_ProductBatches_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: ProductBatches FK_ProductBatches_Suppliers_SupplierId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProductBatches"
    ADD CONSTRAINT "FK_ProductBatches_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES public."Suppliers"("Id") ON DELETE RESTRICT;


--
-- Name: RationIngredients FK_RationIngredients_Products_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RationIngredients"
    ADD CONSTRAINT "FK_RationIngredients_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: RationIngredients FK_RationIngredients_Rations_RationId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RationIngredients"
    ADD CONSTRAINT "FK_RationIngredients_Rations_RationId" FOREIGN KEY ("RationId") REFERENCES public."Rations"("Id") ON DELETE CASCADE;


--
-- Name: Rations FK_Rations_Farms_FarmId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Rations"
    ADD CONSTRAINT "FK_Rations_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES public."Farms"("Id") ON DELETE CASCADE;


--
-- Name: Rations FK_Rations_Species_SpeciesId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Rations"
    ADD CONSTRAINT "FK_Rations_Species_SpeciesId" FOREIGN KEY ("SpeciesId") REFERENCES public."Species"("Id") ON DELETE RESTRICT;


--
-- Name: RefreshTokens FK_RefreshTokens_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: ReproductiveEvents FK_ReproductiveEvents_Animals_DamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReproductiveEvents"
    ADD CONSTRAINT "FK_ReproductiveEvents_Animals_DamId" FOREIGN KEY ("DamId") REFERENCES public."Animals"("Id") ON DELETE RESTRICT;


--
-- Name: ReproductiveEvents FK_ReproductiveEvents_Animals_Insemination_SireId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReproductiveEvents"
    ADD CONSTRAINT "FK_ReproductiveEvents_Animals_Insemination_SireId" FOREIGN KEY ("Insemination_SireId") REFERENCES public."Animals"("Id") ON DELETE RESTRICT;


--
-- Name: ReproductiveEvents FK_ReproductiveEvents_Animals_OffspringId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReproductiveEvents"
    ADD CONSTRAINT "FK_ReproductiveEvents_Animals_OffspringId" FOREIGN KEY ("OffspringId") REFERENCES public."Animals"("Id") ON DELETE RESTRICT;


--
-- Name: ReproductiveEvents FK_ReproductiveEvents_Animals_SireId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReproductiveEvents"
    ADD CONSTRAINT "FK_ReproductiveEvents_Animals_SireId" FOREIGN KEY ("SireId") REFERENCES public."Animals"("Id") ON DELETE RESTRICT;


--
-- Name: ReproductiveEvents FK_ReproductiveEvents_Farms_FarmId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReproductiveEvents"
    ADD CONSTRAINT "FK_ReproductiveEvents_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES public."Farms"("Id") ON DELETE RESTRICT;


--
-- Name: ReproductiveEvents FK_ReproductiveEvents_SemenBatches_SemenBatchId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReproductiveEvents"
    ADD CONSTRAINT "FK_ReproductiveEvents_SemenBatches_SemenBatchId" FOREIGN KEY ("SemenBatchId") REFERENCES public."SemenBatches"("Id") ON DELETE RESTRICT;


--
-- Name: RoleClaims FK_RoleClaims_Roles_RoleId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RoleClaims"
    ADD CONSTRAINT "FK_RoleClaims_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."Roles"("Id") ON DELETE CASCADE;


--
-- Name: RolePermissions FK_RolePermissions_Permissions_PermissionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RolePermissions"
    ADD CONSTRAINT "FK_RolePermissions_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES public."Permissions"("Id") ON DELETE CASCADE;


--
-- Name: RolePermissions FK_RolePermissions_Roles_RoleId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RolePermissions"
    ADD CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."Roles"("Id") ON DELETE CASCADE;


--
-- Name: SemenBatches FK_SemenBatches_Breeds_BreedId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SemenBatches"
    ADD CONSTRAINT "FK_SemenBatches_Breeds_BreedId" FOREIGN KEY ("BreedId") REFERENCES public."Breeds"("Id") ON DELETE RESTRICT;


--
-- Name: SemenBatches FK_SemenBatches_Farms_FarmId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SemenBatches"
    ADD CONSTRAINT "FK_SemenBatches_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES public."Farms"("Id") ON DELETE CASCADE;


--
-- Name: SemenBatches FK_SemenBatches_Suppliers_SupplierId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SemenBatches"
    ADD CONSTRAINT "FK_SemenBatches_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES public."Suppliers"("Id") ON DELETE RESTRICT;


--
-- Name: SlaughterRecords FK_SlaughterRecords_Animals_AnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SlaughterRecords"
    ADD CONSTRAINT "FK_SlaughterRecords_Animals_AnimalId" FOREIGN KEY ("AnimalId") REFERENCES public."Animals"("Id") ON DELETE CASCADE;


--
-- Name: StockMovements FK_StockMovements_ProductBatches_ProductBatchId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "FK_StockMovements_ProductBatches_ProductBatchId" FOREIGN KEY ("ProductBatchId") REFERENCES public."ProductBatches"("Id") ON DELETE RESTRICT;


--
-- Name: StockMovements FK_StockMovements_Products_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."StockMovements"
    ADD CONSTRAINT "FK_StockMovements_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: Tasks FK_Tasks_Animals_RelatedAnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Tasks"
    ADD CONSTRAINT "FK_Tasks_Animals_RelatedAnimalId" FOREIGN KEY ("RelatedAnimalId") REFERENCES public."Animals"("Id") ON DELETE RESTRICT;


--
-- Name: Tasks FK_Tasks_Lots_RelatedLotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Tasks"
    ADD CONSTRAINT "FK_Tasks_Lots_RelatedLotId" FOREIGN KEY ("RelatedLotId") REFERENCES public."Lots"("Id") ON DELETE RESTRICT;


--
-- Name: Transactions FK_Transactions_Animals_AnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "FK_Transactions_Animals_AnimalId" FOREIGN KEY ("AnimalId") REFERENCES public."Animals"("Id") ON DELETE RESTRICT;


--
-- Name: Transactions FK_Transactions_Lots_LotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "FK_Transactions_Lots_LotId" FOREIGN KEY ("LotId") REFERENCES public."Lots"("Id") ON DELETE RESTRICT;


--
-- Name: Transactions FK_Transactions_Products_ProductId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Transactions"
    ADD CONSTRAINT "FK_Transactions_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES public."Products"("Id") ON DELETE RESTRICT;


--
-- Name: UserClaims FK_UserClaims_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserClaims"
    ADD CONSTRAINT "FK_UserClaims_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: UserFarms FK_UserFarms_Farms_FarmId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserFarms"
    ADD CONSTRAINT "FK_UserFarms_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES public."Farms"("Id") ON DELETE CASCADE;


--
-- Name: UserFarms FK_UserFarms_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserFarms"
    ADD CONSTRAINT "FK_UserFarms_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: UserLogins FK_UserLogins_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserLogins"
    ADD CONSTRAINT "FK_UserLogins_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: UserPermissions FK_UserPermissions_Permissions_PermissionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserPermissions"
    ADD CONSTRAINT "FK_UserPermissions_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES public."Permissions"("Id") ON DELETE CASCADE;


--
-- Name: UserPermissions FK_UserPermissions_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserPermissions"
    ADD CONSTRAINT "FK_UserPermissions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: UserRoles FK_UserRoles_Roles_RoleId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserRoles"
    ADD CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."Roles"("Id") ON DELETE CASCADE;


--
-- Name: UserRoles FK_UserRoles_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserRoles"
    ADD CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: UserTokens FK_UserTokens_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserTokens"
    ADD CONSTRAINT "FK_UserTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: WeightRecords FK_WeightRecords_Animals_AnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."WeightRecords"
    ADD CONSTRAINT "FK_WeightRecords_Animals_AnimalId" FOREIGN KEY ("AnimalId") REFERENCES public."Animals"("Id") ON DELETE CASCADE;


--
-- Name: WoolProductionRecords FK_WoolProductionRecords_Animals_AnimalId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."WoolProductionRecords"
    ADD CONSTRAINT "FK_WoolProductionRecords_Animals_AnimalId" FOREIGN KEY ("AnimalId") REFERENCES public."Animals"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--


