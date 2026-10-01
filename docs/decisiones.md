# Registro de decisiones

Entradas breves sobre decisiones de **negocio** y de modelo de datos. Formato: **Decisión · Por qué · Impacto**.

Al cambiar una regla del sistema, actualizar la [guía de negocio](guia-de-negocio.md) y agregar aquí el motivo.

## Especies como tabla de grupos

**Decisión:** clasificar el ganado por especie (bovino, ovino, porcino y otras) en una tabla de grupos, cada una con propósito productivo y período de gestación.

**Por qué:** son datos de referencia estables y compartidos por todas las fincas; cada especie define razas y tiempos propios.

**Impacto:** los catálogos de especie y raza son globales, no pertenecen a una finca.

## Roles y permisos con estructura de Laravel Permission

**Decisión:** adoptar la estructura de Laravel Permission (spatie): `Roles`, `Permissions`, `RolePermissions`, `UserPermissions` y `UserRoles`, con `guard_name`.

**Por qué:** es un modelo de permisos probado y flexible, que permite otorgar permisos a roles y también directo a un usuario.

**Impacto:** el acceso efectivo de un usuario es la unión de sus permisos directos y los de sus roles; el superusuario no tiene restricciones.

## Multi-finca con datos separados

**Decisión:** el sistema admite varias fincas y cada usuario solo accede a las asignadas.

**Por qué:** un mismo equipo puede operar más de un establecimiento sin mezclar información.

**Impacto:** toda entidad operativa pertenece a una finca; los catálogos son comunes.

## Edad y peso derivados

**Decisión:** no almacenar edad ni peso actual como valores editables; se calculan desde la fecha de nacimiento y el último pesaje.

**Por qué:** evita datos desactualizados o contradictorios.

**Impacto:** la ganancia de peso se calcula entre pesajes y se usa como indicador de salud.

## Historial de salud, no solo estado actual

**Decisión:** guardar cada evento de salud (vacuna, tratamiento, enfermedad) en lugar de un único campo de estado.

**Por qué:** la operación necesita saber qué se aplicó, cuándo y con qué retiro.

**Impacto:** el estado actual se deduce del historial y se habilitan alertas por próxima dosis.

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

**Impacto:** el estado actual se deduce del historial y se habilitan alertas por próxima dosis o preñez.

## Multi-finca con `Farm` como raíz

**Decisión:** toda entidad operativa pertenece a una `Farm`; los catálogos (especie, raza, producto) son comunes.

**Por qué:** un mismo equipo opera varias fincas sin mezclar su información.

**Impacto:** las consultas se filtran por finca y los datos de una finca nunca se cruzan con otra.

## Tabla de logs de auditoría

**Decisión:** registrar en `AuditLogs` quién cambió qué, con valores anteriores y nuevos en formato `jsonb`.

**Por qué:** ante varias personas operando, hace falta rastrear cambios y su autor.

**Impacto:** cada modificación relevante queda auditada y consultable por farm.

## Reinicio de migraciones al reemplazar el modelo Phase 1

**Decisión:** al pasar del modelo Phase 1 (`Herd`/`Animal` en memoria) al modelo de negocio, se reiniciaron las migraciones con una `InitialCreate` limpia.

**Por qué:** EF intentaba transformar la tabla `Animals` existente en lugar de recrearla, lo que producía una migración incorrecta.

**Impacto:** el esquema actual nace de una migración limpia; los datos Phase 1 eran descartables.

## Foto del animal en disco con volumen persistente

**Decisión:** guardar las fotos de los animales en el sistema de archivos (`Storage:RootPath`), referenciadas desde `Animal.PhotoUrl` y registradas como `Attachment`; en Docker se monta un volumen nombrado (`daw-uploads`) para que persistan.

**Por qué:** las imágenes no conviene almacenarlas en la base de datos y deben sobrevivir a los reinicios del contenedor.

**Impacto:** subir/borrar la foto se hace por `POST`/`DELETE /api/animals/{id}/photo`, y el archivo se sirve en `/uploads`. No requiere migración: `PhotoUrl` y `Attachments` ya existían.

## Permisos directos al usuario además de por rol

**Decisión:** además de por rol (`RolePermissions`), permitir otorgar un permiso directo a un usuario (`UserPermissions`), como en Laravel Permission.

**Por qué:** cubre casos puntuales sin crear un rol nuevo, y es parte del modelo de spatie que se adoptó.

**Impacto:** `IPermissionChecker` considera permisos directos y de roles; al cambiar de rol, los permisos directos se mantienen.
