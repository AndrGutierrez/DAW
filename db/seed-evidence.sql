--
-- PostgreSQL database dump
--

\restrict 6xEejy6HlOHLxbbqBrbny03BGy7NJBQ38XeunC0dAv6YPjG6L0gHgX4XyymwMm6

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

INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('07f7bc23-b9b5-4316-a98f-146738431b81', 'Avícola', 'AV', 3, 21, true, '2026-10-02 02:35:17.805077+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('2ae62a0c-5860-4c69-bf55-21c9b81146cd', 'Apícola', 'AP', 5, NULL, true, '2026-10-02 02:35:17.805143+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('2c7c09bc-d36c-4b6f-bc23-8304a26af88b', 'Caprino', 'CA', 4, 150, true, '2026-10-02 02:35:17.805008+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('478422d5-9b83-4a35-b9ea-9bb1c61d8cb3', 'Equino', 'EQ', 5, 340, true, '2026-10-02 02:35:17.805051+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('4ed6e0ed-8518-4eec-a970-4c514b72dc0c', 'Cunícola', 'CU', 0, 31, true, '2026-10-02 02:35:17.805117+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('8e798171-3b9f-4d6c-adf7-d218d5c0cb4b', 'Porcino', 'PO', 0, 114, true, '2026-10-02 02:35:17.80494+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', 'Bovino', 'BO', 4, 283, true, '2026-10-02 02:35:17.768149+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('a24f8abd-59f1-45fd-8975-8fcc9724f34a', 'Acuícola', 'AC', 0, NULL, true, '2026-10-02 02:35:17.805176+00');
INSERT INTO public."Species" ("Id", "Name", "Code", "Purpose", "GestationDays", "IsActive", "CreatedAt") VALUES ('cd5cd023-78e1-4157-ab2d-102f96fa1e03', 'Ovino', 'OV', 2, 147, true, '2026-10-02 02:35:17.804273+00');


--
-- Data for Name: Breeds; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('026b74c0-a73b-4d27-a06d-02450576c0ce', '2c7c09bc-d36c-4b6f-bc23-8304a26af88b', 'Boer', 4, NULL, true, '2026-10-02 02:35:17.805008+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('32e323a3-9303-4ad2-add8-ec700d05b658', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', 'Holstein', 4, NULL, true, '2026-10-02 02:35:17.768286+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('48118568-6cfe-49d3-a18f-b5e61d0c2979', '8e798171-3b9f-4d6c-adf7-d218d5c0cb4b', 'Duroc', 0, NULL, true, '2026-10-02 02:35:17.804941+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('57f76f60-2456-4bbf-995c-e071af402241', '2c7c09bc-d36c-4b6f-bc23-8304a26af88b', 'Saanen', 4, NULL, true, '2026-10-02 02:35:17.805009+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('7fb95936-39ef-4b23-8005-b77cce4390de', '478422d5-9b83-4a35-b9ea-9bb1c61d8cb3', 'Cuarto de Milla', 5, NULL, true, '2026-10-02 02:35:17.805052+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('9a665cb7-0f4b-4134-8b63-f1a611d6f85c', '07f7bc23-b9b5-4316-a98f-146738431b81', 'Leghorn', 3, NULL, true, '2026-10-02 02:35:17.805078+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('ae13c4f9-2928-4077-b6de-39399dbebf55', '07f7bc23-b9b5-4316-a98f-146738431b81', 'Rhode Island', 3, NULL, true, '2026-10-02 02:35:17.805079+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('b9327607-2d2a-42c8-bd06-b6d73f0bcfed', '8e798171-3b9f-4d6c-adf7-d218d5c0cb4b', 'Yorkshire', 0, NULL, true, '2026-10-02 02:35:17.804942+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('c6a80c7d-adb4-4aef-8952-897bc10b9b6c', 'cd5cd023-78e1-4157-ab2d-102f96fa1e03', 'Dorper', 2, NULL, true, '2026-10-02 02:35:17.804274+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('c76cd004-2d0c-43f8-afba-37cccb8d6836', 'cd5cd023-78e1-4157-ab2d-102f96fa1e03', 'Merino', 2, NULL, true, '2026-10-02 02:35:17.804276+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('df8ec59b-b335-47e5-968c-4e26361e76d2', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', 'Angus', 4, NULL, true, '2026-10-02 02:35:17.768287+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('f5547115-e45a-4a2a-ad0e-09fc4e666aa8', '4ed6e0ed-8518-4eec-a970-4c514b72dc0c', 'Nueva Zelanda', 0, NULL, true, '2026-10-02 02:35:17.805117+00');
INSERT INTO public."Breeds" ("Id", "SpeciesId", "Name", "Purpose", "Origin", "IsActive", "CreatedAt") VALUES ('f8fab200-0b1f-46f8-a68c-c7f42dc47574', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', 'Brahman', 4, NULL, true, '2026-10-02 02:35:17.768213+00');


--
-- Data for Name: Farms; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Farms" ("Id", "Name", "Code", "Address", "Phone", "Email", "IsActive", "CreatedAt") VALUES ('ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'Finca El Paraíso', 'DEMO', 'Ruta 1 km 5', '+00 000 000', 'demo@daw.local', true, '2026-10-02 02:35:17.852482+00');


--
-- Data for Name: Paddocks; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Paddocks" ("Id", "FarmId", "Name", "Code", "AreaHectares", "Capacity", "IsActive", "CreatedAt") VALUES ('8f833990-a749-4766-ae0b-251462ea4d53', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'Potrero Sur', NULL, 9.0000, 30, true, '2026-10-02 02:35:17.852587+00');
INSERT INTO public."Paddocks" ("Id", "FarmId", "Name", "Code", "AreaHectares", "Capacity", "IsActive", "CreatedAt") VALUES ('c328190a-6a12-4d46-8343-99c3d6c86161', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'Potrero Norte', NULL, 12.5000, 40, true, '2026-10-02 02:35:17.852535+00');


--
-- Data for Name: Lots; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Lots" ("Id", "FarmId", "SpeciesId", "PaddockId", "Name", "Purpose", "IsActive", "CreatedAt") VALUES ('b3a5e931-75fc-4284-943e-de82ab680ee1', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', '8f833990-a749-4766-ae0b-251462ea4d53', 'Cría', 4, true, '2026-10-02 02:35:17.852678+00');
INSERT INTO public."Lots" ("Id", "FarmId", "SpeciesId", "PaddockId", "Name", "Purpose", "IsActive", "CreatedAt") VALUES ('b5a4c107-0d4e-4e22-9636-2f061bcf86d8', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'cd5cd023-78e1-4157-ab2d-102f96fa1e03', 'c328190a-6a12-4d46-8343-99c3d6c86161', 'Ovinos', 2, true, '2026-10-02 02:35:17.852679+00');
INSERT INTO public."Lots" ("Id", "FarmId", "SpeciesId", "PaddockId", "Name", "Purpose", "IsActive", "CreatedAt") VALUES ('e61d2c80-e7a3-4cf2-8979-421c46c4db47', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', 'c328190a-6a12-4d46-8343-99c3d6c86161', 'Engorde', 0, true, '2026-10-02 02:35:17.852619+00');


--
-- Data for Name: Animals; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('2e298246-1c1f-47d9-b444-877afa85938f', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', '32e323a3-9303-4ad2-add8-ec700d05b658', 'e61d2c80-e7a3-4cf2-8979-421c46c4db47', 'c328190a-6a12-4d46-8343-99c3d6c86161', 'DEMO-002', 'OF-DEMO-002', NULL, 'Vaca Luna', 1, '2022-10-02', 40.0000, 'Blanco', NULL, 0, 0, 1, 0, NULL, NULL, NULL, '2026-10-02 02:35:17.990018+00', '2026-10-02 02:35:17.990012+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('5f491634-d1f1-47d7-8edf-3fa9c384941a', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', 'f8fab200-0b1f-46f8-a68c-c7f42dc47574', 'b3a5e931-75fc-4284-943e-de82ab680ee1', '8f833990-a749-4766-ae0b-251462ea4d53', 'DEMO-004', 'OF-DEMO-004', NULL, 'Vaquillona Estrella', 1, '2025-02-02', 24.3000, 'Blanco', NULL, 0, 0, 4, 0, NULL, NULL, NULL, '2026-10-02 02:35:17.990321+00', '2026-10-02 02:35:17.99032+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('7e1e435e-f054-45c0-9553-685be00554cc', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', '32e323a3-9303-4ad2-add8-ec700d05b658', 'b3a5e931-75fc-4284-943e-de82ab680ee1', '8f833990-a749-4766-ae0b-251462ea4d53', 'DEMO-005', 'OF-DEMO-005', NULL, 'Ternero Sol', 0, '2026-02-02', 12.9000, 'Negro', NULL, 0, 0, 0, 0, NULL, NULL, NULL, '2026-10-02 02:35:17.99047+00', '2026-10-02 02:35:17.990468+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('a9653f5a-aa1e-4882-9713-7cc502f41c69', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'cd5cd023-78e1-4157-ab2d-102f96fa1e03', 'c6a80c7d-adb4-4aef-8952-897bc10b9b6c', 'b5a4c107-0d4e-4e22-9636-2f061bcf86d8', 'c328190a-6a12-4d46-8343-99c3d6c86161', 'DEMO-102', 'OF-DEMO-102', NULL, 'Carnero Negro', 0, '2024-04-02', 5.7000, 'Negro', NULL, 0, 0, 2, 0, NULL, NULL, NULL, '2026-10-02 02:35:17.990782+00', '2026-10-02 02:35:17.990781+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('b40d9c91-f20b-4646-8ed0-61947196e97b', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', 'f8fab200-0b1f-46f8-a68c-c7f42dc47574', 'e61d2c80-e7a3-4cf2-8979-421c46c4db47', 'c328190a-6a12-4d46-8343-99c3d6c86161', 'DEMO-001', 'OF-DEMO-001', NULL, 'Toro Bravo', 0, '2024-04-02', 37.1000, 'Negro', NULL, 0, 0, 0, 0, NULL, NULL, NULL, '2026-10-02 02:35:17.853786+00', '2026-10-02 02:35:17.853775+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('d83fd3eb-048c-4195-acde-7b20cff54e50', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', 'df8ec59b-b335-47e5-968c-4e26361e76d2', 'e61d2c80-e7a3-4cf2-8979-421c46c4db47', 'c328190a-6a12-4d46-8343-99c3d6c86161', 'DEMO-003', 'OF-DEMO-003', NULL, 'Novillo Coco', 0, '2025-04-02', 27.1000, 'Negro', NULL, 0, 0, 0, 0, NULL, NULL, NULL, '2026-10-02 02:35:17.990237+00', '2026-10-02 02:35:17.990235+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('dc5448ac-e505-4817-b39e-4e5a15369b32', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '919f1e56-bb1b-473b-ad1b-94f6ed57b8fa', 'df8ec59b-b335-47e5-968c-4e26361e76d2', 'b3a5e931-75fc-4284-943e-de82ab680ee1', '8f833990-a749-4766-ae0b-251462ea4d53', 'DEMO-006', 'OF-DEMO-006', NULL, 'Vaca Nube', 1, '2023-10-02', 33.6000, 'Blanco', NULL, 0, 0, 1, 0, NULL, NULL, NULL, '2026-10-02 02:35:17.990591+00', '2026-10-02 02:35:17.990588+00');
INSERT INTO public."Animals" ("Id", "FarmId", "SpeciesId", "BreedId", "LotId", "PaddockId", "InternalTag", "OfficialId", "Rfid", "Name", "Sex", "BirthDate", "BirthWeightKg", "Color", "Markings", "Status", "Origin", "Purpose", "HealthStatus", "DamId", "SireId", "Notes", "CreatedAt", "UpdatedAt") VALUES ('7aeb953c-4c03-43f3-9de8-fd7c846a5726', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'cd5cd023-78e1-4157-ab2d-102f96fa1e03', 'c76cd004-2d0c-43f8-afba-37cccb8d6836', 'b5a4c107-0d4e-4e22-9636-2f061bcf86d8', 'c328190a-6a12-4d46-8343-99c3d6c86161', 'DEMO-101', 'OF-DEMO-101', NULL, 'Oveja Blanca', 1, '2024-10-02', 4.6000, 'Blanco', NULL, 2, 0, 2, 0, NULL, NULL, NULL, '2026-10-02 02:35:17.990676+00', '2026-10-02 02:35:18.21221+00');


--
-- Data for Name: AnimalProduction; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."AnimalProduction" ("Id", "FarmId", "AnimalId", "OperationId", "Date", "ProductType", "Method", "Quantity", "Unit", "Notes", "CreatedAt") VALUES ('1ad749fe-63fe-4230-9c81-32d528d4d38e', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '7aeb953c-4c03-43f3-9de8-fd7c846a5726', 'c3b917cc-5569-4fe3-89dc-41dcf3887303', '2026-10-02', 2, 2, 28.0000, 0, NULL, '2026-10-02 02:35:18.200426+00');
INSERT INTO public."AnimalProduction" ("Id", "FarmId", "AnimalId", "OperationId", "Date", "ProductType", "Method", "Quantity", "Unit", "Notes", "CreatedAt") VALUES ('2b41414c-a703-455a-a4e2-03e4ea1a8ec1', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '7aeb953c-4c03-43f3-9de8-fd7c846a5726', 'c3b917cc-5569-4fe3-89dc-41dcf3887302', '2026-09-02', 1, 1, 3.2000, 0, 'Esquila previa al sacrificio de demostración', '2026-10-02 02:35:18.199719+00');
INSERT INTO public."AnimalProduction" ("Id", "FarmId", "AnimalId", "OperationId", "Date", "ProductType", "Method", "Quantity", "Unit", "Notes", "CreatedAt") VALUES ('923a54fc-5ee3-4b21-8b8e-c445c4941dd5', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '7aeb953c-4c03-43f3-9de8-fd7c846a5726', 'c3b917cc-5569-4fe3-89dc-41dcf3887303', '2026-10-02', 4, 2, 1.0000, 2, NULL, '2026-10-02 02:35:18.200477+00');
INSERT INTO public."AnimalProduction" ("Id", "FarmId", "AnimalId", "OperationId", "Date", "ProductType", "Method", "Quantity", "Unit", "Notes", "CreatedAt") VALUES ('ed9aa21c-7e17-4139-b209-1049e3478290', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '2e298246-1c1f-47d9-b444-877afa85938f', 'c3b917cc-5569-4fe3-89dc-41dcf3887301', '2026-10-02', 0, 0, 18.5000, 1, 'Ordeño de demostración', '2026-10-02 02:35:18.182195+00');


--
-- Data for Name: InventoryCategories; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."InventoryCategories" ("Id", "Name", "Description", "IsActive", "CreatedAt") VALUES ('9c962fd9-c847-49d7-8ceb-93fbfb255c73', 'Alimentación animal', 'Insumos de la operación ganadera', true, '2026-10-02 02:35:18.06208+00');
INSERT INTO public."InventoryCategories" ("Id", "Name", "Description", "IsActive", "CreatedAt") VALUES ('e9d5bb30-870c-4548-b254-f1224372320b', 'Sanidad animal', 'Insumos de la operación ganadera', true, '2026-10-02 02:35:18.071855+00');


--
-- Data for Name: Products; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."Products" ("Id", "Name", "Unit", "WithdrawalDays", "RequiresPrescription", "IsActive", "CreatedAt", "Brand", "CategoryId", "CostPrice", "Price", "SKU") VALUES ('0a9a4cba-25ff-45ae-921a-b6e3baa0b2ab', 'Vacuna bovina', 3, NULL, false, true, '2026-10-02 02:35:18.138812+00', 'VetSalud', 'e9d5bb30-870c-4548-b254-f1224372320b', 6.50, 8.75, 'SAN-001');
INSERT INTO public."Products" ("Id", "Name", "Unit", "WithdrawalDays", "RequiresPrescription", "IsActive", "CreatedAt", "Brand", "CategoryId", "CostPrice", "Price", "SKU") VALUES ('14a69eae-50d5-4672-81e5-23ef97393324', 'Concentrado bovino', 4, NULL, false, true, '2026-10-02 02:35:18.096513+00', 'AgroCampo', '9c962fd9-c847-49d7-8ceb-93fbfb255c73', 18.00, 24.50, 'ALI-001');
INSERT INTO public."Products" ("Id", "Name", "Unit", "WithdrawalDays", "RequiresPrescription", "IsActive", "CreatedAt", "Brand", "CategoryId", "CostPrice", "Price", "SKU") VALUES ('7f4c4e59-f15b-4424-93ce-21d78dde4712', 'Sal mineral', 4, NULL, false, true, '2026-10-02 02:35:18.136916+00', 'AgroCampo', '9c962fd9-c847-49d7-8ceb-93fbfb255c73', 12.00, 16.00, 'ALI-002');
INSERT INTO public."Products" ("Id", "Name", "Unit", "WithdrawalDays", "RequiresPrescription", "IsActive", "CreatedAt", "Brand", "CategoryId", "CostPrice", "Price", "SKU") VALUES ('d0bd4186-4e82-4f45-a578-6eb5a1512b40', 'Desparasitante', 1, NULL, false, true, '2026-10-02 02:35:18.140182+00', 'VetSalud', 'e9d5bb30-870c-4548-b254-f1224372320b', 15.00, 22.00, 'SAN-002');


--
-- Data for Name: FarmInventory; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."FarmInventory" ("Id", "FarmId", "ProductId", "Stock", "MinStock", "MaxStock", "Location", "CreatedAt") VALUES ('01a10239-c3bf-497f-a6fd-c2541a272fca', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '7f4c4e59-f15b-4424-93ce-21d78dde4712', 25.0000, 5.0000, 80.0000, 'Almacén principal', '2026-10-02 02:35:18.137857+00');
INSERT INTO public."FarmInventory" ("Id", "FarmId", "ProductId", "Stock", "MinStock", "MaxStock", "Location", "CreatedAt") VALUES ('daae2f56-c66c-4ca5-b1c8-56630beae531', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'd0bd4186-4e82-4f45-a578-6eb5a1512b40', 12.0000, 3.0000, 30.0000, 'Almacén principal', '2026-10-02 02:35:18.14084+00');
INSERT INTO public."FarmInventory" ("Id", "FarmId", "ProductId", "Stock", "MinStock", "MaxStock", "Location", "CreatedAt") VALUES ('e7de0fbd-c932-4b06-a1d0-7a8057c148b1', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '14a69eae-50d5-4672-81e5-23ef97393324', 40.0000, 10.0000, 100.0000, 'Almacén principal', '2026-10-02 02:35:18.124656+00');
INSERT INTO public."FarmInventory" ("Id", "FarmId", "ProductId", "Stock", "MinStock", "MaxStock", "Location", "CreatedAt") VALUES ('f18da7ba-2cc3-4447-aacd-eb635b948f62', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '0a9a4cba-25ff-45ae-921a-b6e3baa0b2ab', 60.0000, 15.0000, 150.0000, 'Almacén principal', '2026-10-02 02:35:18.139559+00');


--
-- Data for Name: WeightRecords; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('079f8be9-81bc-4cd3-8f11-3450533ec7a9', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'd83fd3eb-048c-4195-acde-7b20cff54e50', '2026-10-02', 380.00, 3.50, NULL, NULL, '2026-10-02 02:35:17.990239+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('40460b23-92d2-4633-bca3-2aafeebb6b54', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'dc5448ac-e505-4817-b39e-4e5a15369b32', '2026-10-02', 470.00, 3.50, NULL, NULL, '2026-10-02 02:35:17.990593+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('4dacf8c3-330a-40ee-afbd-f743805824f3', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '7aeb953c-4c03-43f3-9de8-fd7c846a5726', '2026-10-02', 65.00, 3.50, NULL, NULL, '2026-10-02 02:35:17.990677+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('5752f042-4877-4a9d-b72c-9baac6bb30c8', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '7e1e435e-f054-45c0-9553-685be00554cc', '2026-10-02', 180.00, 3.50, NULL, NULL, '2026-10-02 02:35:17.990471+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('58b89fb7-029a-4a80-8fb3-ba4995a61761', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '5f491634-d1f1-47d7-8edf-3fa9c384941a', '2026-10-02', 340.00, 3.50, NULL, NULL, '2026-10-02 02:35:17.990322+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('8e91feeb-eed3-4471-aec3-c382af82d44a', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'b40d9c91-f20b-4646-8ed0-61947196e97b', '2026-10-02', 520.00, 3.50, NULL, NULL, '2026-10-02 02:35:17.854018+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('a3a6a9c9-4af7-44ca-bb34-6f0bb7456281', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', '2e298246-1c1f-47d9-b444-877afa85938f', '2026-10-02', 560.00, 3.50, NULL, NULL, '2026-10-02 02:35:17.990022+00');
INSERT INTO public."WeightRecords" ("Id", "FarmId", "AnimalId", "Date", "WeightKg", "BodyConditionScore", "RecordedByUserId", "Notes", "CreatedAt") VALUES ('c8c63fb0-c0b3-4043-abb1-ad5a99126354', 'ff71f933-ea5b-4cf2-b092-5378d4dfbc2c', 'a9653f5a-aa1e-4882-9713-7cc502f41c69', '2026-10-02', 80.00, 3.50, NULL, NULL, '2026-10-02 02:35:17.990783+00');


--
-- PostgreSQL database dump complete
--

\unrestrict 6xEejy6HlOHLxbbqBrbny03BGy7NJBQ38XeunC0dAv6YPjG6L0gHgX4XyymwMm6
