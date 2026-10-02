# Guion de defensa: sistema de gestión ganadera

## Cómo utilizar este documento

Este guion sigue las **doce preguntas** de `Auditoría y Verificación de Repositorio (Fases 1, 2 y 3).docx`: cuatro por fase, diez puntos cada una, **40 puntos por fase y 120 en total**. La tabla oficial del documento identifica al Grupo 3, Andrés Augusto Gutiérrez Ordaz y Santiago Hernández Gelvez, con el sistema de gestión de ganado y el repositorio DAW. Las referencias a ferretería son ejemplos de los requisitos técnicos; las entidades y los datos de este producto corresponden al negocio ganadero.

Las ponderaciones originales de las asignaciones de Fase 2 y Fase 3 se conservan en [la matriz de trazabilidad](fases-2-y-3.md). Este guion utiliza la guía de auditoría posterior. No representa una autoevaluación numérica ni garantiza una calificación.

Estudiar primero los párrafos **«Qué decir»**; usar **«Qué abrir»** como recorrido por el repositorio. En el editor, buscar el nombre de clase o método señalado: es más estable que memorizar números de línea. Los enlaces son rutas reales del proyecto, aunque sus nombres difieran de los ejemplos del documento del profesor.

Recorrido sugerido: apertura y negocio, 2 minutos; Fase 1, 4 minutos; Fase 2, 4 minutos; Fase 3, 4 minutos; demostración, 4–6 minutos. Si el tiempo es menor, conservar una explicación y una evidencia por pregunta.

## 1. Apertura: qué problema resuelve el producto

**Qué decir:**

> Nuestro producto es un sistema de gestión ganadera. Su propósito es mantener información trazable de las fincas, sus potreros y lotes, los animales y sus operaciones: cambios de salud, pesajes, productos obtenidos e inventario de insumos. Permite que la administración mantenga los catálogos y que los empleados registren operaciones de las fincas que tienen asignadas.
>
> En estas tres fases construimos y verificamos el backend. La primera establece la separación de responsabilidades y el manejo uniforme de errores; la segunda hace que los datos y sus relaciones se conserven en PostgreSQL; la tercera controla quién accede, qué acciones puede ejecutar y qué entradas son válidas. Presentaremos esas capacidades mediante código, consultas SQL y solicitudes HTTP.

**Qué abrir:** [README](../README.md), [guía del modelo y CRUD](modelo-produccion-y-crud.md) y `/swagger` en la instalación actualizada.

### Una distinción esencial del negocio

**Qué decir:**

> Tenemos dos conceptos de producto. Los insumos que la finca compra, como concentrado, vacunas o desparasitantes, están en `Products`. Cada insumo tiene SKU, categoría, marca, unidad, precio y costo. Sus existencias, mínimo, máximo y ubicación se guardan en `FarmInventory`, porque pueden cambiar entre fincas.
>
> Los productos obtenidos de un animal se guardan en una sola tabla, `AnimalProduction`. Una fila representa un producto obtenido, con animal, finca, fecha, cantidad, unidad y método. Un animal puede tener muchos registros. Por ejemplo, varios ordeños o una esquila. En un sacrificio se pueden registrar carne y piel con el mismo `OperationId`, que identifica la operación común.
>
> El sistema rechaza otro sacrificio del mismo animal y la producción posterior incompatible. Registrar el sacrificio deja al animal fallecido. Se puede corregir el rendimiento de ese registro, pero no borrarlo ni reactivar al animal para ocultar el hecho. Esto conserva la coherencia y la trazabilidad.

**Qué abrir:** [Catalogs.cs](../src/Core.Domain/Livestock/Catalogs.cs), clases `Product` e `InventoryCategory`; [Production.cs](../src/Core.Domain/Livestock/Production.cs), `AnimalProduction`; [ResourceDefinitions.cs](../src/Core.Application/Management/ResourceDefinitions.cs), `ProductionDefinition.CheckAsync` y `BeforeDeleteAsync`.

**Comprobación:** `OneAnimalProducesMultipleProductsAndSlaughterDisallowsLaterMilking`, en [ManagementIntegrationTests.cs](../tests/Core.Tests/ManagementIntegrationTests.cs), y casos de carne/piel/sacrificio en la [colección Postman completa](../postman/Cattle-Management.full.postman_collection.json).

## 2. Fase 1: arquitectura y resiliencia

### Pregunta 1.1. Pureza del dominio e inversión de dependencias

**Qué decir:**

> Usamos Onion Architecture para separar el negocio de los detalles tecnológicos. `Core.Domain` contiene las entidades y tipos ganaderos y no referencia EF Core, ASP.NET Core ni PostgreSQL. Sus entidades no tienen atributos de tablas o columnas.
>
> `Core.Application` define contratos, DTO, validadores y casos de uso. Allí está `IManagementRepository`, que expresa las operaciones de persistencia necesarias sin introducir `DbContext`. `Infrastructure` implementa ese contrato usando EF Core. `Presentation.API` expone HTTP y compone las dependencias.
>
> Esto aplica la inversión de dependencias: el caso de uso depende de una abstracción definida hacia el núcleo, y el adaptador externo implementa esa abstracción. Cambiar la forma de guardar los datos requeriría sustituir el adaptador; las entidades ganaderas no necesitan conocer el nuevo motor. La capacidad de cambiarlo no significa que otro motor ya esté implementado o probado.

**Qué abrir, en este orden:**

1. [Core.Domain.csproj](../src/Core.Domain/Core.Domain.csproj): no contiene referencias de paquetes ni de otros proyectos.
2. [Animals.cs](../src/Core.Domain/Livestock/Animals.cs) y [Production.cs](../src/Core.Domain/Livestock/Production.cs): entidades sin anotaciones de EF.
3. [Core.Application.csproj](../src/Core.Application/Core.Application.csproj): referencia al dominio y FluentValidation; no referencia a Infrastructure.
4. [Contracts.cs](../src/Core.Application/Management/Contracts.cs): interfaz `IManagementRepository`.
5. [CrudService.cs](../src/Core.Application/Management/CrudService.cs): constructor recibe la interfaz.
6. [ManagementRepository.cs](../src/Infrastructure/Persistence/ManagementRepository.cs): implementación del puerto con `AppDbContext`.

**Por qué sirve al producto:** permite modificar HTTP, almacenamiento o reglas de validación con responsabilidades identificables. La dirección de las referencias de proyectos protege la separación.

**Precisión para una pregunta adicional:** el dominio es independiente de frameworks. Eso no implica que todas sus propiedades sean inmutables: las entidades actuales permiten cambios coordinados por los casos de uso.

### Pregunta 1.2. Inyección de dependencias y ciclos de vida

**Qué decir:**

> Los objetos se reciben por constructor y el contenedor crea sus implementaciones. Elegimos los ciclos de vida según el estado que necesita cada servicio.
>
> El contexto de datos, repositorios y servicios de negocio son scoped: dentro de una solicitud comparten el mismo alcance y unidad de trabajo. Los validadores de DTO son transient, de modo que se resuelven como instancias cortas para cada validación. Las utilidades de tokens y almacenamiento son singleton: utilizan configuración y no capturan el contexto de datos ni el usuario de una solicitud.
>
> Evitamos una dependencia cautiva: un singleton no debe retener un servicio scoped, porque extendería su vida más allá de la solicitud y podría compartir estado entre peticiones. La inyección facilita además sustituir el proveedor de datos en las pruebas.

**Qué abrir:** [DependencyInjection.cs](../src/Infrastructure/DependencyInjection.cs), `AddInfrastructure` y `AddResource`; [Program.cs](../src/Presentation.API/Program.cs), `AddValidatorsFromAssemblyContaining(..., ServiceLifetime.Transient)`.

| Registro | Ciclo | Explicación breve |
| --- | --- | --- |
| `AddDbContext<AppDbContext>` | Scoped por defecto | Contexto y seguimiento por alcance |
| `IManagementRepository`, `ICrudService<T>`, autenticación y permisos | Scoped | Comparten dependencias de datos de la solicitud |
| `IValidator<TRequest>` | Transient | Validación de entradas |
| `ITokenService`, `IFileStorage` | Singleton | Operaciones sin contexto de datos cautivo |
| `IValidateOptions<JwtOptions>` | Singleton | Valida configuración del proceso; no es un validador de DTO |

**Comprobación:** `DependencyInjectionUsesExpectedLifetimes`, en [ApiIntegrationTests.cs](../tests/Core.Tests/ApiIntegrationTests.cs), compara instancias: mismo contexto dentro de un scope, contextos distintos entre scopes, validadores distintos y servicio de tokens compartido.

### Pregunta 1.3. Middleware global registrado en el pipeline

**Qué decir:**

> El middleware de excepciones envuelve el procesamiento posterior mediante `await next(context)` dentro de un `try/catch`. Una excepción de un caso de uso sube por el pipeline, se registra internamente y se convierte en una respuesta HTTP uniforme.
>
> No basta con tener la clase o probarla aislada. En `Program.cs` está registrado antes de autenticación, autorización y controladores. Comprobamos también el pipeline real de la aplicación: los endpoints de demostración generan errores controlados y un fallo inesperado. Este último produce un 500 con mensaje público genérico y sin exponer el detalle interno ni una traza de pila.

**Qué abrir:** [ExceptionMiddleware.cs](../src/Presentation.API/Middleware/ExceptionMiddleware.cs), `InvokeAsync`; [Program.cs](../src/Presentation.API/Program.cs), `app.UseMiddleware<ExceptionMiddleware>()`, `UseAuthentication`, `UseAuthorization` y `MapControllers`.

**Comprobación:** `RegisteredPipelineReturnsProblemDetails`, en [ApiIntegrationTests.cs](../tests/Core.Tests/ApiIntegrationTests.cs), y [ExceptionMiddlewareTests.cs](../tests/Core.Tests/ExceptionMiddlewareTests.cs). En Development, abrir `/api/demo/errors/unexpected` y revisar respuesta y cabeceras en Network de Edge.

**Por qué sirve al producto:** un cliente puede distinguir una entrada inválida de un conflicto o un fallo interno sin interpretar mensajes de excepciones del servidor.

### Pregunta 1.4. Errores RFC 7807 / Problem Details

**Qué decir:**

> Los errores de la API usan `application/problem+json` y cinco campos: `type` identifica el tipo de problema; `title` lo resume; `status` coincide con el código HTTP; `detail` explica el caso; e `instance` identifica la ruta de la petición.
>
> Las validaciones añaden `errors`, agrupado por propiedad. Además de las excepciones, cubrimos JSON inválido y rechazos HTTP que no generan una excepción, como un 401 o un 403. `Program.cs` configura las respuestas de model binding y `UseStatusCodePages` completa las respuestas vacías de las rutas `/api`.

**Qué abrir:** [ExceptionMiddleware.cs](../src/Presentation.API/Middleware/ExceptionMiddleware.cs), construcción de `ProblemDetails` y `ContentType`; [Program.cs](../src/Presentation.API/Program.cs), `InvalidModelStateResponseFactory` y `UseStatusCodePages`.

**Comprobación:** pruebas anteriores y casos Postman de entrada inválida, petición sin token, operación prohibida, conflicto y error inesperado. Verificar **código HTTP + Content-Type + cuerpo**, no solamente el texto visible en el navegador.

| Código | Ejemplo del producto |
| --- | --- |
| 400 | Precio negativo, SKU inválido o JSON incorrecto |
| 401 | Falta una identidad autenticada válida |
| 403 | Employee intenta mantener el catálogo de productos |
| 404 | Registro inexistente o fuera del alcance visible de finca |
| 409 | SKU duplicado, categoría referenciada o sacrificio incompatible |
| 500 | Fallo inesperado: respuesta genérica y registro interno |

## 3. Fase 2: persistencia relacional

### Pregunta 2.1. Fluent API separado del dominio

**Qué decir:**

> Las reglas del esquema están en Infrastructure mediante clases `IEntityTypeConfiguration<T>`. Configuran tablas, claves, tamaños, índices, conversiones y relaciones. El contexto las incorpora con `ApplyConfigurationsFromAssembly`.
>
> La entidad describe información del negocio; la configuración describe cómo se guarda. Esa separación conserva el dominio limpio y aplica responsabilidad única. Varias configuraciones comparten un archivo temático, pero cada clase implementa la configuración de una entidad concreta.

**Qué abrir:** [CatalogConfigurations.cs](../src/Infrastructure/Persistence/Configurations/Livestock/CatalogConfigurations.cs), `ProductConfiguration`; [ProductionConfigurations.cs](../src/Infrastructure/Persistence/Configurations/Livestock/ProductionConfigurations.cs), `InventoryCategoryConfiguration`, `FarmInventoryConfiguration` y `AnimalProductionConfiguration`; [AppDbContext.cs](../src/Infrastructure/Persistence/AppDbContext.cs), `OnModelCreating`.

**Comprobación:** [migraciones versionadas](../src/Infrastructure/Persistence/Migrations/20261002021355_UnifiedAnimalProductionAndInventory.cs), [snapshot](../src/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs), [schema.sql](../db/schema.sql) y consulta de `__EFMigrationsHistory` en [verification.sql](../db/verification.sql). La comprobación de EF no encontró cambios de modelo pendientes de migración.

**Explicación clave:** escribir la configuración no modifica por sí solo una base existente. La migración transforma el esquema; el historial indica qué migraciones se aplicaron en esa base concreta.

### Pregunta 2.2. Dinero e integridad referencial

**Qué decir:**

> Precio y costo son `decimal` en C# y `numeric(18,2)` en PostgreSQL. Configuramos ambos con `HasPrecision(18, 2)`. La precisión permite hasta dieciocho dígitos totales y dos decimales. Esto evita usar representación binaria de coma flotante para importes y establece una escala explícita.
>
> Una categoría tiene muchos productos y cada producto referencia una categoría. Usamos `DeleteBehavior.Restrict` en esa relación: no se puede borrar una categoría mientras tenga productos. Así evitamos un borrado en cascada que destruya información. El cliente recibe 409 y debe resolver las dependencias.
>
> El inventario pertenece al par finca–producto. Mínimo, máximo y ubicación están ahí porque el mismo insumo puede tener existencias diferentes en cada finca. La validación de máximo mayor que mínimo también existe como restricción de base de datos.

**Qué abrir:** [Catalogs.cs](../src/Core.Domain/Livestock/Catalogs.cs), campos monetarios de `Product`; [CatalogConfigurations.cs](../src/Infrastructure/Persistence/Configurations/Livestock/CatalogConfigurations.cs), precisión y relación; [ProductionConfigurations.cs](../src/Infrastructure/Persistence/Configurations/Livestock/ProductionConfigurations.cs), defaults y `CHECK` de `FarmInventory`.

**Comprobación:** [PostgresConstraints.sql](../tests/Core.Tests/Fixtures/PostgresConstraints.sql), ejecutado sobre PostgreSQL real dentro de una transacción que termina en rollback; [schema.sql](../db/schema.sql) muestra los tipos y las claves foráneas. Postman también comprueba rechazo al eliminar una categoría referenciada.

**Precisión:** las relaciones restrictivas señaladas protegen el núcleo operativo; no afirmamos que todas las relaciones de las 43 tablas tengan idéntica política de borrado.

### Pregunta 2.3. SKU único e índices

**Qué decir:**

> El SKU identifica un insumo en el catálogo. Configuramos `HasIndex(p => p.SKU).IsUnique()`, y la migración crea ese índice en PostgreSQL. La aplicación comprueba duplicados para dar una respuesta comprensible y la base impide duplicados incluso ante escrituras concurrentes o fuera de HTTP.
>
> El índice también permite al motor resolver búsquedas por SKU sin recorrer necesariamente toda la tabla. La guía menciona O(1), pero un índice B-tree no garantiza tiempo constante: su búsqueda normalmente tiene comportamiento logarítmico. En tablas pequeñas el optimizador puede elegir un escaneo secuencial; eso no significa que falte el índice.

**Qué abrir:** `ProductConfiguration` en [CatalogConfigurations.cs](../src/Infrastructure/Persistence/Configurations/Livestock/CatalogConfigurations.cs); creación de `IX_Products_SKU` en la [migración de producción e inventario](../src/Infrastructure/Persistence/Migrations/20261002021355_UnifiedAnimalProductionAndInventory.cs); `pg_indexes` en [verification.sql](../db/verification.sql).

**Comprobación:** duplicado SKU → 409 en Postman y rechazo SQL en [PostgresConstraints.sql](../tests/Core.Tests/Fixtures/PostgresConstraints.sql). Identity también tiene índices únicos para nombre y correo normalizados; el correo único se configura en [IdentityConfigurations.cs](../src/Infrastructure/Persistence/Configurations/IdentityConfigurations.cs).

### Pregunta 2.4. AsNoTracking y HasData

**Qué decir:**

> Las consultas de lectura usan `AsNoTracking`: EF no conserva esas entidades en su rastreador de cambios, reduciendo trabajo y memoria innecesarios. En una edición pedimos explícitamente seguimiento para que EF detecte los cambios que se guardarán.
>
> Las categorías iniciales, Alimentación animal y Sanidad animal, están declaradas con `HasData`, identificadores y fecha fijos. EF las incorpora al modelo y a la cuarta migración. Si una base ya tiene esas categorías por nombre, la migración conserva sus identificadores y las referencias existentes. En una base nueva usa los identificadores constantes del modelo.
>
> El resto de la demostración se inicializa mediante `--seed`: productos, existencias, animales, rendimientos, roles y cuentas. Las cuentas se crean con Identity y contraseñas configuradas localmente. No guardamos credenciales en el código ni en `HasData`. Repetimos la siembra y comprobamos que no duplicó los registros previstos.

**Qué abrir:** [ManagementRepository.cs](../src/Infrastructure/Persistence/ManagementRepository.cs), `ListAsync`, `GetAsync` y `ExistsAsync`; [ProductionConfigurations.cs](../src/Infrastructure/Persistence/Configurations/Livestock/ProductionConfigurations.cs), `InventoryCategoryConfiguration.HasData`; [cuarta migración](../src/Infrastructure/Persistence/Migrations/20261002030136_ManagedInventoryCategorySeed.cs); [DatabaseSeeder.cs](../src/Infrastructure/Persistence/DatabaseSeeder.cs).

**Comprobación:** `SeedingIsIdempotentAndReadsDoNotTrackEntities` en [ManagementIntegrationTests.cs](../tests/Core.Tests/ManagementIntegrationTests.cs) verifica conteos y rastreador vacío después de leer productos. En PostgreSQL nuevo, con siembra repetida, se comprobaron 2 categorías, 4 insumos, 4 inventarios, 8 animales y 4 rendimientos. Las [evidencias ejecutadas](evidencias-verificacion.md) incluyen migración de una base ya sembrada e importación de las exportaciones SQL.

**Precisión:** `HasData` se materializa al aplicar migraciones; arrancar HTTP normalmente no siembra ni migra automáticamente. EF lo denomina datos administrados por el modelo; las identidades requieren inicialización a través de servicios ([documentación oficial](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)).

## 4. Fase 3: seguridad y validación

### Pregunta 3.1. JWT: emisión y validación

**Qué decir:**

> El login consulta la identidad persistida y verifica su contraseña. Si la cuenta está activa y las credenciales son correctas, devuelve un token de acceso firmado. Incluye `sub`, que identifica al usuario, `email`, nombre y roles, además de emisor, audiencia y expiración.
>
> Cada petición protegida envía `Authorization: Bearer` con ese token. El middleware JWT comprueba firma, emisor, audiencia y vigencia antes de considerar autenticada la identidad. La firma usa HMAC-SHA256 con una clave secreta del servidor. Impide modificar los claims sin invalidar el token; no cifra el contenido ni se utiliza para almacenar la contraseña.
>
> La autenticación no depende de una sesión de navegador almacenada en memoria del servidor. La autorización sí consulta permisos, estado activo y fincas vigentes en la base. Además, persistimos tokens de renovación y los rotamos: reutilizar el anterior se rechaza.

**Qué abrir:** [AuthService.cs](../src/Infrastructure/Security/AuthService.cs), `LoginAsync` y `BuildResponseAsync`; [JwtTokenService.cs](../src/Infrastructure/Security/JwtTokenService.cs), `CreateAccessToken`; [Program.cs](../src/Presentation.API/Program.cs), `AddAuthentication`, `AddJwtBearer` y `TokenValidationParameters`.

**Comprobación:** `AccessTokenCarriesIdentityRolesAndSuperuserClaims` en [SecurityAndStorageTests.cs](../tests/Core.Tests/SecurityAndStorageTests.cs) comprueba claims y algoritmo; `SeededAdminCanLoginAndReadRoles` en [AuthenticationIntegrationTests.cs](../tests/Core.Tests/AuthenticationIntegrationTests.cs) comprueba el correo en el token del login. Postman demuestra Admin/Employee, acceso autenticado, 401 sin token y rechazo de refresh reutilizado.

**Para la demostración:** inspeccionar el payload localmente. No pegar tokens reales en páginas externas, capturas públicas o commits.

### Pregunta 3.2. Contraseñas con hash y sal

**Qué decir:**

> La identidad usa el `PasswordHasher` nativo de ASP.NET Core Identity. Creamos cuentas mediante `UserManager.CreateAsync` y verificamos mediante `CheckPasswordAsync`. Este mecanismo deriva la contraseña con PBKDF2 y una sal aleatoria; conserva el resultado y los parámetros necesarios dentro de `PasswordHash`.
>
> No guardamos una contraseña recuperable. La sal hace que dos usuarios con la misma contraseña tengan hashes distintos. No necesitamos una columna separada `PasswordSalt`, porque el formato de Identity ya incorpora esa información. Una contraseña incorrecta se rechaza usando el mecanismo del framework.
>
> La sal dificulta reutilizar tablas precalculadas y la derivación aumenta el costo de cada intento. No vuelve imposible adivinar una contraseña débil: siguen siendo necesarios requisitos de contraseña y protección operativa del servidor.

**Qué abrir:** [IdentityEntities.cs](../src/Infrastructure/Persistence/Identity/IdentityEntities.cs), `ApplicationUser : IdentityUser<Guid>`; [DependencyInjection.cs](../src/Infrastructure/DependencyInjection.cs), `AddIdentityCore` y `AddEntityFrameworkStores`; [AuthService.cs](../src/Infrastructure/Security/AuthService.cs), creación y comprobación de contraseña. `PasswordHash` se hereda de Identity; no hay un hasher casero en el repositorio. La implementación del framework está en su [fuente oficial](https://github.com/dotnet/aspnetcore/blob/v10.0.4/src/Identity/Extensions.Core/src/PasswordHasher.cs).

**Comprobación:** `RegistrationPersistsDifferentHashesForTheSamePasswordAndVerifiesCredentials`, en [AuthenticationIntegrationTests.cs](../tests/Core.Tests/AuthenticationIntegrationTests.cs), registra dos usuarios por HTTP con la misma contraseña de prueba, consulta sus hashes, comprueba que son distintos y verifica contraseña correcta e incorrecta. Esa prueba utiliza EF InMemory. El esquema PostgreSQL contiene `PasswordHash` en `Users`, no una columna de contraseña en claro.

**Consulta de apoyo, sólo lectura y sin mostrar hashes:**

```sql
SELECT "UserName", "PasswordHash" IS NOT NULL AS tiene_hash,
       length("PasswordHash") AS longitud_hash
FROM "Users";
```

Mostrar la longitud no demuestra por sí sola el algoritmo; la evidencia completa es el servicio utilizado, la prueba de sal y el esquema persistido.

### Pregunta 3.3. RBAC, permisos y alcance de finca

**Qué decir:**

> Autenticación identifica al usuario; autorización decide lo que puede hacer. Admin mantiene el catálogo de productos y puede ejecutar eliminaciones sujetas a integridad. Employee consulta y registra operaciones diarias autorizadas, como animales e inventario de su finca. No puede crear, editar o eliminar productos del catálogo.
>
> Los controladores combinan rol administrativo y permiso de la acción. `HasPermission` exige autenticación y consulta la autorización correspondiente. `FarmAccess` limita el alcance de los datos operativos: tener permiso para leer animales no permite leer los de una finca ajena.
>
> Comprobamos la diferencia entre 401 y 403: sin token no hay identidad válida; con token Employee hay identidad, pero una operación administrativa está prohibida. Ninguno de esos rechazos debe escribir en la base.

**Qué abrir:** [ProductsController.cs](../src/Presentation.API/Controllers/ProductsController.cs), `Authorize(Roles = "Admin,Administrador")` en POST/PUT/DELETE; [HasPermissionAttribute.cs](../src/Presentation.API/Authorization/HasPermissionAttribute.cs); [SecurityServices.cs](../src/Infrastructure/Security/SecurityServices.cs), `PermissionChecker`; [FarmAccess.cs](../src/Infrastructure/Security/FarmAccess.cs); roles/permisos en [DatabaseSeeder.cs](../src/Infrastructure/Persistence/DatabaseSeeder.cs).

**Comprobación:** `EmployeeCanOperateAnimalsAndInventoryButCannotMaintainCatalogs`, en [ManagementIntegrationTests.cs](../tests/Core.Tests/ManagementIntegrationTests.cs); [FarmAccessSecurityTests.cs](../tests/Core.Tests/FarmAccessSecurityTests.cs); casos Postman Employee POST/DELETE denegados y finca ajena. Las fotografías también requieren autorización para descargarse: conocer la URL no concede acceso.

**Sobre la diferencia entre consignas:** la asignación inicial permitía a Employee registrar productos; la guía posterior pide POST de productos reservado a Admin. La regla actual sigue esa guía y conserva la operación diaria de Employee en animales e inventario. No confundir catálogo de insumos con ajuste de existencias.

### Pregunta 3.4. DTO, FluentValidation y mass assignment

**Qué decir:**

> Los controladores reciben DTO de entrada y no entidades persistidas completas. `ProductRequest` permite enviar datos comerciales, pero no `Id` ni `CreatedAt`; un JSON con esos campos adicionales no los convierte en valores asignables. La entidad se modifica mediante un mapeo explícito del caso de uso.
>
> Los validadores heredan de `AbstractValidator<T>` en Core.Application. Para productos exigimos precio y costo positivos, precisión adecuada y SKU con letras, números o guiones. Para inventario, stock y mínimo no negativos y máximo mayor que mínimo.
>
> El filtro ejecuta validación asíncrona antes de la acción y el caso de uso vuelve a proteger su contrato. Las comprobaciones de coherencia relacional, como finca de un lote o especie de una raza, se hacen con el repositorio. La base añade índices, claves foráneas y restricciones. Cada mecanismo protege un riesgo diferente.

**Qué abrir:** [Contracts.cs](../src/Core.Application/Management/Contracts.cs), `ProductRequest` e `InventoryRequest`; [RequestValidators.cs](../src/Core.Application/Management/RequestValidators.cs), `ProductRequestValidator` e `InventoryRequestValidator`; [RequestValidationFilter.cs](../src/Presentation.API/Validation/RequestValidationFilter.cs); [ResourceDefinitions.cs](../src/Core.Application/Management/ResourceDefinitions.cs), `ProductDefinition.Apply`.

**Comprobación:** `AdministratorReceivesFieldErrorsForNegativeProductPrice`, `AdministratorCannotCreateAProductWithAnInvalidSku` y `ProductDtoDoesNotBindIdentityOrCreationTimestamp`, en [ManagementIntegrationTests.cs](../tests/Core.Tests/ManagementIntegrationTests.cs). La última verifica alta y edición con JSON adicional sin alterar identidad ni fecha original.

**Demostración:** enviar precio negativo como Admin. Debe retornar **400**, `application/problem+json` y `errors.Price`. Si se envía como Employee, el resultado correcto es **403** porque autorización se ejecuta antes de validar el cuerpo.

## 5. Demostración práctica preparada

### 5.1. Preparar la instalación antes de la clase

La rama principal de este repositorio es **`main`**. Conservar el `.env` existente y comprobar que Admin y Employee tengan contraseñas configuradas. Docker Desktop debe estar iniciado. Desde la raíz, en Git Bash:

```bash
git switch main
git pull --ff-only
docker compose config -q
docker compose build blazorapp
docker compose up -d db
docker compose run --rm blazorapp --seed
docker compose up -d blazorapp nginx
docker compose ps
```

Abrir `http://localhost:18080/swagger` si `NGINX_HTTP_PORT` conserva el valor predeterminado. Un pull o una integración de GitHub no reconstruye contenedores ni aplica migraciones. La API de verificación aislada utilizó **18081**; una instalación anterior en **18080** puede seguir ejecutando una imagen antigua. Seguir [setup](setup.md) para actualizar el ambiente que se mostrará.

Si había una base PostgreSQL 17, el volumen físico no se conecta directamente a PostgreSQL 15. Compose conserva el volumen anterior y utiliza uno distinto para la versión 15. La transferencia de datos existentes requiere exportación/importación. No eliminar volúmenes para improvisar la demostración.

**Antes de empezar:** importar la [colección completa](../postman/Cattle-Management.full.postman_collection.json) y el [entorno](../postman/Daw.postman_environment.json); completar `baseUrl`, usuarios y contraseñas locales. La colección obtiene tokens e identificadores automáticamente al ejecutarse en orden. Usar una base de demostración: hace escrituras y conserva el historial del sacrificio.

### 5.2. Orden de exposición con Postman o Swagger

La colección completa es la referencia reproducible. Para una explicación breve, mostrar los siguientes momentos durante su ejecución; algunos necesitan los identificadores creados por las solicitudes anteriores.

| Momento | Qué ejecutar/mostrar | Qué explicar |
| --- | --- | --- |
| Identidad | Login Admin, Login Employee y perfil | Dos identidades y roles diferentes; token emitido después de verificar contraseña |
| Lectura persistida | GET categorías, productos e inventario | Catálogos sembrados e inventario por finca, con precios y umbrales |
| Autorización | Sin token → 401; Employee POST/DELETE producto → 403 | Autenticación y autorización son decisiones distintas; el producto se conserva |
| Validación | Precio negativo con Admin → 400 | DTO y regla de precio; errores por campo bajo Problem Details |
| Integridad | SKU repetido y categoría con productos → 409 | Índice único y restricción de referencias |
| CRUD | Crear, consultar, editar, volver a consultar y borrar un recurso temporal | La API tiene el ciclo completo; 201/200/204 y confirmación posterior |
| Compatibilidad | POST animal y PATCH `health-status` | Las rutas se mantienen y el cambio de salud deja historial |
| Producción | Carne y piel con el mismo `OperationId` | Dos rendimientos de una operación sobre un animal |
| Coherencia | Animal fallecido; nuevo ordeño y borrar sacrificio → 409 | El historial impide operaciones incompatibles |
| Resiliencia | `/api/demo/errors/unexpected` en Development → 500 | Cabecera RFC, cinco campos y ausencia de detalles internos |

**Qué decir durante el CRUD:**

> Una respuesta de alta por sí sola no demuestra persistencia. Consultamos el ID creado, modificamos un campo y volvemos a leerlo. Después ejecutamos la baja de un registro temporal sin dependencias. Si el recurso tiene dependencias protegidas, la baja se rechaza con 409: es una regla de integridad y no la ausencia del endpoint.

Se implementó POST, GET de lista, GET por ID, PUT por ID y DELETE por ID en **once recursos**: fincas, potreros, lotes, especies, razas, animales, categorías, productos, inventario, pesajes y producción. Las rutas están en [README](../README.md) y los controladores en [Controllers](../src/Presentation.API/Controllers). `/api/herds` es un alias persistente de lotes; la ruta de salud anterior sigue disponible junto con `/health`.

**Comprobación completa:** `EveryManagedResourceSupportsCreateReadUpdateDelete`, en [ManagementIntegrationTests.cs](../tests/Core.Tests/ManagementIntegrationTests.cs), y las solicitudes de alta/lectura/edición/baja de la colección. El historial irreversible se demuestra por separado.

### 5.3. Mostrar la base real

Ejecutar las consultas de sólo lectura del repositorio:

```bash
docker compose exec -T db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"' < db/verification.sql
```

**Qué señalar:** versión PostgreSQL, cuatro migraciones, índice único SKU, columnas monetarias `numeric(18,2)`, relación categoría–producto, dos categorías base, existencias y registros en `AnimalProduction`. [verification.sql](../db/verification.sql) también relaciona usuarios con roles sin imprimir hashes ni tokens.

Las migraciones actuales son:

```text
20261001043752_InitialCreate
20261002004015_AddAnimalPhotosAndUpdatedAt
20261002021355_UnifiedAnimalProductionAndInventory
20261002030136_ManagedInventoryCategorySeed
```

**Qué decir sobre la limpieza del modelo:**

> La unificación se hizo con una migración versionada. Preserva las cantidades, identificadores y metadata convertible de las tablas anteriores antes de retirarlas. No usamos un borrado general de la base para conseguir un esquema limpio. Si un registro antiguo de huevos sólo identifica un lote, la migración se detiene: no inventa a qué animal correspondía. Esa asignación requiere información del negocio.

**Qué abrir:** [ProductionUpgradeData.cs](../src/Infrastructure/Persistence/Migrations/ProductionUpgradeData.cs), [LegacyProduction.sql](../tests/Core.Tests/Fixtures/LegacyProduction.sql) y [VerifyLegacyUpgrade.sql](../tests/Core.Tests/Fixtures/VerifyLegacyUpgrade.sql). El bloqueo seguro se comprueba con [LegacyLotProduction.sql](../tests/Core.Tests/Fixtures/LegacyLotProduction.sql) y [VerifyRejectedUpgrade.sql](../tests/Core.Tests/Fixtures/VerifyRejectedUpgrade.sql).

### 5.4. Qué pruebas hicimos y qué demuestra cada una

**Qué decir:**

> Verificamos en distintos niveles. Las pruebas unitarias comprueban piezas aisladas. Las pruebas de integración .NET ejecutan servicios y pipeline HTTP, usando EF InMemory cuando el fixture sustituye PostgreSQL. La colección Postman ejecuta solicitudes contra una API real conectada a PostgreSQL. Las pruebas SQL comprueban restricciones del motor y los scripts adicionales ejercitan concurrencia y persistencia tras reiniciar la API.
>
> No necesitamos un frontend completo para comprobar estos requisitos del backend. Postman, Swagger y las pruebas automatizadas actúan como clientes. Tampoco usamos una prueba InMemory para afirmar que PostgreSQL aplica una clave foránea o un aislamiento transaccional.

| Evidencia ejecutada | Resultado | Archivo para localizarla o reproducirla |
| --- | --- | --- |
| Suite .NET | **94 aprobadas; 0 fallidas/omitidas** | [Core.Tests](../tests/Core.Tests), [DAW.slnx](../DAW.slnx) |
| Postman completo, PostgreSQL 15.19 | **97 solicitudes; 176 assertions; 0 fallos** | [Colección completa](../postman/Cattle-Management.full.postman_collection.json) |
| Colección utilizada por CI | **16 solicitudes; 16 assertions; 0 fallos** | [Colección de CI](../postman/Cattle-Management.postman_collection.json) |
| Restricciones reales | Índices, FK, importes, defaults y stock comprobados | [PostgresConstraints.sql](../tests/Core.Tests/Fixtures/PostgresConstraints.sql) |
| Migración/siembra/exportación | Cuatro migraciones; siembra repetida; exportaciones importadas en otra base | [schema.sql](../db/schema.sql), [seed-evidence.sql](../db/seed-evidence.sql) |
| Concurrencia | Tres escenarios, una respuesta 201 y una 409 por par; sin cambios parciales | [verify-concurrency.py](../scripts/verify-concurrency.py) |
| Reinicio de API | El valor editado se conserva en la misma base | [verify-persistence.py](../scripts/verify-persistence.py) |

El registro de entorno, procedimiento y resultados está en [evidencias-verificacion.md](evidencias-verificacion.md). Las exportaciones contienen esquema y datos de negocio, sin cuentas, hashes ni tokens: no son un respaldo completo del sistema. Los resultados y conteos corresponden a la versión verificada; pueden variar cuando se incorporen nuevas pruebas.

Para ejecutar la suite sin instalar el SDK local, desde Git Bash:

```bash
MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd -W):/src" -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet test DAW.slnx -c Release
```

**Integración continua:** [ci.yml](../.github/workflows/ci.yml) es el workflow de Andrés y se conservó. Ejecuta pruebas .NET y la colección de 16 solicitudes con Admin sobre PostgreSQL temporal. Su servidor temporal usa PostgreSQL 17; Compose y la evidencia relacional del producto usan PostgreSQL 15. La colección completa de Admin/Employee se ejecutó adicionalmente; no se afirma que sus 97 solicitudes estén dentro del workflow actual.

## 6. Respuestas para preguntas de seguimiento

### «¿Por qué usar un repositorio si EF ya ofrece DbContext?»

> DbContext pertenece al detalle de persistencia. El puerto de aplicación mantiene el caso de uso independiente de EF y delimita lecturas, escrituras y transacción. Nuestro repositorio aporta además seguimiento selectivo, traducción de conflictos y auditoría de las operaciones que pasan por él. No elimina la necesidad de entender EF ni obliga a implementar un repositorio distinto por cada entidad.

### «¿Qué ocurre si dos peticiones sacrifican el mismo animal?»

> La transacción serializable incluye la lectura de las reglas y la escritura. Se probó sobre PostgreSQL real: una solicitud se confirma y la otra recibe 409, sin dejar cambios parciales. También hay una restricción única sobre operación y tipo de producto. El servidor traduce conflictos de serialización; no reintenta automáticamente una operación irreversible.

**Archivo:** [ManagementRepository.cs](../src/Infrastructure/Persistence/ManagementRepository.cs), `ExecuteWriteAsync`; [script de concurrencia](../scripts/verify-concurrency.py).

### «¿Qué tiene de ACID esta operación?»

> Atomicidad: los cambios del caso de uso se confirman juntos. Consistencia: validadores y restricciones protegen las reglas. Aislamiento: las escrituras del CRUD usan Serializable. Durabilidad: los cambios confirmados quedan en PostgreSQL; se comprobó reiniciar la API conservando la base. En Compose el almacenamiento usa un volumen persistente.

La prueba de reinicio mantuvo el contenedor de base de datos. No demuestra supervivencia de una base temporal después de eliminar su contenedor ni sustituye una política de respaldo.

### «¿El JWT sirve como contraseña cifrada?»

> No. La contraseña se verifica con Identity y no está en el token. El JWT contiene claims legibles y una firma para proteger su integridad. PBKDF2 protege el almacenamiento de la contraseña; HMAC-SHA256 firma el JWT. Son mecanismos y objetivos distintos.

### «¿Puede el usuario registrarse como Admin enviando un campo extra?»

> El DTO de registro no ofrece asignación de rol ni superusuario. AuthService construye explícitamente los campos permitidos. El registro no concede permisos administrativos; la autorización se verifica por separado.

**Archivo:** [AuthContracts.cs](../src/Core.Application/Security/AuthContracts.cs), `RegisterRequest`, y [AuthService.cs](../src/Infrastructure/Security/AuthService.cs), `RegisterAsync`.

### «¿Por qué stock y umbrales no están dentro de Product?»

> Product describe el artículo compartido. FarmInventory describe cuánto tiene una finca y sus límites locales. Poner un único stock global en Product impediría representar dos fincas con cantidades y ubicaciones distintas. La clave única finca–producto evita duplicar esa existencia.

### «¿Hay CRUD completo si un DELETE devuelve 409?»

> Sí: la operación y su autorización existen. Se probó borrar registros temporales sin dependencias. Los registros con dependencias o hechos irreversibles se protegen con reglas explícitas; disponer de DELETE no obliga a destruir cualquier historial. Para un sacrificio permitimos corregir el rendimiento, manteniendo el hecho registrado.

### «¿Todo lo que aparece en el modelo ya puede operarse?»

> El núcleo de once recursos, cambios de salud, producción y seguridad tiene operaciones verificadas. Las entidades de reproducción, eventos clínicos, alertas y finanzas anticipan futuras capacidades; sus procesos completos no se presentan como implementados. La página Blazor actual presenta la API; los formularios de gestión y los informes completos son evolución posterior.

### «¿Una sal aleatoria elimina todos los ataques a contraseñas?»

> Evita que contraseñas iguales compartan el mismo hash y dificulta reutilizar cálculos precalculados. La derivación aumenta el costo de cada intento. No garantiza que una contraseña débil sea imposible de adivinar. Utilizamos el mecanismo del framework y una política de contraseñas, sin inventar criptografía propia.

## 7. Cierre sugerido

> Estas tres fases dejan una API con separación de responsabilidades, errores uniformes, datos persistidos con reglas relacionales y acceso controlado por identidad, rol, permiso y finca. La producción de los animales está unificada y conserva trazabilidad; el inventario de insumos representa existencias por finca.
>
> Cada punto de la guía tiene una implementación localizable y una forma de comprobarlo. Además de leer el código, ejecutamos pruebas .NET, solicitudes HTTP, migraciones y restricciones de PostgreSQL. El siguiente incremento puede construir los flujos de interfaz sobre estos contratos y ampliar los procesos del negocio con sus propias pruebas.

## 8. Mapa rápido de los doce criterios

| Pregunta | Punto que debes recordar | Primera evidencia que abrir |
| --- | --- | --- |
| 1.1 | Dominio sin frameworks; puertos hacia el núcleo | `Core.Domain.csproj` y `IManagementRepository` en `Contracts.cs` |
| 1.2 | Scoped datos/negocio; transient validadores; singleton utilidades | `DependencyInjection.cs`, `Program.cs` y prueba de ciclos |
| 1.3 | `InvokeAsync` envuelve `next`; middleware en pipeline | `ExceptionMiddleware.cs` y `Program.cs` |
| 1.4 | Cabecera problem+json y cinco campos | Prueba de pipeline y respuesta HTTP en Network |
| 2.1 | Mapeos externos con `IEntityTypeConfiguration<T>` | `CatalogConfigurations.cs`, `AppDbContext.OnModelCreating` |
| 2.2 | Dinero `decimal`/`numeric(18,2)` y categoría Restrict | Configuración, SQL real y prueba de borrado protegido |
| 2.3 | SKU único en aplicación y base | Configuración, migración e índice real |
| 2.4 | Lecturas sin seguimiento; categorías con HasData | `ManagementRepository.cs`, `ProductionConfigurations.cs`, migración 4 |
| 3.1 | JWT firmado con sub/email/roles y validación Bearer | `JwtTokenService.cs`, `AuthService.cs`, `Program.cs` |
| 3.2 | Identity PBKDF2 con sal; PasswordHash | Servicio de autenticación y prueba de hashes distintos |
| 3.3 | Admin mantiene catálogo; Employee opera su finca | `ProductsController.cs`, prueba Employee y Postman |
| 3.4 | DTO limitados y FluentValidation | `Contracts.cs`, `RequestValidators.cs`, pruebas de entradas |

**Para estudiar:** en cada fila poder responder cuatro cosas con tus propias palabras: **qué implementamos, qué concepto aplica, qué problema del producto evita y qué evidencia lo demuestra**.
