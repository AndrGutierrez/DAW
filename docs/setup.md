# Instalación, actualización y pruebas

## 1. Configuración

Usar Docker Desktop con contenedores Linux o .NET 10 SDK y PostgreSQL 15. Los comandos están escritos para Git Bash desde la raíz del repositorio. `docker version` debe mostrar servidor disponible.

Si todavía no existe `.env`, ejecutar `cp .env.example .env`. Si ya existe, conservarlo y añadir las variables nuevas necesarias. El archivo `.env` no se versiona ni se incorpora a la imagen.

| Variable | Propósito |
| --- | --- |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT` | Conexión de PostgreSQL y puerto del host |
| `JWT_KEY` | Clave de firma JWT: secreto aleatorio de al menos 32 caracteres |
| `SEED_ADMIN_USERNAME`, `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` | Cuenta administrativa del seed |
| `SEED_EMPLOYEE_USERNAME`, `SEED_EMPLOYEE_EMAIL`, `SEED_EMPLOYEE_PASSWORD` | Cuenta Employee opcional asignada a la finca DEMO |
| `NGINX_HTTP_PORT` | Puerto público de la aplicación; por defecto 18080 |

Las contraseñas requieren mayúscula, minúscula, número, símbolo y al menos ocho caracteres. `openssl rand -base64 48` genera material para `JWT_KEY`. No imprimir ni subir `.env`.

## 2. Docker Compose

```bash
docker compose config -q
docker compose build blazorapp
docker compose up -d db
docker compose run --rm blazorapp --seed
docker compose up -d blazorapp nginx
```

`--seed` ejecuta `MigrateAsync`, crea permisos y roles, verifica la cuenta Admin y carga datos. Repetirlo no duplica los registros de demostración ni reemplaza sus existencias editadas. La contraseña de Admin se sincroniza mediante el mecanismo de restablecimiento de Identity; se valida antes de cambiarla. La cuenta Employee se crea sólo si se configura su contraseña y recibe una membresía a DEMO.

Las dos categorías de insumos están declaradas con `HasData` en el mapeo de EF y se insertan mediante la cuarta migración, `ManagedInventoryCategorySeed`. Si la base ya tiene una categoría con ese nombre, se conserva su UUID y sus referencias. Los demás datos y las cuentas se inicializan mediante servicios en `--seed`; el arranque HTTP normal no aplica migraciones automáticamente.

Comprobar `docker compose ps`, abrir `/swagger` e iniciar sesión. Para detener sin borrar datos: `docker compose down`. Los archivos se guardan en `daw-uploads`; PostgreSQL 15 usa `daw-postgres15-data`.

## 3. Actualizar una instalación anterior

El volumen anterior `daw-postgres-data` corresponde a PostgreSQL 17. Este cambio usa otro volumen para PostgreSQL 15 y conserva el anterior. No montar el directorio físico de PostgreSQL 17 en un servidor 15. Si hay datos que conservar, exportarlos con `pg_dump` desde el servidor anterior e importarlos en una base PostgreSQL 15 antes de ejecutar `--seed`, verificando primero la copia en un entorno separado.

La migración `UnifiedAnimalProductionAndInventory` transforma el modelo:

1. Copia temporalmente los resultados medidos de leche, lana y carne, conservando UUID, fecha, animal, finca, cantidad y fecha de creación. Los atributos antiguos específicos se conservan en `Notes` como metadatos.
2. Crea `AnimalProduction`, `InventoryCategories` y `FarmInventory`.
3. Convierte la categoría enumerada antigua en un catálogo relacionado. Genera `LEGACY-<uuid>` como SKU para los insumos anteriores; permanecen inactivos y con precios pendientes de completar. No inventa su valor comercial.
4. Retira las cuatro tablas de producción anteriores después de preservar los registros convertibles.
5. Marca como fallecidos los animales con sacrificio registrado y actualiza las URL de fotos existentes al endpoint privado usando el nombre real almacenado.
6. Aplica índices únicos, precisión monetaria y restricciones nuevas.

**Condiciones que detienen la migración:** huevos registrados sólo por lote, cantidades inexistentes o no positivas, varios sacrificios del mismo animal, finca distinta entre producción y animal o emails normalizados duplicados. La transacción evita una conversión parcial. Corregir los datos y repetir: no seleccionar animales arbitrarios ni borrar información para ocultar la inconsistencia.

El retorno automático a ese esquema antiguo se bloquea porque no representa fielmente la nueva producción ni la nueva información comercial. Para regresar se requiere una copia verificada de la base anterior. Los contenedores de prueba no usan ni borran datos del entorno personal.

## 4. Ejecutar sin contenedor para la API

Iniciar PostgreSQL con `docker compose up -d db`. Configurar secretos de desarrollo, sustituyendo los valores de ejemplo:

```bash
dotnet user-secrets --project src/Presentation.API set "Jwt:Key" "<secreto-aleatorio>"
dotnet user-secrets --project src/Presentation.API set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=daw;Username=daw;Password=<contraseña>"
dotnet user-secrets --project src/Presentation.API set "Seed:AdminPassword" "<contraseña-admin>"
dotnet user-secrets --project src/Presentation.API set "Seed:EmployeePassword" "<contraseña-employee>"
dotnet tool restore
dotnet run --project src/Presentation.API -- --seed
dotnet run --project src/Presentation.API --launch-profile http
```

El manifiesto de herramientas se encuentra en `.config/dotnet-tools.json`. `--seed` ya aplica migraciones; `dotnet ef database update --project src/Infrastructure --startup-project src/Presentation.API` permite aplicarlas por separado. El perfil HTTP utiliza `http://localhost:5269`; cambiar `baseUrl` en Postman.

## 5. Pruebas automatizadas y Postman

```bash
dotnet test DAW.slnx -c Release
```

Alternativa con SDK Docker para un equipo sin SDK instalado:

```bash
MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd -W):/src" -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet test DAW.slnx -c Release
```

Las pruebas .NET de HTTP usan una base EF InMemory aislada: verifican reglas, contratos y autorización; por sí solas no demuestran que PostgreSQL aplique índices o claves foráneas. Para esa evidencia se ejecutan la colección y `db/verification.sql` sobre PostgreSQL real.

Importar colección y entorno de `postman/`. Completar `adminPassword` y `employeePassword` con los valores configurados en `.env`. Las variables de tokens y UUID se cargan automáticamente. La colección crea registros con nombres únicos y ejecuta altas, lecturas, actualizaciones, restricciones y bajas; el sacrificio de demostración deja su historial irreversible.

Con Newman, proporcionar un entorno local que contenga las contraseñas y mantenerlo fuera del repositorio:

```bash
npx --yes newman run postman/Cattle-Management.full.postman_collection.json \
  -e /ruta/al/entorno-local-con-secretos.json --bail
```

No exportar tokens ni contraseñas al historial de commits. No ejecutar la colección sobre una base de producción: realiza operaciones de escritura para demostrar el CRUD.

## 6. Evidencias SQL y diagrama

```bash
docker compose exec -T db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"' < db/verification.sql
docker compose exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --schema-only --no-owner --no-privileges' > db/schema.sql
npx @liam-hq/cli erd build --input db/schema.sql --format postgres --output-dir db/erd
```

`db/schema.sql` representa el esquema generado desde PostgreSQL; `db/seed-evidence.sql` contiene INSERT de datos de negocio de demostración, sin identidades, contraseñas ni tokens. Es evidencia de datos, no un respaldo completo. Los [resultados ejecutados](evidencias-verificacion.md) incluyen su importación en otra base vacía. Nginx sirve el ERD generado en `/erd/`. El diagrama Mermaid del [modelo unificado](modelo-produccion-y-crud.md) puede consultarse aunque no se haya generado Liam.

## Integración continua

Cada pull request ejecuta el workflow de Andrés en `.github/workflows/ci.yml`: pruebas .NET y la colección [Cattle-Management.postman_collection.json](../postman/Cattle-Management.postman_collection.json) contra PostgreSQL efímero. El workflow genera secretos para PostgreSQL, JWT y Admin durante la ejecución; no requiere secretos guardados en GitHub.

Se conserva esa colección para su configuración actual con Admin. La descarga de fotos ahora envía Bearer porque el contenido dejó de ser público. La [colección completa](../postman/Cattle-Management.full.postman_collection.json), que añade CRUD y los casos Admin/Employee de Fase 3, se ejecuta además con ambas cuentas configuradas según la sección de pruebas. No se omiten silenciosamente sus casos de Employee cuando falta su contraseña.

El workflow existente utiliza PostgreSQL 17 en su entorno temporal; Compose y la evidencia de persistencia de este proyecto utilizan PostgreSQL 15. El workflow no fue modificado en esta intervención.

## 7. Problemas frecuentes

- Error `dockerDesktopLinuxEngine`: Docker Desktop no está iniciado o todavía no terminó de arrancar.
- API 401: falta JWT, expiró o la cuenta está inactiva.
- API 403: el rol no autoriza la operación o la finca no pertenece al usuario.
- API 400: revisar `errors` del Problem Details y los campos del DTO.
- API 409: identificador duplicado, dependencia pendiente, regla de negocio o conflicto concurrente; corregir o recargar antes de repetir.
- Error de clave JWT: configurar un secreto real; el placeholder se rechaza al arrancar.
- Puerto ocupado: cambiar `NGINX_HTTP_PORT` y `baseUrl`. Una pestaña abierta no demuestra que se esté ejecutando la imagen recién construida.
