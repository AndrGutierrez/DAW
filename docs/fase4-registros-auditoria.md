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

- Las escrituras de ManagementRepository conservan Added/Modified/Deleted, actor, finca cuando la entidad tiene FarmId o es la propia finca, ID, valores y momento. El cambio y su auditlog se guardan en la misma transacción.
- Gestión de usuarios registra UserCreated, UserUpdated y PasswordReset. El restablecimiento conserva la señal de revocación de sesiones y nunca la nueva contraseña.
- Fotografías registra Added/Deleted con actor, finca, IP y metadatos; no copia los bytes de la imagen al historial. El registro y su auditlog se guardan juntos.
- Cambios efectivos de permisos de roles registran RolePermissionGranted/RolePermissionRevoked, con actor, IP y asignación anterior/posterior. Repetir una asignación o retiro sin efecto no duplica eventos.
- Autenticación registra LoginSucceeded, LoginRejected, RegistrationSucceeded y Logout. Un rechazo de credenciales no atribuye el intento al nombre escrito: actor e ID quedan vacíos, con un motivo general. No se guardan las credenciales presentadas, JWT, cookies ni refresh tokens en esos eventos.
- Logout se identifica mediante el refresh token existente en el servidor y registra el usuario cuyo token se revoca. Repetir el cierre no duplica el evento. La revocación previa a un nuevo login no genera un cierre ficticio. Logout revoca la renovación de esa sesión; un JWT ya emitido conserva su vencimiento, salvo que la cuenta o su security stamp se invaliden.

La IP nueva proviene de Connection.RemoteIpAddress. Un proxy solo puede sustituirla mediante Forwarded Headers si está reconocido por ASP.NET Core. Compose y la imagen desactivan la opción automática que aceptaba cabeceras de cualquier proxy. Se permite configurar una IP exacta mediante TRUSTED_PROXY_IP (ReverseProxy:KnownProxies:0); el valor debe corresponder al proxy administrado y revisarse cuando cambie su dirección. Sin esa configuración se conserva la IP del transporte, que en Docker puede ser la del proxy. No se inventan IP para eventos anteriores.

Esta decisión sigue la [documentación de Microsoft sobre proxies confiables](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0). La configuración local mantiene HTTP de loopback para Desarrollo; no acredita un despliegue remoto HTTPS.

Los nombres de usuario/finca del listado se resuelven contra el catálogo actual. Se conservan los IDs y snapshots cuando ya no existe ese registro. La consulta no audita todos los GET, SQL ejecutado fuera de la aplicación, ni cambios hechos por otro programa. No es un ledger criptográfico y no impide a un propietario de la base alterar sus filas. La auditoría tampoco sustituye las copias de seguridad.

## Eliminación actual y criterio recomendado

| Información | Comportamiento actual |
| --- | --- |
| Usuarios | Se desactivan con IsActive, conservan la cuenta y su autoría; se invalidan sus sesiones. No hay DELETE de cuentas en el módulo |
| Fincas, especies, razas, lotes, potreros y productos/categorías | Admiten IsActive. DELETE sigue siendo físico y está restringido a administradores, con protecciones de dependencias |
| Animales | El estado representa Activo, Vendido, Fallecido, Transferido o Extraviado. Un registro sin referencias puede borrarse físicamente. El historial sanitario, productivo, reproductivo, de suministros y parentesco tiene protecciones de aplicación o de claves foráneas |
| Traslados del animal | Se añadió una protección explícita: DELETE del animal devuelve 409 si tiene movimientos, evitando que una cascada borre su trazabilidad |
| Pesajes y producción no proveniente de sacrificio | Sus DELETE autorizados eliminan el registro físico y conservan el snapshot de auditoría. No existe papelera o restauración de esos registros |
| Sacrificio | No se permite borrar el registro ni reactivar el animal para deshacer el sacrificio. Las correcciones de rendimiento son un flujo distinto |
| Inventario con movimientos | No se elimina el saldo trazado; el servidor exige movimientos para variarlo |
| Fotografías | DELETE elimina el registro y su archivo privado; no hay papelera de archivos |

Para ganado con actividad, conservar la ficha y utilizar su estado operativo es lo apropiado. La desactivación es útil para personas y catálogos que ya no deben seleccionarse. Un futuro archivo administrativo de animales debe ser un campo separado del estado biológico/comercial: no se debería marcar como vendido o muerto únicamente para esconderlo.

La eliminación lógica es recomendable para registros operativos cuya recuperación sea necesaria, pero exige definir archivado/anulación, actor, fecha, motivo, restauración, filtros, unicidad y efecto en KPIs. No se implantó un IsDeleted universal en esta entrega. En pesajes o producción, anular con motivo y excluir del cálculo es más explícito que ocultar una fila mientras los informes siguen sumándola. Los eventos de auditoría deben conservarse como historial; una política de retención necesita su propio diseño. Borrar físicamente solo un dato erróneo sin actividad dependiente es un caso diferente de retirar un animal o anular una operación histórica.

## Utilidad de la referencia BCV

Los costos/precios de insumos se almacenan en USD. La referencia fechada permite mostrar en Bs el costo o valor de las existencias y comparar categorías en la moneda de uso cotidiano de la finca. La fórmula de presentación es valor USD × Bs por USD, utilizando la fecha elegida. Esto ayuda a revisar suministros veterinarios/alimentación y planificar reposiciones; no representa ganancias, ventas ni una contabilidad financiera completa.

El administrador ingresa la tasa manualmente, con fecha efectiva e historial auditable. La aplicación no descarga ni verifica automáticamente la cotización del BCV. En la demo aún debe incorporarse una referencia oficial comprobable; sin ella Bs permanece deshabilitado. Las cotizaciones sintéticas utilizadas por las pruebas permanecen en la base desechable.

## Por qué se documentaron diferencias

1. **JWT:** el quiz/material propone persistir el token en localStorage. REAF recomienda memoria o cookies seguras. La SPA conserva el JWT en memoria y recupera la sesión con refresh HttpOnly/SameSite/CSRF; el token no queda en Web Storage. Esta solución conserva la persistencia solicitada y sigue la recomendación de la página. Debe explicarse en la defensa, sin atribuir al profesor una aprobación que no consta.
2. **Colores:** la rúbrica/asignación anterior pide Azul UNET #003366. El usuario eligió expresamente una identidad de finca. Se utilizan neutros dominantes, verde para identidad/acciones positivas y otros colores según información, advertencia, error o serie del gráfico. Es una diferencia de marca literal, no ausencia de tema claro/oscuro o de contraste.
3. **Employee:** un antecedente de Fase 3 permitía crear productos. En esta implementación Employee opera animales, pesajes, atención y existencias de sus fincas; el catálogo compartido, sus precios y sus condiciones de retiro quedan bajo mantenimiento de Admin/Administrador. Es una restricción deliberada frente a ese antecedente. Una ampliación debería limitarse al permiso concreto y contrastarse con la pauta vigente, sin convertir al operador en administrador.

Estas diferencias no constituyen un porcentaje calculado de cumplimiento. El documento vigente del 9 de octubre pide cobertura razonable; aprobar todos los casos ejecutados tampoco significa medir 100% de líneas o ramas. La [auditoría integral](fase4-auditoria-integral.md) mantiene el contraste entre los instrumentos.

## Verificación de esta ampliación

237 UnitTests, 169 Core.Tests, 117 Vitest y 133 Playwright aprobados, con una omisión intencional exclusiva móvil. Se ejecutaron 48 combinaciones de pantalla/tema/viewport con axe y dos detalles laterales adicionales, sin infracciones detectadas. AuditLogsPage tiene 100% de líneas y 76% de ramas en la medición V8; el conjunto frontend tiene 40,04% de líneas y 24,41% de ramas. No se presenta esa cobertura global como completa.

Las pruebas incluyen filtros combinados, paginación, límites, redacción histórica, eventos sin actor/finca existentes, permisos de rol y directos, login rechazado sin credenciales, logout repetido, origen de IP con proxy confiable y no confiable, auditoría de alta/baja de fotografías, concesión/retiro idempotente de permisos de roles y protección de los movimientos del animal. Los recorridos de navegador utilizan PostgreSQL en daw-phase4-empty. La limpieza de fixtures quita movimientos únicamente de IDs propiedad de la prueba, mediante un script que valida UUID, indicador de entorno y etiqueta exacta del contenedor; nunca se ejecuta contra daw-phase4. La base desechable se elimina al concluir la verificación.

[Reportes y resumen](evidence/phase4-audit-module/README.md) · [Captura del detalle](screenshots/audit-log-detail.png).
