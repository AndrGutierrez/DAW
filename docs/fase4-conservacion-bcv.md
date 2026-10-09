# Conservación de registros y referencia BCV

Actualización de Fase 4, 8 de octubre de 2026. Esta guía sustituye las descripciones anteriores de borrado físico y cotización exclusivamente manual. La [evidencia de esta revisión](evidence/phase4-retention-bcv/README.md) identifica las pruebas actuales y su alcance.

## Eliminación lógica y recuperación

Todos los DELETE de recursos de negocio archivan la fila mediante IsDeleted, DeletedAt UTC y DeletedByUserId. Las consultas operativas excluyen las filas archivadas con filtros globales de EF. IsActive y el estado del animal conservan su significado: archivar no equivale a vender o sacrificar. Las cuentas se desactivan con IsActive, conservan su identidad y se invalidan sus sesiones.

La papelera administrativa en /archive permite buscar, filtrar por tipo, paginar, revisar valores en un panel lateral y restaurar. Incluye fincas, especies, razas, potreros, lotes, categorías, productos, existencias, animales, pesajes, producción y fotografías. Los permisos archive.list/get/restore requieren además rol Admin/Administrador. No ofrece eliminación permanente.

| Caso | Regla de conservación |
| --- | --- |
| Animal con actividad | Se conserva la ficha y sus eventos. Desaparece de consultas operativas; su producción válida permanece en informes históricos |
| Genealogía | Una referencia mínima de parentesco conserva el antepasado archivado dentro de las fincas autorizadas. No se permite seleccionarlo como progenitor de un nuevo animal |
| Fotografía | Se archivan metadatos y se conserva el archivo privado. No es accesible mientras la foto o su animal están archivados; restaurar recupera el acceso |
| Pesaje o rendimiento | Se conserva la fila y queda excluida del cálculo correspondiente. Restaurar vuelve a incorporarla si cumple las reglas |
| Sacrificio | Anular su rendimiento no cambia el estado Fallecido ni permite otro sacrificio o un peso vivo posterior |
| Existencias | Un saldo distinto de cero bloquea el archivo. Todo cambio de saldo, incluso desde cero, requiere un movimiento justificado; PUT permite editar límites/ubicación sin alterar el saldo |
| Catálogos y fincas | Se bloquea el archivo si quedan dependencias operativas activas. Se deben resolver o archivar antes; no se usa cascada para ocultarlas |
| Usuarios, fincas y permisos asignados | Las cuentas se desactivan; las asignaciones retiradas se conservan y se recuperan al reasignar, sin duplicar claves |
| AuditLogs | Historial de solo consulta. Sus registros no entran en la papelera ni admiten DELETE |

La restauración vuelve a validar referencias, finca, capacidad, fechas, retiro farmacológico e identidad de la operación. Puede requerir restaurar primero una dependencia y puede rechazarse por las condiciones actuales. No restaura dependientes automáticamente. Los identificadores, SKU y combinaciones únicas permanecen reservados mientras la fila está archivada; reutilizar un código requiere restaurar y corregir su registro.

ManagementRepository registra Archived/Restored con snapshots anteriores y posteriores, autor, fecha y origen de conexión, en la misma transacción. Fotografías y asignaciones conservan sus eventos específicos. Se retiró la limpieza SQL física de fixtures: las pruebas archivan sus registros; la base y los archivos de pruebas se retiran al eliminar únicamente el proyecto Compose desechable. Una subida fallida puede retirar un archivo temporal nunca asociado a un registro persistido.

La protección de AppDbContext convierte Remove en archivado y cubre ambas variantes de SaveChanges/SaveChangesAsync; rechaza la eliminación persistente no contemplada y el DELETE de AuditLogs. Esto protege escrituras de la aplicación, no SQL externo ejecutado por un propietario de la base. La migración SoftDeletionAndBcvReferences añade columnas e índices sin reiniciar datos ni borrar tablas.

## Cotización actualizada

Se eligió [BCV Today](https://bcv.today/en/api/), proveedor público sin clave que replica la referencia del BCV. Es un servicio de terceros, no una API oficial operada por el Banco Central. La integración utiliza HttpClient del backend y no expone claves ni depende de una librería JavaScript adicional.

La API consulta primero el archivo del día en Caracas, api/v1/history/YYYY-MM-DD.json; si aún no existe, consulta api/v1/rate.json. Usa USD, effective_date y updated_at. Valida valor positivo, precisión de hasta seis decimales, fecha efectiva no futura, publicación no futura y antigüedad máxima de siete días. Permite la fecha efectiva del último día hábil durante fines de semana. Timeout: ocho segundos; tamaño máximo de respuesta: 64 KiB.

Compose activa BcvSync__Enabled mediante BCV_AUTO_SYNC=true por defecto. El servicio consulta al arrancar y cada treinta minutos. Admin también puede solicitar una actualización desde Dashboard/Inventario; el proceso serializa solicitudes para evitar superposición. Cada referencia nueva conserva fuente exacta, fecha efectiva, publicación del proveedor, creación y origen automatic. No se duplican referencias automáticas con igual fuente/fecha/valor. Las correcciones manuales siguen disponibles y se identifican como manual; no se sobrescribe una corrección posterior al volver a consultar un valor automático ya guardado.

| Endpoint | Acceso | Resultado |
| --- | --- | --- |
| GET /api/exchange-rates/usd-ves | Admin + products.list | Referencia guardada para una fecha pasada/actual en Caracas; 204 si no existe |
| GET /api/exchange-rates/usd-ves/status | Admin + products.list | Estado de sincronización del proceso actual |
| POST /api/exchange-rates/usd-ves/sync | Admin + products.update | Recupera y registra referencia válida; 503 RFC 7807 si falla el proveedor |
| POST /api/exchange-rates/usd-ves | Admin + products.update | Entrada manual fechada e idempotente, con auditoría |

Si el proveedor falla o devuelve datos inválidos, la tasa guardada no se borra. La UI muestra el fallo, la última consulta correcta y permite reintentar o registrar manualmente. El estado de intentos vive en el proceso; las referencias e historial permanecen en PostgreSQL. Sin referencia previa se mantiene USD, sin inventar una tasa.

La conversión es valor USD × bolívares por USD. Sirve para revisar costo y valor de referencia de insumos, presupuestar reposiciones y visualizar categorías en moneda local; no representa ventas, ganancias ni contabilidad realizada. Los importes se muestran con dos decimales y la tasa conserva seis.

## Decisiones frente a las fuentes del profesor

- JWT ya sigue REAF: access token en memoria; persistencia con renovación en cookie HttpOnly/SameSite y CSRF. No utiliza localStorage para tokens. localStorage conserva únicamente el tema. No fue necesario cambiar el esquema de sesión.
- Azul UNET aparece literalmente en **Fase 4/03_Tarea_Asignacion_Fase4_y_Rubrica.docx**: “ThemeContext.jsx: Implementar el selector de tema institucional de la UNET (Azul UNET #003366 y Modo Oscuro persistente en localStorage).” La paleta ganadera se conserva por decisión expresa del usuario; se documenta la diferencia de marca y se mantienen temas y contraste.
- La asignación anterior de Fase 3 permitía crear productos al empleado. La decisión actual reserva el catálogo compartido, precios y condiciones de retiro a Admin/Administrador. Employee opera animales, pesajes, atención y existencias de las fincas asignadas. Es una adaptación autorizada del dominio, no un conflicto entre el frontend y la API.

Estas decisiones se deben explicar en la defensa. No equivalen a un porcentaje de cumplimiento ni a una aprobación del profesor. El instrumento del 9 de octubre exige un nivel razonable de cobertura; no fija 100% de líneas o ramas.

## Cierre de entrega

El software y la evidencia permiten demostrar SPA React 18, integración REST protegida, cuatro KPI del dashboard, rendimiento observado y pruebas de frontend/xUnit/Moq. El equipo debe ensayar en el computador/red de presentación, revisar el PR, entregar el enlace/formulario de Classroom y realizar la sustentación. HTTP local no acredita HTTPS ni accesibilidad desde otro computador. Las muestras locales y las pruebas automatizadas no certifican rendimiento de toda la población ni cobertura completa.
