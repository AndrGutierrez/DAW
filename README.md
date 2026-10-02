# Sistema de gestión ganadera

API para administrar fincas, potreros, lotes, animales, su estado de salud, pesajes, productos obtenidos del ganado y existencias de insumos. Construida con **.NET 10, C# 14, Onion Architecture, EF Core 10 y PostgreSQL 15**, con ASP.NET Core Identity, JWT, permisos por operación y validación FluentValidation.

Un animal puede producir leche, lana, carne u otros productos a lo largo de su vida. Estos resultados se registran en **una única tabla `AnimalProduction`**, indicando el animal, la finca, la fecha, el método, la cantidad y la unidad. `OperationId` permite registrar varios productos de una misma operación; por ejemplo, carne y piel del mismo sacrificio.

Los productos obtenidos se distinguen de los **insumos comprados**: alimentos, medicamentos y vacunas se catalogan en `Products`, y sus existencias, umbrales y ubicación se guardan **por finca** en `FarmInventory`.

## Iniciar con Docker Desktop

Ejecutar desde la raíz del repositorio en Git Bash. Docker Desktop debe estar iniciado; `docker version` debe mostrar cliente y servidor.

```bash
cp .env.example .env
```

Completar `.env`: `POSTGRES_PASSWORD`, `JWT_KEY`, `SEED_ADMIN_PASSWORD` y, para probar Employee, `SEED_EMPLOYEE_PASSWORD`. Las contraseñas requieren al menos ocho caracteres, mayúscula, minúscula, número y símbolo. Generar un secreto JWT con `openssl rand -base64 48`. **No volver a copiar `.env.example` si ya existe un `.env` configurado.**

```bash
docker compose config -q
docker compose build blazorapp
docker compose up -d db
docker compose run --rm blazorapp --seed
docker compose up -d blazorapp nginx
```

El comando `--seed` aplica las migraciones y carga datos de demostración de forma idempotente. La API estará en `http://localhost:18080` y Swagger en `http://localhost:18080/swagger`. Usuario Admin: valor de `SEED_ADMIN_USERNAME`; contraseña: valor configurado en `.env`. Employee se crea solamente si tiene contraseña configurada.

**Persistencia existente:** PostgreSQL 15 usa el volumen nuevo `daw-postgres15-data`. Un volumen creado con PostgreSQL 17 no se puede conectar directamente a PostgreSQL 15: se conserva y requiere exportación/importación para trasladar sus datos. La migración de producción convierte leche, lana y carne con cantidades válidas; bloquea registros de huevos por lote sin animal y datos inconsistentes. Los insumos antiguos sin precios se conservan inactivos para revisión. No ejecuta un borrado general de la base. El detalle está en [instalación y actualización](docs/setup.md).

## API y CRUD

Para los recursos de la tabla existen **POST, GET de lista, GET por ID, PUT por ID y DELETE por ID**.

| Ruta base | Recurso |
| --- | --- |
| `/api/farms` | Fincas |
| `/api/paddocks` | Potreros |
| `/api/lots` | Lotes; `/api/herds` es un alias persistente |
| `/api/species` | Especies |
| `/api/breeds` | Razas |
| `/api/animals` | Animales |
| `/api/categories` | Categorías de insumos |
| `/api/products` | Catálogo de insumos con SKU, marca y precios |
| `/api/inventory` | Existencias y umbrales por finca |
| `/api/weights` | Pesajes |
| `/api/production` | Productos obtenidos de un animal |

`POST /api/animals` está disponible nuevamente. `PATCH /api/animals/{id}/health-status` conserva la ruta anterior y recibe `{"status":"InTreatment","reason":"Revisión veterinaria"}`. También existe `/health` con `healthStatus`. Ambos registran el historial del cambio.

Otras operaciones: login, registro, renovación de token, perfil actual; administración de permisos; subida, consulta privada y eliminación de fotos; animales sin actualización reciente. Los contratos completos y ejemplos están en Swagger y la [guía del producto](docs/product-and-technical-guide.md).

**Reglas de eliminación:** todos los DELETE requieren Admin/Administrador. Las referencias importantes usan `Restrict`; si existen dependencias se devuelve 409 y deben resolverse primero. El sacrificio es irreversible: su rendimiento puede corregirse, pero el registro no se borra ni el animal se reactiva. El historial sanitario también se conserva.

## Verificación

```bash
MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd -W):/src" -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet test DAW.slnx -c Release
```

Importar [colección Postman](postman/Cattle-Management.full.postman_collection.json) y [entorno](postman/Daw.postman_environment.json). Completar las contraseñas del entorno sin exportarlas al repositorio y ejecutar la colección en orden. Para ejecución automatizada consultar [pruebas reproducibles](docs/setup.md). [db/verification.sql](db/verification.sql) comprueba tablas, índices, restricciones y datos sembrados.

## Arquitectura y documentación

| Capa | Responsabilidad |
| --- | --- |
| `Core.Domain` | Entidades ganaderas y tipos del negocio, sin EF ni HTTP |
| `Core.Application` | Casos de uso CRUD, reglas de coherencia, DTO, validadores y puertos |
| `Infrastructure` | EF Core, PostgreSQL, migraciones, repositorios, Identity, JWT y archivos |
| `Presentation.API` | HTTP, autorización, composición DI y Problem Details |
| `Core.Tests` | Pruebas del dominio, aplicación, persistencia aislada, seguridad y pipeline HTTP |

- [Instalación, migración, ejecución y pruebas](docs/setup.md).
- [Producto, contratos y fundamentos técnicos](docs/product-and-technical-guide.md).
- [Producción unificada y CRUD: reglas y decisiones](docs/modelo-produccion-y-crud.md).
- [Verificación de Fase 2 y Fase 3](docs/fases-2-y-3.md).
- [Evidencias ejecutadas y reproducción de pruebas](docs/evidencias-verificacion.md).
- [Guion de defensa de las fases 1, 2 y 3](docs/guion-defensa-fases-1-2-3.md).
- [Decisiones técnicas](docs/engineering-decisions.md).
- [Visión del negocio completo](docs/guia-de-negocio.md).

La página Blazor actual presenta la API. Los formularios de gestión, informes y flujos completos de reproducción, eventos clínicos, alimentación, tareas y finanzas están previstos para posteriores incrementos; tener sus entidades mapeadas no equivale a disponer de sus casos de uso.
