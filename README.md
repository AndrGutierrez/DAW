# Gestión Ganadera

Sistema multi-finca para registrar y gestionar ganado: bovinos, ovinos, porcinos, caprinos y otras especies. Cubre la ficha de cada animal, su salud, reproducción, producción, alimentación, inventario y las tareas diarias, con autenticación y permisos por roles.

La lógica de negocio está descrita en la [guía de negocio](docs/guia-de-negocio.md) y las decisiones de modelado en el [registro de decisiones](docs/decisiones.md).

## Módulos de negocio

| Módulo | Qué resuelve |
| --- | --- |
| Organización | Fincas, potreros y lotes (rebaños) |
| Animales | Identificación, especie, raza, sexo, edad, peso, estado y genealogía |
| Salud | Historial clínico, vacunación, tratamientos, cuarentena y mortalidad |
| Reproducción | Celo, servicio o inseminación, preñez, parto y destete |
| Producción | Leche, lana, huevos y rendimiento en canal |
| Nutrición | Raciones y alimentación por lote |
| Inventario | Medicamentos, vacunas y alimento con stock y vencimientos |
| Trazabilidad | Movimientos entre potreros y fincas |
| Tareas y alertas | Avisos de vacunas, preñez, stock y peso |
| Finanzas | Compras, ventas y gastos |
| Usuarios y permisos | Roles y permisos con estructura de Laravel Permission (spatie) |
| Auditoría | Tabla de logs (`AuditLogs`) con quién cambió qué y cuándo |
| Fotos y adjuntos | Imagen del animal y documentos, guardados en disco con volumen persistente |

## Requisitos y ejecución

- .NET 10 SDK para desarrollo local, o Docker Desktop.
- PostgreSQL (se levanta con el `docker-compose` del proyecto).

```bash
docker compose up -d db          # levanta PostgreSQL
dotnet tool restore              # habilita dotnet-ef
dotnet ef database update --project src/Infrastructure --startup-project src/Presentation.API
dotnet run --project src/Presentation.API -- --seed   # siembra permisos, grupos, especies y admin
dotnet build DAW.slnx
dotnet test DAW.slnx
dotnet run --project src/Presentation.API --launch-profile http
```

El seed crea los roles (Administrador, Veterinario, Capataz, Operario, Solo lectura), los permisos por acción, las especies y razas, y el usuario `admin` (`REMOVED-SECRET` en desarrollo). Las fotos se suben con `POST /api/animals/{id}/photo` (multipart) y se sirven en `/uploads`; en Docker persisten en el volumen `daw-uploads`.

La API queda en `http://localhost:5269`. La documentación interactiva (Swagger) y la colección de Postman se publican a medida que avanza cada fase.

## Diagrama entidad-relación

El esquema se volca desde PostgreSQL y se visualiza con [Liam ERD](https://liambx.com/docs):

```bash
docker exec daw-postgres pg_dump -U daw -d daw --schema-only --no-owner --no-privileges \
  --exclude-table='public."__EFMigrationsHistory"' | grep -v '^\\' > db/schema.sql
npx @liam-hq/cli erd build --input db/schema.sql --format postgres --output-dir db/erd
npx serve db/erd
```
