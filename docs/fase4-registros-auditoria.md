# Auditoría, conservación de información y decisiones de Fase 4

## Qué permite consultar el módulo

La entrada **Auditoría** de la navegación abre `/auditlogs`. Solo Admin/Administrador con los permisos `auditlogs.list` y `auditlogs.get` pueden consultar la API y el detalle. Los roles operativos no reciben estos permisos por defecto; incluso un permiso directo no elimina la restricción de rol del servidor.

El listado permite combinar usuario, finca, acción, tipo de registro, identificador y fechas. La paginación es del servidor, con 25 filas por defecto y un máximo de 100; el orden es fecha descendente e ID como desempate. El campo de búsqueda busca tipo de entidad, acción técnica o identificador, no el contenido de los valores. Las fechas del filtro corresponden a días UTC y las horas se muestran en la zona del navegador.

El detalle se abre en un lateral modal, carga los valores únicamente al solicitarlo y compara los campos anteriores y posteriores. Por defecto muestra cambios; el administrador puede incluir los campos que conservaron su valor. El teclado permite cerrar con Escape y volver al control que abrió el panel. Los valores no se interpretan como HTML. El listado no devuelve los snapshots completos. La consulta aplica redacción recursiva de campos de contraseña, token, secreto y SecurityStamp en los snapshots históricos.

### Contratos REST

| Operación | Endpoint | Permiso |
| --- | --- | --- |
| Listado | GET /api/admin/auditlogs | auditlogs.list |
| Opciones | GET /api/admin/auditlogs/options | auditlogs.list |
| Detalle | GET /api/admin/auditlogs/{id} | auditlogs.get |

No hay endpoints para modificar o borrar auditoría. Un usuario sin sesión recibe 401 y un empleado recibe 403. Las consultas inválidas reciben 400; un evento inexistente recibe 404. Se añadieron índices de fecha/ID, usuario/fecha y finca/fecha mediante la migración AuditQueryIndexes. No se eliminaron tablas ni registros de la demostración.

## Qué se registra y cómo se identifica el origen

- Las escrituras de ManagementRepository conservan Added/Modified/Archived/Restored, actor, finca cuando la entidad tiene FarmId o es la propia finca, ID, valores y momento. El cambio y su auditlog se guardan en la misma transacción.
- Gestión de usuarios registra UserCreated, UserUpdated y PasswordReset. El restablecimiento conserva la señal de revocación de sesiones y nunca la nueva contraseña.
- Fotografías registra Added/Archived/Restored con actor, finca, IP y metadatos; no copia los bytes de la imagen al historial. El registro y su auditlog se guardan juntos.
- Cambios efectivos de permisos de roles registran RolePermissionGranted/RolePermissionRevoked, con actor, IP y asignación anterior/posterior. Repetir una asignación o retiro sin efecto no duplica eventos.
- Autenticación registra LoginSucceeded, LoginRejected, RegistrationSucceeded y Logout. Un rechazo de credenciales no atribuye el intento al nombre escrito: actor e ID quedan vacíos, con un motivo general. No se guardan las credenciales presentadas, JWT, cookies ni refresh tokens en esos eventos.
- Logout se identifica mediante el refresh token existente en el servidor y registra el usuario cuyo token se revoca. Repetir el cierre no duplica el evento. La revocación previa a un nuevo login no genera un cierre ficticio. Logout revoca la renovación de esa sesión; un JWT ya emitido conserva su vencimiento, salvo que la cuenta o su security stamp se invaliden.

La IP nueva proviene de Connection.RemoteIpAddress. Un proxy solo puede sustituirla mediante Forwarded Headers si está reconocido por ASP.NET Core. Compose y la imagen desactivan la opción automática que aceptaba cabeceras de cualquier proxy. Se permite configurar una IP exacta mediante TRUSTED_PROXY_IP (ReverseProxy:KnownProxies:0); el valor debe corresponder al proxy administrado y revisarse cuando cambie su dirección. Sin esa configuración se conserva la IP del transporte, que en Docker puede ser la del proxy. No se inventan IP para eventos anteriores.

Esta decisión sigue la [documentación de Microsoft sobre proxies confiables](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0). La configuración local mantiene HTTP de loopback para Desarrollo; no acredita un despliegue remoto HTTPS.

Los nombres de usuario/finca del listado se resuelven contra el catálogo actual. Se conservan los IDs y snapshots cuando ya no existe ese registro. La consulta no audita todos los GET, SQL ejecutado fuera de la aplicación, ni cambios hechos por otro programa. No es un ledger criptográfico y no impide a un propietario de la base alterar sus filas. La auditoría tampoco sustituye las copias de seguridad.

## Conservación y cotización actuales

La actualización de eliminación lógica incorpora metadatos de archivo en los recursos de negocio, filtros globales, protección de SaveChanges, papelera administrativa y restauración con reglas de negocio. Se conservan fotografías, genealogía y hechos históricos. Anular un rendimiento lo excluye del cálculo sin reactivar un animal sacrificado. Los usuarios se desactivan y las asignaciones retiradas se conservan.

La tasa USD/Bs se obtiene automáticamente desde BCV Today, con fecha efectiva, publicación, fuente e historial. Es un proveedor de terceros que replica la referencia BCV; una falla conserva la última tasa guardada y permite entrada manual. El costo y valor de insumos en USD se convierten para revisión y planificación de reposiciones, sin presentarlos como ganancias. Consultar [contratos, validación y límites de conservación/BCV](fase4-conservacion-bcv.md).

## Por qué se documentaron diferencias

1. **JWT:** el quiz/material propone persistir el token en localStorage. REAF recomienda memoria o cookies seguras. La SPA conserva el JWT en memoria y recupera la sesión con refresh HttpOnly/SameSite/CSRF; el token no queda en Web Storage. Esta solución conserva la persistencia solicitada y sigue la recomendación de la página. Debe explicarse en la defensa, sin atribuir al profesor una aprobación que no consta.
2. **Colores:** el documento Fase 4/03_Tarea_Asignacion_Fase4_y_Rubrica.docx pide Azul UNET #003366 en ThemeContext.jsx y la rúbrica. El usuario eligió expresamente una identidad de finca. Se utilizan neutros dominantes, verde para identidad/acciones positivas y otros colores según información, advertencia, error o serie del gráfico. Es una diferencia de marca literal, no ausencia de tema claro/oscuro o de contraste.
3. **Employee:** un antecedente de Fase 3 permitía crear productos. En esta implementación Employee opera animales, pesajes, atención y existencias de sus fincas; el catálogo compartido, sus precios y sus condiciones de retiro quedan bajo mantenimiento de Admin/Administrador. Es una restricción deliberada frente a ese antecedente. El usuario ratificó esta restricción: no hay contradicción entre UI y API; permanece la diferencia frente al antecedente.

Estas diferencias no constituyen un porcentaje calculado de cumplimiento. El documento vigente del 9 de octubre pide cobertura razonable; aprobar todos los casos ejecutados tampoco significa medir 100% de líneas o ramas. La [auditoría integral](fase4-auditoria-integral.md) mantiene el contraste entre los instrumentos.

## Verificación histórica del visor

237 UnitTests, 169 Core.Tests, 117 Vitest y 133 Playwright aprobados, con una omisión intencional exclusiva móvil. Se ejecutaron 48 combinaciones de pantalla/tema/viewport con axe y dos detalles laterales adicionales, sin infracciones detectadas. AuditLogsPage tiene 100% de líneas y 76% de ramas en la medición V8; el conjunto frontend tiene 40,04% de líneas y 24,41% de ramas. No se presenta esa cobertura global como completa.

Las pruebas incluyen filtros combinados, paginación, límites, redacción histórica, eventos sin actor/finca existentes, permisos de rol y directos, login rechazado sin credenciales, logout repetido, origen de IP con proxy confiable y no confiable, auditoría de alta/baja de fotografías, concesión/retiro idempotente de permisos de roles y protección de los movimientos del animal. Los recorridos de navegador utilizan PostgreSQL en daw-phase4-empty. La limpieza SQL descrita en esta revisión histórica fue retirada por la actualización de conservación: las pruebas actuales archivan registros y retiran únicamente los volúmenes del proyecto desechable.

Verificación actual: [conservación y BCV](evidence/phase4-retention-bcv/README.md).

[Reportes y resumen](evidence/phase4-audit-module/README.md) · [Captura del detalle](screenshots/audit-log-detail.png).
