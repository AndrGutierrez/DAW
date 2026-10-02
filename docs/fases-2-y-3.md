# Persistencia, seguridad y trazabilidad de las fases 2 y 3

## 1. Finalidad y forma de leer este documento

Gestión Ganadera ofrece un backend para administrar fincas, animales, insumos, existencias, pesajes y rendimientos productivos. Este documento explica cómo esas capacidades se relacionan con la teoría de persistencia y seguridad, y registra su correspondencia con los criterios de las fases 2 y 3 de Desarrollo de Aplicaciones Web.

La descripción funcional detallada está en [modelo de producción y CRUD](modelo-produccion-y-crud.md). La preparación del ambiente está en [setup](setup.md). Aquí se separan tres clases de evidencia:

1. **Implementación:** archivos y mecanismos presentes en el código.
2. **Configuración declarada:** esquema de EF Core, migraciones y datos previstos por el seeder.
3. **Ejecución:** resultados obtenidos sobre una instancia identificada de PostgreSQL y la API.

Un mapeo correcto en C# no demuestra que la migración esté aplicada en una base de datos concreta. Una prueba que sustituye PostgreSQL por EF InMemory tampoco demuestra claves foráneas, precisión SQL o aislamiento transaccional. La sección de resultados mantiene separados esos alcances.

## 2. Fuentes y adaptación al dominio ganadero

### 2.1. Material utilizado

| Fuente de la asignatura | Información utilizada |
| --- | --- |
| `Fase 2/03_Tarea_Asignacion_Fase2_y_Rubrica.docx` | PostgreSQL 15, EF Core 10, Fluent API, migraciones, siembra mínima, repositorio y rúbrica de 80 puntos |
| `Fase 2/01_Material_Lectura_Teoria_Fase2.docx` | Fundamentos de modelado relacional, normalización, transacciones, integridad y lectura optimizada |
| `Fase 2/DAW-0423807T Fase 2.pdf` | Material de exposición de persistencia y evolución del esquema |
| `Fase 3/03_Tarea_Asignacion_Fase3_y_Rubrica.docx` | JWT, RBAC, FluentValidation, escenarios Postman y rúbrica de 80 puntos |
| `Fase 3/DAW-0423807T Fase 3.pdf` | Material de exposición del subsistema de seguridad |
| Feedback del 27 de septiembre de 2026 | Verificación de pipeline, ciclos DI, índices de usuario/producto, precisión monetaria, categorías, siembra y lecturas sin seguimiento |

Las consignas emplean una ferretería como ejemplo de categorías, precios, costos y existencias. La adaptación mantiene esos requisitos técnicos dentro de una explotación ganadera: los productos son insumos agropecuarios, las categorías corresponden a alimentación/sanidad y el stock se administra por finca.

No se introduce un segundo producto de ferretería ni un módulo de gestión de IT. La finalidad de negocio del repositorio es la gestión ganadera.

### 2.2. Correspondencia de conceptos

| Concepto del ejemplo académico | Implementación del producto ganadero | Motivo |
| --- | --- | --- |
| Categoría de producto | `InventoryCategory` | Catálogo relacional de insumos |
| Producto con SKU | `Product` | Identificación única del artículo utilizado por la explotación |
| Precio y costo | `Product.Price`, `Product.CostPrice` | Importes monetarios explícitos con dos decimales |
| Stock, mínimo, máximo y ubicación | `FarmInventory` | Esos datos dependen de finca y producto, no del artículo global |
| Unidad de medida | `Product.Unit` / `MeasurementUnit` | Unidad del insumo conservada en el catálogo |
| Marca y estado activo | `Product.Brand`, `Product.IsActive` | Descripción y disponibilidad administrativa |
| Usuario con roles Admin/Employee | `ApplicationUser`, `Role` y relaciones de Identity | Identidad persistida, contraseña gestionada por Identity y autorización por rol |
| Registros propios del sector | `Animal`, `WeightRecord`, `AnimalProduction` | Identificación individual, pesajes y rendimientos ganaderos |

`ApplicationUser` se encuentra en el adaptador de identidad de `Infrastructure`, donde puede heredar de `IdentityUser<Guid>`. Los contratos utilizados por la aplicación están en `Core.Application.Security`. Esta disposición evita introducir dependencias de ASP.NET Core Identity en las entidades puras del dominio.

## 3. Fase 2: persistencia y siembra

### 3.1. Rúbrica y evidencias de implementación

La fase 2 tiene un máximo de **80 puntos**, distribuidos en cuatro criterios. La tabla sirve para ubicar las evidencias; no adjudica una calificación ni sustituye la ejecución.

| Criterio | Puntos | Implementación verificable | Evidencia de ejecución necesaria |
| --- | ---: | --- | --- |
| Fluent API y reglas de persistencia | 30 | Configuraciones `IEntityTypeConfiguration<T>`, UUID, nombres de tabla, límites, `numeric(18,2)`, índices únicos y relaciones restrictivas del núcleo operativo | Consultar columnas, índices, `CHECK` y claves foráneas en PostgreSQL; demostrar rechazo de duplicados y borrados incompatibles |
| DbContext y migraciones versionadas | 20 | `AppDbContext`, carga mediante `ApplyConfigurationsFromAssembly` y archivos de migración/snapshot | Aplicar el historial a PostgreSQL 15 y comparar esquema real con la configuración actual |
| Siembra representativa | 15 | `DatabaseSeeder`: dos categorías de insumos, cuatro productos con SKU/importes y cuatro existencias de la finca demo | Consultar precios, costos, unidades, stock, umbrales y referencias de los registros sembrados |
| Repositorio y `AsNoTracking` | 15 | Puerto `IManagementRepository`, adaptador EF `ManagementRepository` y lecturas especializadas | Verificar lecturas y ausencia de seguimiento innecesario; demostrar que GET recupera datos persistidos |
| **Total** | **80** | | |

### 3.2. Configuraciones independientes y Onion Architecture

Las entidades de `Core.Domain` describen el negocio. Las decisiones de tabla, clave, índice y tipo SQL se encuentran en `Infrastructure/Persistence/Configurations`.

Cada clase implementa `IEntityTypeConfiguration<T>`. Varias clases pueden compartir un archivo temático sin perder su separación como configuraciones. Por ejemplo, `ProductConfiguration` y `SpeciesConfiguration` son clases distintas dentro de `CatalogConfigurations.cs`.

`AppDbContext.OnModelCreating` registra los mapeos con:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

La aplicación conoce el puerto `IManagementRepository`; su implementación conoce EF Core. El contrato permite expresar filtros y guardar cambios sin que `CrudService` dependa directamente de `DbContext` o Npgsql.

**Archivos:** [AppDbContext.cs](../src/Infrastructure/Persistence/AppDbContext.cs), [configuraciones de catálogos](../src/Infrastructure/Persistence/Configurations/Livestock/CatalogConfigurations.cs), [configuraciones de producción/inventario](../src/Infrastructure/Persistence/Configurations/Livestock/ProductionConfigurations.cs), [contratos del repositorio](../src/Core.Application/Management/Contracts.cs).

### 3.3. Precisión decimal: dinero y magnitudes físicas

`Product.Price` y `Product.CostPrice` utilizan `HasPrecision(18, 2)`. Los montos de transacciones, costos clínicos, costos de alimentación y costos unitarios de lotes de insumos también tienen configuración monetaria de 18,2, aunque sus módulos completos todavía no tengan CRUD público.

Las cantidades físicas pueden necesitar otra escala: `AnimalProduction.Quantity` y las existencias usan 14,4; los pesajes y la condición corporal tienen su propia precisión. Aplicar 18,2 a todos los decimales reduciría innecesariamente la exactitud de algunas mediciones. El criterio financiero se aplica a los importes.

La declaración SQL y FluentValidation trabajan en conjunto: la base almacena el tipo definido y los validadores rechazan entradas que excedan la escala o las condiciones aceptadas.

### 3.4. Índices y normalización de identidad

| Dato | Restricción | Finalidad |
| --- | --- | --- |
| `Product.SKU` | Índice único | Evitar duplicados del artículo |
| `ApplicationUser.NormalizedUserName` | Índice único de Identity | Evitar cuentas duplicadas por nombre normalizado |
| `ApplicationUser.NormalizedEmail` | `EmailIndex` único | Evitar cuentas duplicadas por correo normalizado |
| `InventoryCategory.Name` | Índice único | Evitar categorías duplicadas |
| `(FarmId, ProductId)` | Índice único en `FarmInventory` | Una existencia actual por producto y finca |
| `(OperationId, ProductType)` | Índice único en `AnimalProduction` | Un rendimiento de cada tipo dentro de una operación |
| `(FarmId, InternalTag)` y referencias oficiales/RFID | Índices únicos en `Animal` | Identificaciones únicas dentro de una finca |

Identity normaliza los identificadores antes de persistirlos. El índice sobre el valor normalizado garantiza la unicidad de la identidad; un índice sobre el texto original por sí solo podría permitir variaciones de mayúsculas/minúsculas.

La consigna utiliza la expresión «índices únicos O(1)». El mecanismo implementado es un índice único relacional. En PostgreSQL los índices creados de esta manera utilizan B-tree; no se afirma que ofrezcan una búsqueda constante O(1). Lo demostrable es la unicidad y la disponibilidad de un índice para las consultas. Véase la [documentación de tipos de índice](https://www.postgresql.org/docs/15/indexes-types.html) y [CREATE INDEX de PostgreSQL 15](https://www.postgresql.org/docs/15/sql-createindex.html).

### 3.5. Integridad referencial y política de borrado

`ProductConfiguration` define la relación 1:N categoría–producto con `DeleteBehavior.Restrict`. `FarmInventory` también restringe la eliminación de la finca o producto referenciado. `AnimalProduction` restringe la eliminación del animal o finca del historial productivo.

Las relaciones principales de animales, lotes y potreros hacia sus fincas son restrictivas; un borrado administrativo no debe eliminar la explotación completa y sus datos de manera accidental.

La política no es universal para todas las tablas: algunas asociaciones de identidad y entidades auxiliares/futuras conservan cascadas explícitas. Por ejemplo, `UserFarm` mantiene reglas de asociación y las fotografías tienen una relación de dependencia con el animal. El caso de uso exige retirar las fotografías antes de eliminar el animal, para que el borrado del archivo físico sea explícito.

Para evaluar una relación concreta deben revisarse su clase de configuración, la migración y la clave foránea real en PostgreSQL. Esta documentación no presenta todas las relaciones auxiliares como `Restrict`.

### 3.6. Valores por defecto y coherencia del inventario

En `FarmInventoryConfiguration` existen valores iniciales para stock, mínimo, máximo y ubicación: `0`, `5`, `100` y `Main warehouse`. `ProductConfiguration` define la marca inicial `Generic` y unidad `Unit`.

El enum de unidades tiene `Kilogram` con valor cero. La configuración de la unidad utiliza un valor centinela para que EF Core pueda diferenciar ese valor válido del mecanismo de valor por defecto. Esto evita que un producto expresado explícitamente en kilogramos se guarde como `Unit`.

`FarmInventory.MinStock` también utiliza un centinela `-1`: un mínimo explícito de cero no debe reemplazarse por el default de cinco. Son decisiones de mapeo necesarias para que el valor recibido por la API coincida con el valor persistido.

Un `CHECK` de inventario exige stock/mínimo no negativos y máximo mayor que mínimo. Su existencia protege también escrituras que no pasen por el controlador. FluentValidation ofrece el rechazo temprano y el detalle por campo para el cliente de la API.

### 3.7. Siembra representativa

El seeder declara los siguientes insumos y sus existencias de demostración. La tabla describe los datos previstos por el código; su presencia real debe confirmarse después de ejecutar la siembra en la base seleccionada.

| SKU | Producto | Categoría | Precio | Costo | Stock | Mínimo | Máximo | Unidad |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| `ALI-001` | Concentrado bovino | Alimentación animal | 24,50 | 18,00 | 40 | 10 | 100 | `Bag` |
| `ALI-002` | Sal mineral | Alimentación animal | 16,00 | 12,00 | 25 | 5 | 80 | `Bag` |
| `SAN-001` | Vacuna bovina | Sanidad animal | 8,75 | 6,50 | 60 | 15 | 150 | `Dose` |
| `SAN-002` | Desparasitante | Sanidad animal | 22,00 | 15,00 | 12 | 3 | 30 | `Liter` |

Además se declaran especies/razas, la finca `DEMO`, potreros, lotes, animales y pesajes. La producción de ejemplo incluye leche, lana, carne y piel; las dos últimas comparten una operación de sacrificio. El animal de esa operación queda con estado `Dead`.

La siembra crea los roles y permisos, y utiliza Identity para crear el usuario administrador con contraseña configurada. El usuario Employee de demostración se crea únicamente cuando se proporciona `Seed:EmployeePassword`; sin esa configuración, no existe una garantía de disponer de esa cuenta para demostrar el escenario Employee.

La operación utiliza comprobaciones de existencia para evitar duplicar los datos previstos al ejecutarla otra vez sobre un ambiente coherente. No es un mecanismo general de reparación de cualquier modificación manual de los datos de demostración.

El seeder es explícito: el proceso `--seed` aplica migraciones y siembra. Arrancar normalmente la API no debe confundirse con haber ejecutado ese proceso.

**Archivo:** [DatabaseSeeder.cs](../src/Infrastructure/Persistence/DatabaseSeeder.cs).

### 3.8. PostgreSQL 15 y migraciones

`docker-compose.yml` utiliza `postgres:15-alpine`. El volumen de datos se llama `daw-postgres15-data`, separado del almacenamiento de versiones anteriores. Cambiar la imagen de PostgreSQL no convierte automáticamente el formato de un directorio de datos de otra versión mayor.

Esta separación permite preparar la base de la versión actual sin reutilizar directamente un directorio de otro motor mayor. El volumen anterior no se transfiere automáticamente; si contiene información que deba conservarse, su migración de datos requiere un procedimiento de exportación/importación revisado.

EF Core conserva el historial versionado en `Infrastructure/Persistence/Migrations`. La evolución incorpora el catálogo de categorías, atributos financieros de productos, existencias por finca y producción unificada. El snapshot representa el modelo esperado al terminar el historial.

La migración `UnifiedAnimalProductionAndInventory` utiliza [ProductionUpgradeData.cs](../src/Infrastructure/Persistence/Migrations/ProductionUpgradeData.cs) para preservar datos de las tablas productivas anteriores durante la transformación. Conserva identificadores, fechas, cantidades medidas y metadatos disponibles de leche, lana y carne. Los metadatos especializados se incorporan a `Notes` como JSON de origen. Los animales con sacrificio migrado quedan en estado `Dead`.

Los registros antiguos de huevos pertenecían a un lote y no identificaban un animal. La migración se detiene si existen: asignarles un animal automáticamente inventaría trazabilidad. También se detiene ante rendimientos no positivos/sin cantidad medida, múltiples sacrificios del mismo animal o pertenencias de finca inconsistentes. Un fallo dentro de la transacción impide una transformación parcial.

Los insumos heredados reciben un SKU identificable y categoría migrada. Se conservan inactivos cuando carecen de los nuevos datos financieros, en lugar de inventar precios y costos. Las URLs de fotografías antiguas del prefijo predeterminado se convierten al endpoint privado y se conserva el nombre real almacenado.

La reversión automática `Down` está bloqueada: esta transformación no reconstruye de manera fiel todas las tablas y reglas anteriores. Volver al esquema previo requiere restaurar una copia verificada. El procedimiento de actualización y respaldo se explica en [setup](setup.md).

La ejecución se comprueba mediante `__EFMigrationsHistory` y consultas al catálogo SQL. El archivo `db/schema.sql` sirve como evidencia exportable cuando esté actualizado con esa evolución. No basta con que el contenedor de PostgreSQL esté encendido: debe tener el historial aplicado y los datos sembrados.

### 3.9. Repositorio, lectura y seguimiento

`ManagementRepository.ListAsync`, `GetAsync` sin seguimiento y `ExistsAsync` utilizan `AsNoTracking`. `AnimalQueryService`, `AnimalWeightReader`, `FarmAccess` y consultas de permisos también utilizan lecturas sin seguimiento donde corresponde.

Para editar una entidad, el caso de uso solicita `GetAsync(..., tracking: true)`. EF Core detecta los cambios y genera la escritura. Utilizar `AsNoTracking` indiscriminadamente en una operación de edición impediría confiar en ese seguimiento sin adjuntar la entidad explícitamente.

El repositorio no guarda una lista de animales en memoria como fuente principal. Los datos recuperados por la API provienen de PostgreSQL cuando se ejecuta el registro DI normal. EF InMemory se utiliza únicamente en fixtures de prueba que sustituyen el proveedor.

### 3.10. ACID, aislamiento y auditoría

| Propiedad | Aplicación en el backend | Alcance de la demostración |
| --- | --- | --- |
| Atomicidad | Rendimiento, cambio de estado y auditoría se guardan en una transacción del repositorio | Comprobar que una solicitud rechazada no deja cambios parciales |
| Consistencia | Validadores, referencias, índices y `CHECK` protegen invariantes | Probar duplicados, referencias inválidas e importes/stock incompatibles |
| Aislamiento | CRUD y cambios de salud abren transacciones `Serializable` | Probar solicitudes concurrentes contra PostgreSQL; InMemory no reproduce esta propiedad |
| Durabilidad | PostgreSQL conserva los cambios confirmados en el volumen persistente | Reiniciar la API y repetir GET sin reconstruir ni eliminar la base |

`IManagementRepository.ExecuteWriteAsync` envuelve el caso de uso completo. La implementación abre una transacción serializable antes de consultar las reglas, ejecuta la operación y confirma al finalizar. `SaveAsync` guarda las entidades y la auditoría dentro de esa transacción, sin confirmar por separado.

La envoltura examina la cadena de excepciones para reconocer fallas PostgreSQL de serialización (`40001`) o interbloqueo (`40P01`), aunque EF Core las haya encapsulado. Se presentan como `409 Conflict`, tanto en comprobaciones previas como al guardar/confirmar, indicando al cliente que actualice y reintente. No se hace un reintento automático oculto. La creación física de la transacción es un detalle privado de Infrastructure.

`AuditLog.OldValues` y `NewValues` utilizan `jsonb`. La implementación mantiene UUID como identificador e IP como texto y no captura automáticamente la IP en todas las operaciones. El ejemplo teórico con `BIGINT`/`inet` no se presenta como una implementación idéntica. Tampoco existe todavía una API completa de consulta de auditoría ni una auditoría integral de login, siembra, permisos y archivos.

Los cambios de salud conservan el usuario autenticado en `HealthStatusChange.UserId`; los pesajes nuevos registran `RecordedByUserId` y las ediciones conservan la autoría inicial. Crear/editar pesajes y producción actualiza la fecha de información del animal.

La coherencia temporal se comprueba en ambas direcciones: se rechaza un sacrificio anterior a un pesaje existente y se rechaza un peso nuevo fechado en el día de un sacrificio ya registrado o después. Un peso incorporado antes del sacrificio en el mismo día permanece legítimo; puede corregirse manteniendo su fecha original cuando su `CreatedAt` es anterior al del sacrificio. Esta distinción utiliza tanto la fecha del hecho como el orden de registro, sin inventar una hora de medición que el DTO no contiene.

La integridad del parentesco también se protege durante ediciones: un animal utilizado como padre o madre conserva especie, sexo y nacimiento. Así, modificar un progenitor no invalida las referencias ya registradas por sus crías.

## 4. Fase 3: autenticación, autorización y validación

### 4.1. Rúbrica y evidencias

La fase 3 tiene un máximo independiente de **80 puntos**.

| Criterio | Puntos | Implementación verificable | Evidencia requerida |
| --- | ---: | --- | --- |
| JWT y claims | 25 | `AuthService`, `JwtTokenService`, emisión HMAC-SHA256 y validación de emisor/audiencia/firma/expiración | Login Admin/Employee, perfil y rechazo de token inválido/expirado |
| Matriz RBAC | 25 | Roles Admin/Employee, `Authorize(Roles=...)`, permisos por acción y acceso por finca | Sin token → 401; Employee eliminando producto → 403; mantenimiento de categorías restringido |
| FluentValidation | 20 | Validadores en `Core.Application`, registro transient y filtro asíncrono | Producto con precio negativo → 400 con errores por campo; validaciones de stock y texto |
| Evidencias Postman | 10 | Colección JSON y ambiente configurables | Ejecutar y exportar los cuatro escenarios obligatorios; conservar reporte identificable |
| **Total** | **80** | | |

### 4.2. Login por nombre o correo

`POST /api/auth/login` recibe `username` y `password`. El campo `username` acepta el nombre de usuario o el correo; `AuthService` consulta Identity por nombre y, si no lo encuentra, por correo. Una contraseña incorrecta no genera un token y una cuenta inactiva no puede iniciar sesión.

La respuesta contiene `accessToken`, `refreshToken`, `accessTokenExpiresAt` y `user`. El usuario de la respuesta incluye identificador, nombre, correo, nombre completo, indicador de superusuario, lista de roles y lista de permisos.

Este contrato admite varios roles. El campo de correo está en la representación del usuario de la respuesta; no se afirma que el JWT tenga un claim de correo que el emisor actual no genera.

### 4.3. Contraseñas y firma son mecanismos distintos

La contraseña se crea y verifica mediante `UserManager.CreateAsync` y `CheckPasswordAsync`. ASP.NET Core Identity utiliza un hash de contraseña PBKDF2 con sal y coste de derivación; no se almacena texto plano ni se implementa una comparación artesanal de SHA-256. El formato de Identity conserva la información necesaria para verificar el hash. Esta elección aplica una derivación específica de contraseñas y cubre el requisito de algoritmo seguro. Su mecanismo puede verificarse en el [PasswordHasher oficial de ASP.NET Core 10](https://github.com/dotnet/aspnetcore/blob/v10.0.4/src/Identity/Extensions.Core/src/PasswordHasher.cs).

La firma del token usa `HmacSha256` y una clave secreta del servidor. Firmar un JWT protege su integridad; no cifra los claims. Por eso el token no contiene la contraseña.

### 4.4. Claims y verificación del token

`JwtTokenService` emite identificador (`sub` y NameIdentifier), nombre de usuario, roles y, cuando corresponde, indicador de superusuario. El token incluye emisor, audiencia y expiración.

`Program.cs` configura `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime` y `ValidateIssuerSigningKey`. El margen de reloj se establece en cero. Los tiempos de vida se obtienen de `JwtOptions`; los valores base del producto son 30 minutos para acceso y 7 días para refresh, y pueden configurarse por ambiente.

El subsistema no mantiene una sesión de navegador en el servidor para reconocer cada petición. El token firmado identifica al cliente; las consultas de permisos y finca vuelven a comprobar el estado activo y la autorización vigente en PostgreSQL.

**Archivos:** [AuthService.cs](../src/Infrastructure/Security/AuthService.cs), [JwtTokenService.cs](../src/Infrastructure/Security/JwtTokenService.cs), [Program.cs](../src/Presentation.API/Program.cs).

### 4.5. Refresh y cuentas inactivas

`POST /api/auth/refresh` busca un token de renovación activo y no expirado, comprueba que su usuario siga activo, revoca el token utilizado y genera uno nuevo. La revocación y el nuevo registro se guardan conjuntamente. La reutilización secuencial del token anterior se rechaza.

`GET /api/auth/me` devuelve el perfil de la identidad autenticada. Una cuenta inactiva no puede consultar ese perfil ni renovar el token. El comprobador de permisos también deniega acciones a usuarios inactivos y `FarmAccess` elimina su alcance de fincas.

La rotación de refresh no se presenta como revocación criptográfica inmediata de todos los JWT ya emitidos. Los permisos y acceso a datos se comprueban además en la base de datos.

### 4.6. RBAC y permisos por acción

El catálogo define acciones `list`, `get`, `create`, `update` y `delete` para los recursos administrados, y `roles.manage` para asignar/revocar permisos de roles. Contener un nombre en el catálogo no equivale a tener un endpoint CRUD implementado para cada recurso de seguridad.

| Capacidad | Admin / Administrador | Employee | SoloLectura |
| --- | --- | --- | --- |
| Consultar catálogos permitidos | Sí | Sí | Sí |
| Consultar datos de finca | Todas las fincas existentes | Fincas asignadas | Fincas asignadas |
| Registrar/editar productos | Sí | Sí | No |
| Registrar operaciones diarias autorizadas | Sí | Sí | No |
| Crear/editar categorías | Sí | No | No |
| Mantener fincas, especies y razas | Sí | No | No |
| Eliminar recursos | Sí, sujeto a reglas de integridad | No | No |
| Cargar fotografías | Sí | Sí, en finca asignada | No |
| Descargar fotografías | Sí | Sí, en finca asignada | Sí, en finca asignada |
| Administrar permisos de roles | Sí, con `roles.manage` | No | No |

Los roles españoles especializados se mantienen junto a `Admin` y `Employee`. Sus permisos se declaran en el seeder. El rol `SoloLectura` obtiene únicamente lectura; modificar un archivo no se considera una operación de consulta.

Todos los `DELETE` de los recursos operativos y fotografías requieren rol `Admin` o `Administrador`. Crear/modificar categorías, especies, razas y fincas también tiene guarda de rol administrativa. `HasPermission` añade el control de la acción concreta.

Una identidad que sólo tenga el permiso `roles.manage` sin rol administrativo no puede otorgar permisos a un rol. Las reglas de negocio y las claves foráneas siguen aplicándose después de superar RBAC: el rol no habilita destruir un sacrificio o una referencia protegida.

### 4.7. Acceso por finca y descargas

`IFarmAccess` separa la pregunta «¿puede realizar esta acción?» de «¿puede operar sobre esta finca?». `FarmAccess` identifica al usuario desde el contexto HTTP, comprueba la cuenta activa y consulta sus asignaciones/roles persistidos.

Las listas se filtran con las fincas accesibles. Las lecturas de un identificador fuera del alcance visible devuelven 404 para evitar divulgar un registro de otra explotación. Una creación dirigida explícitamente a una finca no autorizada puede devolver 403.

Los archivos de animales se descargan mediante `/api/animals/{id}/photos/{photoId}/content`, con JWT, `photos.get` y la misma comprobación de finca. La carpeta de almacenamiento no se sirve públicamente con `UseStaticFiles`. Conocer la URL no evita la autorización.

### 4.8. Validación desacoplada y ciclos de vida

Los validadores se encuentran en `Core.Application.Management` y `Core.Application.Security`. Incluyen DTOs de creación/edición, cambios de salud, registro, login y refresh.

`RequestValidationFilter` resuelve `IValidator<T>` y ejecuta `ValidateAsync` antes de la acción. Las guardas de autorización se ejecutan antes de ese filtro; una operación prohibida no se autoriza porque su cuerpo sea válido. Los casos de uso CRUD también validan su contrato, manteniendo protección si se invocan fuera del controlador.

| Dependencia | Ciclo de vida | Razón |
| --- | --- | --- |
| `AppDbContext` | Scoped | Unidad de trabajo y rastreador por solicitud |
| Servicios CRUD, definiciones, repositorio, autenticación, permisos y acceso por finca | Scoped | Comparten dependencias del alcance HTTP y contexto de datos |
| `IValidator<TRequest>` | Transient | Instancia corta para validar cada entrada; registro explícito de FluentValidation |
| Servicio de tokens y almacenamiento local | Singleton | Configuración del proceso y operaciones sin estado de usuario/contexto cautivo |
| `IValidateOptions<JwtOptions>` | Singleton | Valida configuración de arranque, sin depender de DbContext ni solicitud |

El validador de opciones JWT no es un validador de DTO de negocio. Su registro singleton forma parte del sistema de opciones y no captura servicios scoped. Los validadores de entrada sí se registran con `ServiceLifetime.Transient`.

### 4.9. Errores detallados bajo Problem Details

`ExceptionMiddleware` está registrado antes de los componentes que pueden producir excepciones de solicitud. Convierte validación, conflicto, ausencia de datos y fallos inesperados en respuestas `application/problem+json`.

La estructura conserva `type`, `title`, `status`, `detail` e `instance`. Una `ValidationException` añade la extensión `errors`, agrupada por campo. La respuesta de model binding inválido se configura con `ValidationProblemDetails`; las respuestas de autorización y códigos HTTP vacíos de `/api` se completan mediante `UseStatusCodePages`.

Este contrato permite que el cliente muestre los errores sin interpretar trazas de excepciones. El formato se basa en [RFC 7807, Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc7807). Los errores inesperados devuelven un mensaje público genérico y se registran internamente.

Ejemplo ilustrativo de validación, sin sustituir una captura de ejecución:

```json
{
  "type": "about:blank",
  "title": "Bad Request",
  "status": 400,
  "detail": "Validation failed...",
  "instance": "/api/products",
  "errors": {
    "Price": ["'Price' must be greater than '0'."]
  }
}
```

El texto preciso puede variar con la regla; lo estable es el código, tipo de contenido, campos estándar y errores por propiedad.

### 4.10. Configuración de secretos

`appsettings.json` conserva emisor, audiencia y tiempos; la clave JWT y contraseñas de siembra se suministran por ambiente o secretos de desarrollo. Docker Compose toma esos valores de la configuración local.

`JwtOptionsValidator` rechaza una clave ausente, el marcador `change-me` y claves menores de 32 caracteres. La aplicación valida esas opciones al arrancar. Una configuración vacía no debe permitir emitir tokens con una clave conocida o implícita.

Los ejemplos versionados describen nombres de variables. Los reportes, capturas y colección exportada deben omitir credenciales y tokens reales.

## 5. Demostración y evidencias reproducibles

### 5.1. Los cuatro escenarios de Fase 3

| Escenario | Procedimiento | Resultado que debe conservarse |
| --- | --- | --- |
| Login Admin y Employee | Ejecutar login con cada cuenta sembrada y consultar `/api/auth/me` | 200, token emitido y roles correspondientes |
| Endpoint protegido sin token | Consultar un catálogo protegido omitiendo Bearer | 401 y respuesta Problem Details |
| Employee elimina producto | Ejecutar `DELETE /api/products/{id}` con token Employee | 403; verificar posteriormente que el producto siga existiendo |
| Precio negativo | Enviar un producto con `price < 0` mediante una cuenta autorizada para crear | 400 con error de `Price`; verificar que no se haya creado el SKU |

La colección está en [Cattle-Management.full.postman_collection.json](../postman/Cattle-Management.full.postman_collection.json) y el ambiente en [Daw.postman_environment.json](../postman/Daw.postman_environment.json). La evidencia de cumplimiento exige ejecutar los escenarios, no sólo tener solicitudes guardadas.

### 5.2. Evidencias de Fase 2

Debe conservarse un reporte o exportación SQL que permita comprobar:

1. PostgreSQL 15 y migraciones aplicadas.
2. Tablas del núcleo, tipos UUID y precisión `numeric(18,2)` de los importes.
3. Índices únicos de SKU, identidad normalizada y pares de inventario/producción.
4. Restricción categoría–producto y relaciones restrictivas del núcleo.
5. Dos categorías y cuatro productos con existencias válidas.
6. Lectura de un registro creado, después de actualizarlo y después de reiniciar la API conservando la misma base.

Las capturas de pgAdmin/DBeaver o la exportación SQL deben corresponder al esquema actual. Un diagrama antiguo o el snapshot de EF no prueba por sí solo la siembra ejecutada.

### 5.3. Qué prueban los archivos de pruebas

| Archivo | Riesgo principal cubierto |
| --- | --- |
| `ManagementIntegrationTests.cs` | CRUD, registros normalizados, producción múltiple, historial de salud, RBAC de Employee y siembra/lecturas |
| `AuthenticationIntegrationTests.cs` | Login, perfil, refresh, cuenta inactiva, permisos, lectura exclusiva y fotografías privadas |
| `FarmAccessSecurityTests.cs` | Aislamiento por finca, rol persistido frente a claim obsoleto y acceso a archivos de otra finca |
| `AuthRequestValidationTests.cs` | Reglas de identidad y credenciales de entrada |
| `SecurityAndStorageTests.cs` | Catálogo de permisos, tokens, firmas de archivo, límite de tamaño y rutas inseguras |
| `AnimalQueryServiceTests.cs` / `AnimalWeightReaderTests.cs` | Proyecciones descriptivas y último pesaje |
| `ExceptionMiddlewareTests.cs` / pruebas de pipeline | Estructura y códigos de error de la API |

Las pruebas que configuran `.UseInMemoryDatabase(...)` ejecutan lógica y pipeline con un proveedor de prueba. La verificación de migraciones, `CHECK`, claves foráneas, precisiones y concurrencia debe realizarse además sobre PostgreSQL. Cada resultado debe indicar qué proveedor utilizó.

### 5.4. Registro de resultados de ejecución

Validación final ejecutada el 1 de octubre de 2026, hora de Venezuela. Los comandos, proveedores, fixtures y resultados detallados están en [evidencias de verificación](evidencias-verificacion.md).

| Comprobación | Resultado ejecutado |
| --- | --- |
| Build de la solución | Publicación Docker exitosa con SDK .NET 10.0.401 |
| Pruebas .NET | 87 aprobadas, 0 fallidas/omitidas; EF InMemory en pruebas de servicios/pipeline |
| Migraciones PostgreSQL 15 | Tres migraciones aplicadas; actualización histórica y rechazo seguro de huevos por lote comprobados |
| Esquema y siembra SQL | Exportaciones reales de esquema/datos importadas en otra base; seed dos veces sin duplicados |
| Concurrencia serializable | Tres carreras: una respuesta 201 y una 409 por par; sin estado parcial |
| Colección Postman/Newman | 96 solicitudes y 174 assertions; 0 fallos sobre API con PostgreSQL 15.19 |
| Persistencia tras reinicio | Alta/edición/reinicio/lectura: valor editado conservado en la misma base |
| Descarga privada | Pruebas .NET: JWT autorizado, 401 sin token y 404 fuera de finca |

Las comprobaciones PostgreSQL se ejecutaron en bases desechables separadas. La base personal existente no fue migrada ni borrada durante esta validación.

## 6. Límites y alcance posterior

La cobertura de estas fases es persistencia del núcleo operativo y seguridad de la API. Los once recursos CRUD, los cambios de estado de salud y la producción unificada tienen contratos HTTP concretos. Las entidades clínicas, reproductivas, financieras y de alertas amplían el modelo, pero sus procesos completos aún requieren casos de uso, autorización, validación y evidencia propios.

La auditoría actual registra operaciones del repositorio, pero no es una bitácora universal. Los archivos físicos no participan en una transacción distribuida con PostgreSQL. El control de imágenes verifica firmas y rutas, sin decodificación completa. Los catálogos compartidos son visibles según permiso, mientras los datos operativos se filtran por finca.

Estas delimitaciones permiten explicar exactamente qué puede demostrarse con la API actual y qué corresponde a una evolución del producto, sin atribuir funcionalidad operativa a una tabla o diagrama solamente.
