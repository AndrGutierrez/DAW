# Sistema de gestión de ganado

El sistema de gestión de ganado ofrece una API HTTP para organizar hatos, identificar animales por su arete y consultar su estado de salud actual. Un hato es un grupo de animales y se representa mediante la entidad `Herd`. El sistema utiliza .NET 10 y C# 14, separa las reglas del negocio del almacenamiento y de HTTP mediante la arquitectura Onion, y centraliza las respuestas de error con el formato Problem Details de RFC 7807.

Además de la base anterior, el sistema evoluciona hacia una **gestión ganadera multi-finca** con persistencia en PostgreSQL, autenticación con roles y permisos, y un modelo de negocio que cubre salud, reproducción, producción, nutrición, inventario, trazabilidad, tareas, finanzas y auditoría.

## Resumen del producto

Las personas responsables de una explotación ganadera pueden registrar hatos y animales, consultar a qué hato pertenece un animal y actualizar su estado de salud cuando cambie su condición. La comprobación de aretes sin distinguir mayúsculas y minúsculas evita registros duplicados. La validación rechaza datos obligatorios ausentes y fechas de nacimiento futuras.

La versión actual almacena los registros en memoria; reiniciar el servidor los elimina. La página de inicio de Blazor presenta un resumen de la API. Todavía no están implementados una interfaz completa de gestión, el almacenamiento persistente, la autenticación ni los informes.

La [guía del producto y su implementación técnica](docs/product-and-technical-guide.md) explica el flujo de trabajo, el contrato de la API, la arquitectura y la estrategia de verificación. El [registro de decisiones técnicas](docs/engineering-decisions.md) detalla cada decisión de diseño, su propósito, sus evidencias y sus limitaciones.

La lógica de negocio del sistema ampliado está descrita en la [guía de negocio](docs/guia-de-negocio.md) y las decisiones de modelado en el [registro de decisiones](docs/decisiones.md).

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

## Requisitos

- SDK de .NET 10 para compilar localmente, o Docker Desktop para compilar mediante contenedores.
- PostgreSQL (se levanta con el `docker-compose` del proyecto).
- Postman o Newman para ejecutar la colección de pruebas de la API.

## Clonar, compilar y ejecutar las pruebas

```bash
git clone https://github.com/AndrGutierrez/DAW.git
cd DAW
docker compose up -d db          # levanta PostgreSQL
dotnet tool restore              # habilita dotnet-ef
dotnet ef database update --project src/Infrastructure --startup-project src/Presentation.API
dotnet run --project src/Presentation.API -- --seed   # siembra permisos, roles, especies y admin
dotnet build DAW.slnx
dotnet test DAW.slnx
```

El seed crea los roles (Administrador, Veterinario, Capataz, Operario, Solo lectura), los permisos por acción, las especies y razas, y el usuario `admin` (`REMOVED-SECRET` en desarrollo). Las fotos se suben con `POST /api/animals/{id}/photo` (multipart) y se sirven en `/uploads`; en Docker persisten en el volumen `daw-uploads`.

La solución contiene cuatro proyectos de la aplicación y un proyecto de pruebas:

| Proyecto | Responsabilidad | Referencias a otros proyectos |
| --- | --- | --- |
| `Core.Domain` | Entidades puras y estado del negocio | Ninguna |
| `Core.Application` | Casos de uso e interfaz del repositorio | `Core.Domain` |
| `Infrastructure` | Repositorio con alcance por petición, almacenamiento en memoria, persistencia EF Core, identidad y archivos | `Core.Application`, `Core.Domain` |
| `Presentation.API` | Controladores HTTP, middleware y alojamiento de Blazor | `Core.Application`, `Infrastructure` |
| `Core.Tests` | Pruebas unitarias y de integración HTTP | Proyectos de la aplicación que se verifican |

`Herd` y `Animal` heredan de `BaseEntity`, que asigna un identificador GUID y una fecha de creación en UTC. Cada animal mantiene una referencia a su hato y comienza con estado de salud sano. El almacenamiento actual se vacía cuando se reinicia el proceso.

En `Presentation.API/Program.cs`, el normalizador de aretes sin estado compartido se registra como singleton, el validador de registros como transient y el servicio de catálogo y el repositorio como scoped, con alcance por petición HTTP. El repositorio depende de un almacén singleton en memoria con acceso sincronizado; así, un animal creado en una petición puede consultarse en otra. Este almacén no depende de servicios scoped. Todavía no existe un contexto de base de datos; un futuro adaptador de persistencia deberá registrar su `DbContext` como scoped.

## Fundamentos de la arquitectura

**Arquitectura Onion e inversión de dependencias.** Las reglas del negocio se ubican en el centro. Por eso, `Core.Domain` contiene `Herd`, `Animal` y `BaseEntity` sin referencias a ASP.NET Core ni a paquetes de persistencia. `Core.Application` depende del dominio y define `ICattleRepository`, el contrato que necesita su caso de uso. `Infrastructure` implementa ese contrato y `Presentation.API` conecta la implementación con los controladores HTTP en `Program.cs`. Durante la ejecución, la llamada pasa del controlador al caso de uso y al repositorio; las referencias entre proyectos apuntan hacia el núcleo. Una implementación con base de datos puede sustituir al repositorio en memoria sin cambiar el modelo del dominio. Este diseño sigue la dirección de dependencias descrita por [Palermo en Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/).

**Inyección de dependencias y ciclos de vida.** ASP.NET Core crea un ámbito de servicios para cada petición. El servicio de catálogo y el repositorio son `Scoped` porque coordinan una petición; el validador es `Transient` porque no comparte estado y puede crearse cada vez que se solicita; el normalizador de aretes es `Singleton` porque no mantiene estado mutable y puede reutilizarse. `InMemoryCattleStore` también es singleton para que distintas peticiones consulten los mismos registros. Sincroniza el acceso a los diccionarios y no depende de servicios scoped, lo que evita una dependencia cautiva. Sus datos no son persistentes. La prueba de integración obtiene servicios desde dos ámbitos para comprobar estos ciclos de vida. Véase la [documentación de Microsoft sobre ciclos de vida](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes).

**Errores centralizados con RFC 7807.** `ExceptionMiddleware` se ejecuta antes de los endpoints de los controladores, captura sus excepciones no manejadas y convierte los recursos inexistentes en respuestas 404, las operaciones inválidas en 400 y los fallos inesperados en 500. Cada respuesta de ese mapeo utiliza `application/problem+json` y los campos estándar `type`, `title`, `status`, `detail` e `instance`. En un error 500, `detail` es genérico para no exponer el mensaje interno de la excepción ni su traza. El controlador de demostración, las pruebas de integración HTTP y la colección de Postman comprueban este comportamiento. Véase [RFC 7807](https://www.rfc-editor.org/info/rfc7807/).

## Modelo de datos, persistencia y autenticación

**Persistencia con EF Core y PostgreSQL.** El modelo de negocio se mapea con Entity Framework Core usando **Fluent API** (`IEntityTypeConfiguration<T>`), migraciones y consultas con **LINQ**, sobre PostgreSQL. La cadena de conexión se configura en `ConnectionStrings:Default` y el `DbContext` se registra mediante `AddInfrastructure`.

**Multi-finca.** La entidad `Farm` es la raíz; las entidades operativas la referencian y los catálogos (`Species`, `Breed`, `Product`) son globales.

**Entidades por módulo.** Organización (`Farm`, `Paddock`, `Lot`, `UserFarm`); catálogos (`Species`, `Breed`, `Disease`, `Product`, `Supplier`); animal (`Animal`, `WeightRecord`, `HealthStatusChange`); salud (`HealthEvents` con vacunación, tratamiento, enfermedad, desparasitación, cuarentena y mortalidad); reproducción (`ReproductiveEvents`, `SemenBatch`); producción (leche, huevos, lana, canal); nutrición (`Ration`, `RationIngredient`, `FeedingRecord`); inventario (`ProductBatch`, `StockMovement`); trazabilidad (`AnimalMovement`); tareas y alertas (`Task`, `AlertRule`, `Alert`); finanzas (`Transaction`); soporte (`Attachment`, `AuditLog`).

**Autenticación y permisos.** ASP.NET Core Identity con **JWT** para la API. Los permisos siguen la estructura de **Laravel Permission** (spatie): `Roles`, `Permissions`, `RolePermissions`, `UserPermissions` y `UserRoles`, con `guard_name`. Un usuario obtiene permisos por sus roles y también de forma directa; el superusuario no tiene restricciones. Los endpoints protegidos usan `[Authorize]` y el atributo `[HasPermission("...")]`.

**Fotos y archivos.** Las imágenes del animal se guardan en disco (`Storage:RootPath`), se referencian desde `Animal.PhotoUrl` y se registran como `Attachment`. En Docker persisten en el volumen nombrado `daw-uploads` y se sirven bajo `/uploads`.

## Diagrama entidad-relación

El esquema se volca desde PostgreSQL y se visualiza con [Liam ERD](https://liambx.com/docs):

```bash
docker exec daw-postgres pg_dump -U daw -d daw --schema-only --no-owner --no-privileges \
  --exclude-table='public."__EFMigrationsHistory"' | grep -v '^\\' > db/schema.sql
npx @liam-hq/cli erd build --input db/schema.sql --format postgres --output-dir db/erd
npx serve db/erd
```

## Ejecutar la API

Con el SDK de .NET 10:

```bash
dotnet run --project src/Presentation.API --launch-profile http
```

La dirección local es `http://localhost:5269`. Cambia la variable `baseUrl` de la colección de Postman a esta dirección.

En Windows con Git Bash y Docker Desktop, inicia Docker y comprueba que su motor esté disponible:

```bash
docker desktop start
docker version
```

`docker version` debe mostrar las versiones del cliente y del servidor. Si solo aparece el cliente, espera a que Docker Desktop termine de iniciar y ejecuta `docker version` otra vez. Después, ejecuta las pruebas:

```bash
MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd -W):/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test DAW.slnx -c Release
```

Luego, construye la imagen y ejecuta la aplicación:

```bash
docker build -t cattle-management .
docker run --rm -p 18080:8080 -e ASPNETCORE_ENVIRONMENT=Development -e DisableHttpsRedirection=true cattle-management
```

La dirección con Docker es `http://localhost:18080`, que coincide con la colección de Postman incluida. Detén el contenedor que se ejecuta en primer plano con Ctrl+C.

## API y manejo de errores

| Método | Ruta | Propósito |
| --- | --- | --- |
| `POST` | `/api/herds` | Crear un hato |
| `GET` | `/api/herds` | Listar los hatos |
| `GET` | `/api/herds/{id}` | Consultar un hato |
| `POST` | `/api/animals` | Registrar un animal asociado a un hato |
| `GET` | `/api/animals` | Listar los animales |
| `GET` | `/api/animals/{id}` | Consultar un animal |
| `PATCH` | `/api/animals/{id}/health-status` | Cambiar el estado de salud de un animal (`0` sano, `1` en observación, `2` en tratamiento) |
| `GET` | `/api/demo/errors/{kind}` | Provocar un error de ejemplo únicamente en el entorno Development |

Endpoints nuevos (autenticación, permisos y archivos):

| Método | Ruta | Propósito |
| --- | --- | --- |
| `POST` | `/api/auth/register` | Registrar un usuario |
| `POST` | `/api/auth/login` | Iniciar sesión y obtener un token JWT |
| `POST` | `/api/auth/refresh` | Renovar el token de acceso |
| `GET` | `/api/auth/me` | Datos del usuario autenticado y sus permisos |
| `POST` | `/api/animals/{id}/photo` | Subir la foto de un animal |
| `DELETE` | `/api/animals/{id}/photo` | Eliminar la foto de un animal |
| `GET` | `/api/admin/permissions` | Listar los permisos (requiere `permissions.view_permission`) |
| `GET` | `/api/admin/roles` | Listar roles y sus permisos (requiere `roles.view_role`) |
| `POST` | `/api/admin/roles/{roleId}/permissions/{permissionId}` | Otorgar un permiso a un rol |
| `DELETE` | `/api/admin/roles/{roleId}/permissions/{permissionId}` | Quitar un permiso a un rol |

`ExceptionMiddleware` se registra antes de los controladores en `Program.cs`. Convierte `KeyNotFoundException` en 404, `UnauthorizedAccessException` en 401, `InvalidOperationException` y los argumentos inválidos en 400, y las excepciones inesperadas en 500. Las respuestas tienen el tipo de contenido `application/problem+json` e incluyen `type`, `title`, `status`, `detail` e `instance`. Las respuestas HTTP 500 ocultan los mensajes internos y las trazas de las excepciones. `ApiIntegrationTests` inicia la aplicación real de ASP.NET Core en memoria y comprueba las tres respuestas mediante HTTP, además de los ciclos de vida de DI y los endpoints de ganado.

## Comprobaciones de calidad

| Objetivo | Implementación | Verificación |
| --- | --- | --- |
| Cuatro capas Onion y un dominio puro | Cuatro proyectos de la aplicación en `DAW.slnx`; `Core.Domain` no tiene referencias a otros proyectos | `dotnet build DAW.slnx` |
| Entidades relacionadas con GUID y fecha de creación UTC | `BaseEntity`, `Herd` y `Animal` | `DomainAndApplicationTests` |
| Ciclos de vida adecuados en DI | Registros en `Presentation.API/Program.cs` | `DependencyInjectionUsesExpectedLifetimes` |
| Middleware RFC 7807 integrado en el flujo HTTP | `ExceptionMiddleware` antes de los controladores en `Program.cs` | `RegisteredPipelineReturnsProblemDetails` y la colección de Postman |
| Persistencia con EF Core y LINQ | `AppDbContext`, configuraciones Fluent API y migraciones | `dotnet ef database update` |
| Autenticación y permisos | Identity + JWT y estructura Laravel Permission | `POST /api/auth/login` y endpoints `/api/admin` |
| Diagrama entidad-relación | Volcado del esquema y Liam ERD | `db/schema.sql` y `db/erd` |

Para una comprobación rápida con cURL:

```bash
curl -i http://localhost:18080/api/demo/errors/not-found
curl -i http://localhost:18080/api/demo/errors/invalid-operation
curl -i http://localhost:18080/api/demo/errors/unexpected
```

Los códigos de estado esperados son 404, 400 y 500. En la última respuesta, el campo `detail` contiene un mensaje genérico.

## Verificación de la API con Postman

Importa [la colección de la API de gestión de ganado](postman/Cattle-Management.postman_collection.json), ajusta `baseUrl` según la forma de ejecución y ejecuta las peticiones en orden. La colección crea un hato y un animal, consulta el animal, cambia su estado de salud y comprueba los errores de negocio y de ejemplo. El endpoint de demostración está disponible únicamente cuando `ASPNETCORE_ENVIRONMENT=Development`.

Para ejecutar la misma colección desde una terminal mientras la aplicación está disponible en el puerto 18080:

```bash
npx --yes newman run postman/Cattle-Management.postman_collection.json
```

La petición **Unexpected exception returns safe 500 Problem Details** comprueba el estado de la respuesta, `Content-Type: application/problem+json`, los campos estándar y la ausencia de detalles internos de la excepción. Se conserva su nombre exacto para localizarla en la colección. Estas comprobaciones permiten que quienes consumen o mantienen la API verifiquen su contrato de errores de forma reproducible.
