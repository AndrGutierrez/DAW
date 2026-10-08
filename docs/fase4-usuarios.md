# Gestión administrativa de usuarios

## Propósito y acceso

Usuarios permite al administrador crear y mantener cuentas sin modificar la base manualmente. La API requiere Admin/Administrador y permisos users.list, users.create o users.update. Cambiar identidad/roles/fincas/permisos requiere también roles.manage. Employee no puede administrar cuentas aunque el catálogo de permisos incluya users.list.

El listado filtra nombre, usuario, correo y estado, con conteo y paginación en PostgreSQL. La pantalla comparte controles, avisos, validación por campo, protección de cambios sin guardar y panel lateral con el resto de la SPA.

## Operaciones

- Crear cuenta: nombre, usuario único, correo único, contraseña inicial, rol, fincas y permisos adicionales.
- Editar cuenta: identidad, rol, asignaciones, permisos adicionales y estado activo.
- Restablecer contraseña: confirmación explícita, contraseña nueva y cierre de las sesiones anteriores.
- Desactivar: conserva la cuenta y los registros asociados. No existe borrado de usuarios desde este módulo.

Los roles del seed son Administrador, Admin, Employee, Veterinario, Capataz, Operario y SoloLectura. Los permisos adicionales se suman a los del rol; no son una lista de denegaciones. Los roles administrativos acceden a todas las fincas. Los demás necesitan asignaciones; sin ellas no hay datos operativos accesibles.

La pantalla muestra los permisos del rol como consulta. Cambiar la definición global de un rol sigue disponible en la API de administración previa, con roles.manage; no se añadió un editor de plantillas de roles a esta pantalla.

## Integridad y seguridad

ASP.NET Core Identity genera el hash y valida contraseñas de 8–256 caracteres con mayúscula, minúscula, número y símbolo. La contraseña no se devuelve ni se guarda en auditoría. La entrega de la contraseña inicial o restablecida es manual mediante el canal acordado; el sistema no envía correos ni mensajes.

La cuenta superusuario del seed está protegida contra edición y restablecimiento por esta API. Un administrador no puede desactivar su propia cuenta, cambiar su propio rol/permisos adicionales ni restablecer su propia contraseña desde este módulo. El servicio impide quitar al último administrador activo. Estas protecciones se aplican en Application, además de los controles de la interfaz.

Editar requiere expectedVersion, obtenida del ConcurrencyStamp vigente; una versión obsoleta devuelve 409. La escritura usa transacción serializable en PostgreSQL. Identidad, roles, membresías, permisos y auditoría se guardan juntos; un conflicto no deja una asignación parcial.

Los cambios de acceso y contraseña rotan el SecurityStamp y revocan todos los refresh tokens de esa cuenta. La validación de cada JWT consulta Identity y compara el sello: un token anterior recibe 401 de inmediato. La SPA limpia el acceso cuando también falla la renovación. La revocación requiere esa consulta de estado; no se presenta como autenticación completamente sin estado.

## Contratos

| Operación | Ruta | Resultado |
| --- | --- | --- |
| Consulta paginada | GET /api/admin/users?page=1&pageSize=12&search=&isActive=true | items, total, page, pageSize |
| Creación | POST /api/admin/users | 201, cuenta sin hash ni contraseña |
| Edición | PUT /api/admin/users/{id} | 200, cuenta y versión nueva |
| Restablecimiento | POST /api/admin/users/{id}/password | 204 |

El cuerpo de creación/edición contiene username, email, fullName, role, farmIds, directPermissions e isActive. Crear añade initialPassword; editar añade expectedVersion. Restablecer usa newPassword y expectedVersion. No se acepta initialPassword en una edición.

## Evidencia

UserManagementServiceTests usa Moq estricto sobre IUserManagementStore e ICurrentUser y cubre contraseña débil, normalización, duplicación de asignaciones, versiones, protección propia/superusuario y último administrador. UserManagementIntegrationTests prueba el pipeline Identity/HTTP aislado. e2e/users.spec.ts demuestra creación, finca asignada, permisos, restablecimiento y desactivación contra PostgreSQL en escritorio y móvil. La colección Phase4 valida también 403 y 401.

Las cuentas temporales de estas pruebas permanecen desactivadas para conservar su auditoría. El seed respeta los accesos ya establecidos del usuario Employee de demostración. La recuperación de la cuenta protegida usa la configuración de seed de una instalación controlada, según setup.md.
