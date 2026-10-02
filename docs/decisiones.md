# Registro de decisiones

## Actualización: producción unificada y gestión persistente

**Decisión:** una sola tabla `AnimalProduction` para los productos obtenidos de animales; `OperationId` identifica una operación con varios resultados. Los insumos se catalogan en `Products` y sus existencias por finca en `FarmInventory`. El CRUD persistente reemplaza el catálogo en memoria y conserva las rutas recuperadas con contratos explícitos.

**Por qué:** el animal es el origen de la producción, mientras que los insumos se compran y almacenan. Las existencias de fincas diferentes no pueden sumarse como si pertenecieran a un único almacén.

**Impacto:** tablas antiguas de leche/lana/huevos/sacrificio retiradas mediante migración, API unificada y reglas de fecha, unidad y sacrificio. Los permisos ahora incluyen acciones de escritura; las entradas siguientes que describen el inicio con `list/get` representan decisiones anteriores. Consulte [decisiones actuales](engineering-decisions.md) y [modelo implementado](modelo-produccion-y-crud.md).

Entradas breves sobre decisiones de **negocio** y de modelo de datos. Formato: **Decisión · Por qué · Impacto**.

Al cambiar una regla del sistema, actualizar la [guía de negocio](guia-de-negocio.md) y agregar aquí el motivo.

## Especies como tabla de grupos

**Decisión:** clasificar el ganado por especie (bovino, ovino, porcino y otras) en una tabla de grupos, cada una con propósito productivo y período de gestación.

**Por qué:** son datos de referencia estables y compartidos por todas las fincas; cada especie define razas y tiempos propios.

**Impacto:** los catálogos de especie y raza son globales, no pertenecen a una finca.

## Roles y permisos con estructura de Laravel Permission

**Decisión:** adoptar la estructura de Laravel Permission (spatie): `Roles`, `Permissions`, `RolePermissions`, `UserPermissions` y `UserRoles`, con `guard_name`, y nombrar los permisos al estilo Django (`tabla.accion`), empezando por `list` y `get`.

**Por qué:** es un modelo de permisos probado y flexible, que permite otorgar permisos a roles y también directo a un usuario.

**Impacto:** el acceso efectivo de un usuario es la unión de sus permisos directos y los de sus roles; el superusuario supera las comprobaciones de permisos, pero conserva las restricciones de negocio e integridad.

## Multi-finca con datos separados

**Decisión:** el sistema admite varias fincas y cada usuario solo accede a las asignadas.

**Por qué:** un mismo equipo puede operar más de un establecimiento sin mezclar información.

**Impacto:** toda entidad operativa pertenece a una finca; los catálogos son comunes.

## Edad y peso derivados

**Decisión:** no almacenar edad ni peso actual como valores editables; se calculan desde la fecha de nacimiento y el último pesaje.

**Por qué:** evita datos desactualizados o contradictorios.

**Impacto actual:** las consultas muestran edad y último peso sin duplicar esos valores en el animal. El cálculo de ganancia entre pesajes como indicador sanitario corresponde a una ampliación posterior.

## Historial de salud, no solo estado actual

**Decisión actual:** conservar el estado resumido en `Animal.HealthStatus` y registrar cada cambio en `HealthStatusChange`, con estado anterior, nuevo, motivo y usuario. El modelo de eventos clínicos permite una ampliación futura de vacunas, tratamientos y enfermedades.

**Por qué:** la operación necesita saber qué se aplicó, cuándo y con qué retiro.

**Impacto actual:** el estado se modifica explícitamente mediante los casos de uso y el historial permite seguir esos cambios. No se deduce automáticamente de eventos clínicos ni genera alertas de dosis en esta versión.

## Trazabilidad completa del animal

**Decisión:** registrar nacimiento, salud, reproducción, producción y movimientos como historial del animal.

**Por qué:** permite auditar el origen y la vida del animal y responder a exigencias sanitarias.

**Impacto:** los movimientos y eventos son datos permanentes, no se sobrescriben.

## El sistema se entrega como API REST

**Decisión:** construir el producto como API REST, sin interfaz web en esta etapa.

**Por qué:** concentra el esfuerzo en la lógica de negocio y facilita pruebas y futuras interfaces.

**Impacto:** la verificación se hace con documentación interactiva y colecciones de peticiones.

## Modelo de datos ganadero con especies como eje

**Decisión:** modelar el dominio con `Species` (bovino, ovino, porcino y otras) como eje de clasificación, del que cuelgan `Breed`, `Lot` y `Animal`.

**Por qué:** la especie define razas, propósito productivo y período de gestación, comunes a las fincas.

**Impacto:** los catálogos son globales; los animales y lotes se relacionan con su especie y raza.

## Eventos con discriminador e historial

**Decisión:** modelar salud y reproducción como eventos con un discriminador (`HealthEvents`, `ReproductiveEvents`) en lugar de sobrescribir estados.

**Por qué:** interesa saber qué pasó, cuándo y con qué resultado, no solo la situación actual.

**Impacto previsto:** los discriminadores permiten representar eventos clínicos y reproductivos diferentes. La deducción automática de estados y las alertas por dosis/preñez todavía requieren casos de uso propios.

## Multi-finca con `Farm` como raíz

**Decisión:** toda entidad operativa pertenece a una `Farm`; los catálogos (especie, raza, producto) son comunes.

**Por qué:** un mismo equipo opera varias fincas sin mezclar su información.

**Impacto:** las consultas se filtran por finca y los datos de una finca nunca se cruzan con otra.

## Tabla de logs de auditoría

**Decisión:** registrar en `AuditLogs` quién cambió qué, con valores anteriores y nuevos en formato `jsonb`.

**Por qué:** ante varias personas operando, hace falta rastrear cambios y su autor.

**Impacto actual:** las escrituras realizadas mediante `ManagementRepository` generan auditoría en la misma transacción. Las operaciones de Identity, fotografías y SQL externo no pasan por este repositorio y no quedan cubiertas automáticamente; tampoco existe aún una interfaz completa para consultar auditoría.

## Reinicio de migraciones al reemplazar el modelo Phase 1

**Decisión:** al pasar del modelo Phase 1 (`Herd`/`Animal` en memoria) al modelo de negocio, se reiniciaron las migraciones con una `InitialCreate` limpia.

**Por qué:** EF intentaba transformar la tabla `Animals` existente en lugar de recrearla, lo que producía una migración incorrecta.

**Impacto:** el esquema actual nace de una migración limpia; los datos Phase 1 eran descartables.

## Fotos del animal (múltiples) en disco con volumen persistente

**Decisión:** modelar las fotos como la entidad `AnimalPhoto` (relación 1—N con `Animal`), cada una con su fecha de subida (`UploadedAt`). Los archivos se guardan en el sistema de archivos (`Storage:RootPath`) y, en Docker, en el volumen nombrado (`daw-uploads`).

**Por qué:** un animal suele tener varias fotos a lo largo del tiempo y conviene saber cuándo se subió cada una; las imágenes no deben guardarse en la base de datos.

**Impacto actual:** `POST /api/animals/{id}/photo` agrega una foto y `DELETE /api/animals/{id}/photos/{photoId}` elimina una concreta. El contenido se obtiene por `GET /api/animals/{id}/photos/{photoId}/content`, con JWT, permiso y acceso a la finca; `/uploads` ya no se sirve públicamente. La lista expone `coverPhotoUrl` y `photoCount`, y el detalle la colección `photos`. Las URL requieren autenticación también cuando se consumen desde una interfaz.

## Seguimiento de actualizaciones (`updatedAt`) y animales sin actualización reciente

**Decisión:** agregar `Animal.UpdatedAt` (se refresca con cada cambio del animal, vía `SaveChanges`, y al subir/borrar fotos) y un endpoint `GET /api/animals/stale?days=X` que lista los animales sin actualizar en más de X días.

**Por qué:** permite detectar animales cuyo registro se ha dejado de mantener.

**Impacto:** el listado y el detalle exponen `updatedAt`; `stale` habilita tareas de control y alertas.

## Permisos directos al usuario además de por rol

**Decisión:** además de por rol (`RolePermissions`), permitir otorgar un permiso directo a un usuario (`UserPermissions`), como en Laravel Permission.

**Por qué:** cubre casos puntuales sin crear un rol nuevo, y es parte del modelo de spatie que se adoptó.

**Impacto:** `IPermissionChecker` considera permisos directos y de roles; al cambiar de rol, los permisos directos se mantienen.

## Secretos fuera del repositorio (`.env`)

**Decisión:** sacar la contraseña de PostgreSQL, la clave de firma JWT y la contraseña del admin de `appsettings`. Se definen en un archivo `.env` (no versionado) que `docker compose` consume y pasa a la app como variables de entorno (`Jwt__Key`, `ConnectionStrings__Default`, `Seed__AdminPassword`). Se versiona `.env.example` como plantilla.

**Por qué:** los secretos en el código quedan públicos; con `.env` cada entorno usa los suyos y no se filtran al repositorio ni a la imagen Docker.

**Impacto:** la app valida al arrancar y **no inicia** si `Jwt:Key` falta o es el placeholder. Para ejecución local sin Docker se usan `dotnet user-secrets`.
