# Producto y fundamentos técnicos

## 1. Qué resuelve el sistema

La operación ganadera necesita identificar sus animales, conocer su ubicación y condición, registrar cuánto producen y controlar los insumos disponibles. Esta versión ofrece esos flujos mediante una API persistente con permisos de acceso por finca y operación.

**Disponibles:** CRUD de once recursos, historial de cambios de salud, pesajes, fotos privadas, producción animal unificada, categorías e insumos con precios, existencias por finca, autenticación JWT y autorización por roles y permisos. Swagger permite ejecutar los contratos. La página Blazor es una presentación de la API; los formularios de gestión completos pertenecen a un incremento posterior.

## 2. Flujo operativo reproducible

1. Ejecutar las migraciones y la siembra según [setup](setup.md).
2. Iniciar sesión en `POST /api/auth/login` con `username` —también acepta correo— y `password`.
3. Copiar `accessToken` y autorizar Swagger con el esquema Bearer. El resultado de login incluye vencimiento, refresh token y `user` con nombre, email, roles y permisos.
4. Consultar fincas y especies; sus respuestas incluyen `id` y `data` con los atributos editables. Los detalles de animales incluyen además sus UUID relacionados para poder editarlos.
5. Crear un animal con el UUID de su finca, especie y, opcionalmente, raza, lote y potrero:

```json
{
  "farmId": "<uuid-finca>",
  "speciesId": "<uuid-especie-bovina>",
  "internalTag": "BOV-009",
  "sex": "Female",
  "purpose": "DualPurpose",
  "birthDate": "2024-01-15"
}
```

6. Consultar `GET /api/animals/{id}`. El tag se normaliza en mayúsculas; un duplicado dentro de la misma finca devuelve 409. Una raza de otra especie o un lote de otra finca también se rechazan.
7. Actualizar su condición con `PATCH /api/animals/{id}/health-status`:

```json
{"status":"UnderObservation","reason":"Seguimiento de condición corporal"}
```

8. Registrar peso en `/api/weights` y producción en `/api/production`, proporcionando el animal, la finca y una fecha válida. Para ordeño: `productType: Milk`, `method: Milking`, `unit: Liter`. Para esquila: `Wool`, `Shearing`, `Kilogram`. Para carne: `Meat`, `Slaughter`, `Kilogram`.
9. Registrar varios productos del mismo sacrificio usando el mismo `operationId`: carne y piel ocupan filas distintas dentro de `AnimalProduction`. El animal queda `Dead` y no puede ser ordeñado posteriormente ni sacrificarse por segunda vez.
10. Crear categorías e insumos; registrar existencias en `/api/inventory`. El stock y sus umbrales pertenecen a la finca y al insumo, por lo que dos fincas pueden tener existencias diferentes del mismo SKU.

La [colección Postman](../postman/Cattle-Management.full.postman_collection.json) automatiza los flujos, las bajas en orden de dependencias y los escenarios 401, 403, 400, 404, 409 y 500.

## 3. Onion Architecture y Repository

```mermaid
flowchart LR
    API[Presentation.API] --> APP[Core.Application]
    API --> INF[Infrastructure]
    INF --> APP
    INF --> DOM[Core.Domain]
    APP --> DOM
```

Las flechas representan referencias entre proyectos. Domain contiene las entidades, sin referencias a EF o ASP.NET Core. Application define los contratos `ICrudService<TRequest>` y `IManagementRepository`, los validadores y las reglas de cada recurso. Infrastructure implementa el puerto de persistencia mediante EF. API compone esos servicios y convierte HTTP en llamadas al caso de uso.

La ejecución concreta sigue **controlador → CrudService → ResourceDefinition → IManagementRepository → ManagementRepository → AppDbContext → PostgreSQL**. La aplicación conoce la interfaz del repositorio, no el proveedor Npgsql ni `DbContext`. Los predicados LINQ se expresan como árboles `Expression<Func<T,bool>>`; EF traduce su ejecución a SQL desde Infrastructure. Los DTO evitan serializar navegaciones del dominio y crear ciclos JSON.

Los contratos de consulta existentes `IAnimalQueryService` e `IAnimalWeightReader` se conservan. Sus implementaciones proyectan los datos del animal y el último pesaje. Las lecturas usan `AsNoTracking`; las entidades sólo se rastrean cuando se necesita modificarlas. Véase la explicación original de [Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/) y [consultas sin seguimiento de EF Core](https://learn.microsoft.com/en-us/ef/core/querying/tracking).

## 4. DI y ciclos de vida

| Ciclo | Servicios | Motivo |
| --- | --- | --- |
| Scoped | `AppDbContext`, repositorios, CRUD, definiciones, consultas, permisos, autenticación y acceso a fincas | Comparten el contexto de una petición y su unidad de trabajo |
| Transient | `IValidator<TRequest>` | Cada resolución obtiene un validador independiente |
| Singleton | `ITokenService`, `IFileStorage`, validación inmutable de opciones JWT | Utilidades sin depender de un `DbContext` ni de servicios scoped |

`JwtOptionsValidator` verifica configuración; no es un validador de DTO de negocio. Las pruebas resuelven servicios desde dos ámbitos y verifican identidad/reutilización. No existe ya un almacén singleton de animales ni otra fuente de datos operativos en memoria. Documentación de [DI de Microsoft](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes) y [registro de FluentValidation](https://docs.fluentvalidation.net/en/latest/di.html).

## 5. Integridad y transacciones

Fluent API declara UUID, tablas, longitudes, índices únicos, precisión monetaria `(18,2)`, valores predeterminados y restricciones. `Products.CategoryId` relaciona categorías e insumos con `Restrict`. `FarmInventory` tiene una fila única por finca/insumo. El email normalizado y el nombre de usuario normalizado tienen índices únicos para reforzar Identity.

El CRUD valida el DTO y las referencias antes de guardar. En PostgreSQL, la comprobación y escritura se ejecutan dentro de una transacción **Serializable**. Se guarda conjuntamente el rendimiento, el cambio de estado por sacrificio y los registros de auditoría. Un conflicto concurrente devuelve 409 y exige recargar/reintentar; no deja un animal muerto sin el rendimiento confirmado. Las violaciones reales de índices y claves foráneas también se convierten en 409, sin exponer SQL interno.

La auditoría implementada por `ManagementRepository` registra el usuario, entidad, acción, valores anteriores y nuevos. Cubre los casos de uso que pasan por ese repositorio; no se afirma que todas las operaciones de Identity, archivos o modificaciones SQL externas estén auditadas. La transformación de datos de una migración tampoco representa una acción de un usuario operativo.

## 6. Seguridad y errores HTTP

Identity conserva hashes de contraseñas; JWT HMAC-SHA256 protege la firma del token. El servidor valida firma, emisor, destinatario y expiración. Admin/Administrador mantiene catálogos y elimina registros. Employee opera sobre sus fincas y no puede ejecutar DELETE ni administrar categorías. SoloLectura consulta: un permiso de lectura nunca autoriza subir fotos ni asignar permisos.

Las fotos se sirven por un endpoint autenticado con `photos.get` y acceso a la finca. No se publica `/uploads` como carpeta estática. Los nombres internos se generan de forma segura y se verifica tamaño, tipo permitido y firma de formato. La validación de firma no equivale a un análisis completo de la imagen ni a un antivirus.

El pipeline utiliza `ExceptionMiddleware`, filtro de validación asíncrono y respuestas de estado de API. Se devuelve `application/problem+json` con `type`, `title`, `status`, `detail`, `instance`; los errores de FluentValidation añaden `errors` por campo. El mapeo de errores automáticos MVC se desactiva para evitar respuestas 401 sin `detail`. Los errores 500 mantienen un detalle genérico y se registran en el servidor. [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) establece el contrato de referencia; [FluentValidation](https://docs.fluentvalidation.net/en/latest/aspnet.html) documenta la validación asíncrona explícita.

## 7. Alcance y evidencia

Las pruebas .NET verifican reglas, API y autorización con EF InMemory; PostgreSQL se comprueba mediante migraciones reales, colección HTTP, SQL de restricciones y pruebas concurrentes. El registro de resultados y la comparación académica están en [Fase 2 y Fase 3](fases-2-y-3.md). No se identifica un modelo futuro mapeado con un módulo operativo terminado.

El diseño completo previsto —reproducción, eventos clínicos, raciones, finanzas y tareas— está en [guía de negocio](guia-de-negocio.md). Los once recursos y la producción implementada se describen exhaustivamente en [modelo y CRUD](modelo-produccion-y-crud.md).
