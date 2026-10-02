--
-- PostgreSQL database dump
--

\restrict Gw0efaEFbc7ggCRragwSFY5mUq5NpKnXVFCO86y0zKpBNLKVhGHS1PVBaDua2aq

-- Dumped from database version 15.19
-- Dumped by pg_dump version 15.19

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: Species; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('0efa7c8c-e9d7-48f7-830c-96b1089c8127', 'Avícola', 'AV', 3, 21, true, '2026-10-02 03:08:13.206741+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('0f464b05-30b9-4241-9014-1a5163031587', 'Porcino', 'PO', 0, 114, true, '2026-10-02 03:08:13.206496+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('24db8a9e-c602-43e2-921e-a25e12d0262e', 'Acuícola', 'AC', 0, NULL, true, '2026-10-02 03:08:13.206841+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('37bbee8e-96d8-4dc3-88f5-ec18a5f8717d', 'Ovino', 'OV', 2, 147, true, '2026-10-02 03:08:13.206115+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('6c6d5b5e-0620-4030-97fc-c446725db0d1', 'Cunícola', 'CU', 0, 31, true, '2026-10-02 03:08:13.206781+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('7942b995-cc81-4f90-955a-f76c8e870a41', 'Bovino', 'BO', 4, 283, true, '2026-10-02 03:08:13.170002+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('c8575530-2e15-4ebc-a785-4b5a705138d3', 'Caprino', 'CA', 4, 150, true, '2026-10-02 03:08:13.206631+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('c8bbd40f-cf98-477c-96b6-a826b7ea05b7', 'Equino', 'EQ', 5, 340, true, '2026-10-02 03:08:13.206701+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('dfbfd3b5-8beb-457d-a9e8-b8c4b093ad61', 'Apícola', 'AP', 5, NULL, true, '2026-10-02 03:08:13.206812+00');


--
-- Data for Name: Breeds; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('1e5b878b-46c1-432b-a9d2-981b304b03ca', 'c8575530-2e15-4ebc-a785-4b5a705138d3', 'Boer', 4, NULL, true, '2026-10-02 03:08:13.206632+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('27c13f6e-6f6f-4685-ab55-fb7a4181bc4d', '0efa7c8c-e9d7-48f7-830c-96b1089c8127', 'Rhode Island', 3, NULL, true, '2026-10-02 03:08:13.206742+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('309312ff-be88-4bac-80ca-d0d074771f29', '7942b995-cc81-4f90-955a-f76c8e870a41', 'Brahman', 4, NULL, true, '2026-10-02 03:08:13.170066+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('3b7dc938-c9a0-4649-bdb6-c7c1c176cf48', 'c8bbd40f-cf98-477c-96b6-a826b7ea05b7', 'Cuarto de Milla', 5, NULL, true, '2026-10-02 03:08:13.206702+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('6c82c6bf-f176-4281-b281-41e68be6431e', '37bbee8e-96d8-4dc3-88f5-ec18a5f8717d', 'Merino', 2, NULL, true, '2026-10-02 03:08:13.206118+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('7895bdfe-426a-446a-a955-8d8ff51a3f3e', '37bbee8e-96d8-4dc3-88f5-ec18a5f8717d', 'Dorper', 2, NULL, true, '2026-10-02 03:08:13.206117+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('9dec48de-85f4-47e9-b083-59f05600591b', '0f464b05-30b9-4241-9014-1a5163031587', 'Duroc', 0, NULL, true, '2026-10-02 03:08:13.206497+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('b0d03f32-a97d-435b-8f13-0f9958b6567a', '6c6d5b5e-0620-4030-97fc-c446725db0d1', 'Nueva Zelanda', 0, NULL, true, '2026-10-02 03:08:13.206787+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('cb93a825-c21f-43df-8ec4-bd1eed6205c4', '7942b995-cc81-4f90-955a-f76c8e870a41', 'Holstein', 4, NULL, true, '2026-10-02 03:08:13.170134+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('cd4151cd-d09b-4a93-8798-04c5a668b59c', '0f464b05-30b9-4241-9014-1a5163031587', 'Yorkshire', 0, NULL, true, '2026-10-02 03:08:13.206498+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('d141dfb1-8d1e-4c1e-b4b3-047e34741ad1', '0efa7c8c-e9d7-48f7-830c-96b1089c8127', 'Leghorn', 3, NULL, true, '2026-10-02 03:08:13.206742+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('d4fc058a-4b4b-4242-8a8e-59ef3d0836ee', '7942b995-cc81-4f90-955a-f76c8e870a41', 'Angus', 4, NULL, true, '2026-10-02 03:08:13.170135+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('db87400c-8162-466c-bbd8-7bf8fd7dcc99', 'c8575530-2e15-4ebc-a785-4b5a705138d3', 'Saanen', 4, NULL, true, '2026-10-02 03:08:13.206633+00');


--
-- Data for Name: Farms; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Farms" ("Id", "Name", "Code", "Address", "Phone", "Email", "IsActive", "CreatedAt") VALUES ('ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'Finca El Paraíso', 'DEMO', 'Ruta 1 km 5', '+00 000 000', 'demo@daw.local', true, '2026-10-02 03:08:13.257617+00');


--
-- Data for Name: Paddocks; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Paddocks" ("Id", "FarmId", "Name", "Code", "AreaHectares", "Capacity", "IsActive", "CreatedAt") VALUES ('723ccfc8-958e-4e75-b63a-829fe302afa7', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'Potrero Norte', NULL, 12.5000, 40, true, '2026-10-02 03:08:13.257672+00');
INSERT INTO public."Paddocks" ("Id", "FarmId", "Name", "Code", "AreaHectares", "Capacity", "IsActive", "CreatedAt") VALUES ('d9b08de0-6e6d-4b6c-9262-1535a1dfcf9a', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'Potrero Sur', NULL, 9.0000, 30, true, '2026-10-02 03:08:13.257763+00');


--
-- Data for Name: Lots; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Lots" ("Id", "FarmId", "SpeciesId", "PaddockId", "Name", "Purpose", "IsActive", "CreatedAt") VALUES ('35d6ae94-62cf-4ab4-9a7d-92d7ed8933df', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '37bbee8e-96d8-4dc3-88f5-ec18a5f8717d', '723ccfc8-958e-4e75-b63a-829fe302afa7', 'Ovinos', 2, true, '2026-10-02 03:08:13.257896+00');
INSERT INTO public."Lots" ("Id", "FarmId", "SpeciesId", "PaddockId", "Name", "Purpose", "IsActive", "CreatedAt") VALUES ('78e91fc7-d16d-4094-b928-714f9c1d130a', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '7942b995-cc81-4f90-955a-f76c8e870a41', '723ccfc8-958e-4e75-b63a-829fe302afa7', 'Engorde', 0, true, '2026-10-02 03:08:13.257798+00');
INSERT INTO public."Lots" ("Id", "FarmId", "SpeciesId", "PaddockId", "Name", "Purpose", "IsActive", "CreatedAt") VALUES ('9546aa88-9f3d-43ea-a9a2-0ff39a1654b6', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '7942b995-cc81-4f90-955a-f76c8e870a41', 'd9b08de0-6e6d-4b6c-9262-1535a1dfcf9a', 'Cría', 4, true, '2026-10-02 03:08:13.257895+00');


--
-- Data for Name: Animals; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('047fa78e-7a8f-43f7-90cd-cde35771cfaa', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '7942b995-cc81-4f90-955a-f76c8e870a41', 'cb93a825-c21f-43df-8ec4-bd1eed6205c4', '78e91fc7-d16d-4094-b928-714f9c1d130a', '723ccfc8-958e-4e75-b63a-829fe302afa7', 'DEMO-002', 'OF-DEMO-002', NULL, 'Vaca Luna', 1, '2022-10-02', 40.0000, 'Blanco', NULL, 0, 0, 1, 0, NULL, NULL, NULL, '2026-10-02 03:08:13.395775+00', '2026-10-02 03:08:13.395769+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('3dfd9750-29f2-483a-bf55-fcca5e16f9a9', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '7942b995-cc81-4f90-955a-f76c8e870a41', 'cb93a825-c21f-43df-8ec4-bd1eed6205c4', '9546aa88-9f3d-43ea-a9a2-0ff39a1654b6', 'd9b08de0-6e6d-4b6c-9262-1535a1dfcf9a', 'DEMO-005', 'OF-DEMO-005', NULL, 'Ternero Sol', 0, '2026-02-02', 12.9000, 'Negro', NULL, 0, 0, 0, 0, NULL, NULL, NULL, '2026-10-02 03:08:13.396339+00', '2026-10-02 03:08:13.396337+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('56dbed60-2500-439c-8dbb-07ad8dd07d19', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '7942b995-cc81-4f90-955a-f76c8e870a41', '309312ff-be88-4bac-80ca-d0d074771f29', '9546aa88-9f3d-43ea-a9a2-0ff39a1654b6', 'd9b08de0-6e6d-4b6c-9262-1535a1dfcf9a', 'DEMO-004', 'OF-DEMO-004', NULL, 'Vaquillona Estrella', 1, '2025-02-02', 24.3000, 'Blanco', NULL, 0, 0, 4, 0, NULL, NULL, NULL, '2026-10-02 03:08:13.396168+00', '2026-10-02 03:08:13.396165+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('b2d210e8-f8a6-4e61-b156-892c3da42d88', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '37bbee8e-96d8-4dc3-88f5-ec18a5f8717d', '7895bdfe-426a-446a-a955-8d8ff51a3f3e', '35d6ae94-62cf-4ab4-9a7d-92d7ed8933df', '723ccfc8-958e-4e75-b63a-829fe302afa7', 'DEMO-102', 'OF-DEMO-102', NULL, 'Carnero Negro', 0, '2024-04-02', 5.7000, 'Negro', NULL, 0, 0, 2, 0, NULL, NULL, NULL, '2026-10-02 03:08:13.396623+00', '2026-10-02 03:08:13.396622+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('b34ce798-1b6a-455f-896d-9f64cc41b874', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '7942b995-cc81-4f90-955a-f76c8e870a41', 'd4fc058a-4b4b-4242-8a8e-59ef3d0836ee', '78e91fc7-d16d-4094-b928-714f9c1d130a', '723ccfc8-958e-4e75-b63a-829fe302afa7', 'DEMO-003', 'OF-DEMO-003', NULL, 'Novillo Coco', 0, '2025-04-02', 27.1000, 'Negro', NULL, 0, 0, 0, 0, NULL, NULL, NULL, '2026-10-02 03:08:13.396006+00', '2026-10-02 03:08:13.396004+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('b8127e2e-a608-4670-8014-163bbd2cfe05', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '7942b995-cc81-4f90-955a-f76c8e870a41', '309312ff-be88-4bac-80ca-d0d074771f29', '78e91fc7-d16d-4094-b928-714f9c1d130a', '723ccfc8-958e-4e75-b63a-829fe302afa7', 'DEMO-001', 'OF-DEMO-001', NULL, 'Toro Bravo', 0, '2024-04-02', 37.1000, 'Negro', NULL, 0, 0, 0, 0, NULL, NULL, NULL, '2026-10-02 03:08:13.259006+00', '2026-10-02 03:08:13.258986+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('ffd70e52-e9bb-4816-90d9-4f26b6542024', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '7942b995-cc81-4f90-955a-f76c8e870a41', 'd4fc058a-4b4b-4242-8a8e-59ef3d0836ee', '9546aa88-9f3d-43ea-a9a2-0ff39a1654b6', 'd9b08de0-6e6d-4b6c-9262-1535a1dfcf9a', 'DEMO-006', 'OF-DEMO-006', NULL, 'Vaca Nube', 1, '2023-10-02', 33.6000, 'Blanco', NULL, 0, 0, 1, 0, NULL, NULL, NULL, '2026-10-02 03:08:13.396421+00', '2026-10-02 03:08:13.396419+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('e333bda9-2a0a-4fcf-9988-adca1c533f4d', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '37bbee8e-96d8-4dc3-88f5-ec18a5f8717d', '6c82c6bf-f176-4281-b281-41e68be6431e', '35d6ae94-62cf-4ab4-9a7d-92d7ed8933df', '723ccfc8-958e-4e75-b63a-829fe302afa7', 'DEMO-101', 'OF-DEMO-101', NULL, 'Oveja Blanca', 1, '2024-10-02', 4.6000, 'Blanco', NULL, 2, 0, 2, 0, NULL, NULL, NULL, '2026-10-02 03:08:13.396497+00', '2026-10-02 03:08:13.635182+00');


--
-- Data for Name: AnimalProduction; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."AnimalProduction" ("Id", "FarmId", "AnimalId", "OperationId", "Date", "ProductType", "Method", "Quantity", "Unit", "Notes", "CreatedAt") VALUES ('952d800e-fabd-46ce-ab92-caabd260d558', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'e333bda9-2a0a-4fcf-9988-adca1c533f4d', 'c3b917cc-5569-4fe3-89dc-41dcf3887303', '2026-10-02', 4, 2, 1.0000, 2, NULL, '2026-10-02 03:08:13.624795+00');
INSERT INTO public."AnimalProduction" ("Id", "FarmId", "AnimalId", "OperationId", "Date", "ProductType", "Method", "Quantity", "Unit", "Notes", "CreatedAt") VALUES ('c6d7f011-a3fc-4ec7-a663-e10680385798', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'e333bda9-2a0a-4fcf-9988-adca1c533f4d', 'c3b917cc-5569-4fe3-89dc-41dcf3887302', '2026-09-02', 1, 1, 3.2000, 0, 'Esquila previa al sacrificio de demostración', '2026-10-02 03:08:13.623236+00');
INSERT INTO public."AnimalProduction" ("Id", "FarmId", "AnimalId", "OperationId", "Date", "ProductType", "Method", "Quantity", "Unit", "Notes", "CreatedAt") VALUES ('cf2a5872-c109-4e25-b9d8-c642c85a9376', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'e333bda9-2a0a-4fcf-9988-adca1c533f4d', 'c3b917cc-5569-4fe3-89dc-41dcf3887303', '2026-10-02', 2, 2, 28.0000, 0, NULL, '2026-10-02 03:08:13.624714+00');
INSERT INTO public."AnimalProduction" ("Id", "FarmId", "AnimalId", "OperationId", "Date", "ProductType", "Method", "Quantity", "Unit", "Notes", "CreatedAt") VALUES ('e9a554e9-1d8f-4c3d-8b79-cd425679669f', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '047fa78e-7a8f-43f7-90cd-cde35771cfaa', 'c3b917cc-5569-4fe3-89dc-41dcf3887301', '2026-10-02', 0, 0, 18.5000, 1, 'Ordeño de demostración', '2026-10-02 03:08:13.60606+00');


--
-- Data for Name: InventoryCategories; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."InventoryCategories" ("Id", "Name", "Description", "IsActive", "CreatedAt") VALUES ('a1100000-0000-4000-8000-000000000001', 'Alimentación animal', 'Insumos de la operación ganadera', true, '2026-01-01 00:00:00+00');
INSERT INTO public."InventoryCategories" ("Id", "Name", "Description", "IsActive", "CreatedAt") VALUES ('a1100000-0000-4000-8000-000000000002', 'Sanidad animal', 'Insumos de la operación ganadera', true, '2026-01-01 00:00:00+00');


--
-- Data for Name: Products; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Products" ("Id", "Name", "Unit", "WithdrawalDays", "RequiresPrescription", "IsActive", "CreatedAt", "Brand", "CategoryId", "CostPrice", "Price", "SKU") VALUES ('1be7946a-ef45-46f3-9707-2ae8feaadfe5', 'Desparasitante', 1, NULL, false, true, '2026-10-02 03:08:13.563886+00', 'VetSalud', 'a1100000-0000-4000-8000-000000000002', 15.00, 22.00, 'SAN-002');
INSERT INTO public."Products" ("Id", "Name", "Unit", "WithdrawalDays", "RequiresPrescription", "IsActive", "CreatedAt", "Brand", "CategoryId", "CostPrice", "Price", "SKU") VALUES ('4fd2ada9-7412-4315-9078-3313ccba11fc', 'Sal mineral', 4, NULL, false, true, '2026-10-02 03:08:13.560603+00', 'AgroCampo', 'a1100000-0000-4000-8000-000000000001', 12.00, 16.00, 'ALI-002');
INSERT INTO public."Products" ("Id", "Name", "Unit", "WithdrawalDays", "RequiresPrescription", "IsActive", "CreatedAt", "Brand", "CategoryId", "CostPrice", "Price", "SKU") VALUES ('73712a69-05fc-4c80-a98c-d5c364a1c88b', 'Vacuna bovina', 3, NULL, false, true, '2026-10-02 03:08:13.562609+00', 'VetSalud', 'a1100000-0000-4000-8000-000000000002', 6.50, 8.75, 'SAN-001');
INSERT INTO public."Products" ("Id", "Name", "Unit", "WithdrawalDays", "RequiresPrescription", "IsActive", "CreatedAt", "Brand", "CategoryId", "CostPrice", "Price", "SKU") VALUES ('88f97569-3578-43cc-aa16-f9daef366eea', 'Concentrado bovino', 4, NULL, false, true, '2026-10-02 03:08:13.52149+00', 'AgroCampo', 'a1100000-0000-4000-8000-000000000001', 18.00, 24.50, 'ALI-001');


--
-- Data for Name: FarmInventory; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."FarmInventory" ("Id", "FarmId", "ProductId", "Stock", "MinStock", "MaxStock", "Location", "CreatedAt") VALUES ('4f8381f7-bfc8-4966-8830-e0ce1675eccb', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '73712a69-05fc-4c80-a98c-d5c364a1c88b', 60.0000, 15.0000, 150.0000, 'Almacén principal', '2026-10-02 03:08:13.563348+00');
INSERT INTO public."FarmInventory" ("Id", "FarmId", "ProductId", "Stock", "MinStock", "MaxStock", "Location", "CreatedAt") VALUES ('519a0b3a-f576-4d85-b860-0d2f83e4fe2d', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '88f97569-3578-43cc-aa16-f9daef366eea', 40.0000, 10.0000, 100.0000, 'Almacén principal', '2026-10-02 03:08:13.547931+00');
INSERT INTO public."FarmInventory" ("Id", "FarmId", "ProductId", "Stock", "MinStock", "MaxStock", "Location", "CreatedAt") VALUES ('d8022bf7-aaa0-40dd-8efe-45ddadd33013', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '4fd2ada9-7412-4315-9078-3313ccba11fc', 25.0000, 5.0000, 80.0000, 'Almacén principal', '2026-10-02 03:08:13.561639+00');
INSERT INTO public."FarmInventory" ("Id", "FarmId", "ProductId", "Stock", "MinStock", "MaxStock", "Location", "CreatedAt") VALUES ('f37264ff-01d6-4c5c-bf91-f2def3e4ab4d', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '1be7946a-ef45-46f3-9707-2ae8feaadfe5', 12.0000, 3.0000, 30.0000, 'Almacén principal', '2026-10-02 03:08:13.564622+00');


--
-- Data for Name: WeightRecords; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('02c9f2b2-73d8-4bf9-b582-f5945583a4e1', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'b2d210e8-f8a6-4e61-b156-892c3da42d88', '2026-10-02', 80.00, 3.50, NULL, NULL, '2026-10-02 03:08:13.396625+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('1a4e9f00-d1ff-41da-b984-69073a9b3211', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'ffd70e52-e9bb-4816-90d9-4f26b6542024', '2026-10-02', 470.00, 3.50, NULL, NULL, '2026-10-02 03:08:13.396422+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('39a022c0-cb03-4d28-9358-d7a59534f27c', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '56dbed60-2500-439c-8dbb-07ad8dd07d19', '2026-10-02', 340.00, 3.50, NULL, NULL, '2026-10-02 03:08:13.39617+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('4b3f5729-2e06-4a45-b0fa-0e07ab5c8997', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'b8127e2e-a608-4670-8014-163bbd2cfe05', '2026-10-02', 520.00, 3.50, NULL, NULL, '2026-10-02 03:08:13.259288+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('9344bcbf-19e3-4c7d-941d-13ff44cb56a8', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '3dfd9750-29f2-483a-bf55-fcca5e16f9a9', '2026-10-02', 180.00, 3.50, NULL, NULL, '2026-10-02 03:08:13.39634+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('a11d90ad-352a-441b-9dad-0bdb118e52e6', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', '047fa78e-7a8f-43f7-90cd-cde35771cfaa', '2026-10-02', 560.00, 3.50, NULL, NULL, '2026-10-02 03:08:13.395779+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('c550f08c-90ac-4b7a-bf08-72a5528eef4d', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'b34ce798-1b6a-455f-896d-9f64cc41b874', '2026-10-02', 380.00, 3.50, NULL, NULL, '2026-10-02 03:08:13.396009+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('c5aadee1-207c-49a7-8963-fd2e046f2be4', 'ffbb8c49-4267-4c56-b21a-3642a7dbb0f6', 'e333bda9-2a0a-4fcf-9988-adca1c533f4d', '2026-10-02', 65.00, 3.50, NULL, NULL, '2026-10-02 03:08:13.396499+00');


--
-- PostgreSQL database dump complete
--

\unrestrict Gw0efaEFbc7ggCRragwSFY5mUq5NpKnXVFCO86y0zKpBNLKVhGHS1PVBaDua2aq
