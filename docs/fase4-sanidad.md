# Sanidad, reproducción y producción del animal

Este incremento conecta las entidades sanitarias y reproductivas existentes con la ficha React, y aplica el retiro farmacológico en Core.Application. Conserva el modelo ganadero; no elimina historia ni crea otra base paralela.

## Recorrido y utilidad

La ficha incluye accesos a Sanidad, Reproducción, Producción y Genealogía:

- Sanidad: tratamientos, vacunaciones, desparasitaciones, casos clínicos y cuarentenas; producto, dosis, vía, fechas, notas y costo según el tipo de evento. El historial identifica el producto y muestra el retiro registrado. La mortalidad histórica puede consultarse; no se añade un formulario de mortalidad en este incremento.
- Estado de salud: cambio explícito con motivo e historial. Registrar un evento no inventa una recuperación ni cambia automáticamente el estado clínico.
- Reproducción: celo, monta natural, inseminación, diagnóstico de gestación, parto, destete y aborto. Los selectores buscan reproductores o descendientes por finca, especie y relación con la madre.
- Producción: leche, carne, lana o huevos con fecha, cantidad y unidad. Las reglas existentes comprueban especie, sexo, compatibilidad de método/producto/unidad y operaciones de sacrificio.
- Genealogía: árbol con madre, padre y abuelos. Cada ascendiente abre su ficha para continuar la exploración; un registro no autorizado se muestra sin revelar sus datos.

Los historiales sanitarios, reproductivos y productivos usan Count/Skip/Take en la base, con fechas descendentes y desempate por CreatedAt/Id. El tamaño predeterminado es 10 y el máximo 100. Los cambios de estado de salud muestran los últimos 20, identificados como tales.

## Regla de retiro

El modelo mantiene un único período de retiro por producto/tratamiento. La aplicación utiliza ese valor para leche y sacrificio; no calcula prescripciones ni asigna períodos por medicamento.

Un tratamiento requiere producto activo, dosis, fecha inicial y última administración realizada. El período debe ser conocido: se propone el del catálogo, o se registra expresamente si el catálogo no lo tiene. Puede ampliarse, pero no reducirse por debajo del producto configurado. WithdrawalDays y WithdrawalEndDate quedan guardados en el tratamiento; cambiar el catálogo después no reduce el retiro ya registrado.

Política de fechas de esta aplicación:

1. La restricción comienza en la fecha del evento y cubre el curso registrado.
2. Último día restringido = última administración + días de retiro.
3. Ese día es inclusivo. La liberación comienza al día siguiente. Incluso un período de cero días restringe la fecha de administración.
4. Si coinciden tratamientos, prevalece la liberación más tardía. Un tratamiento histórico sin período ni fecha final conocidos bloquea la producción hasta una revisión; no se inventa una liberación a partir del catálogo actual.
5. Las fechas operativas se validan con el día UTC usado por la API.

Ejemplo de software: tratamiento del 1 al 3 de enero, retiro configurado de cinco días. Se restringe del 1 al 8 de enero inclusive y se permite la operación desde el 9. Este ejemplo explica el algoritmo; no prescribe un período para un medicamento real.

La validación ocurre en ProductionDefinition, dentro de la escritura serializable. Por tanto, protege tanto POST/PUT de /api/production como el nuevo recorrido por animal. Los registros retroactivos se evalúan por su fecha, no solo por el estado actual del día. Un tratamiento retroactivo que contradice leche o sacrificio ya registrados se rechaza con 409 y conserva la historia.

Una vacunación o desparasitación con producto cuyo retiro sea positivo o desconocido requiere primero un tratamiento de la misma administración, con producto y período conocidos. Esto evita evadir la restricción eligiendo otra clase de evento. Un producto con retiro explícito de cero días puede registrarse directamente en esos dos tipos de evento.

**Alcance del bloqueo:** no existe todavía un flujo de venta/disposición de leche con distinción entre consumo, descarte y otros destinos. La política actual bloquea el registro de leche durante el retiro y el sacrificio, como medida conservadora. No demuestra una integración con facturación, venta de leche ni trazabilidad de leche descartada. Un futuro flujo de disposición necesitará un campo persistente y reglas propias.

## Reproducción e identidad

Solo una hembra activa puede recibir un evento reproductivo. El reproductor pertenece a la misma finca y especie, es macho y su nacimiento precede al evento cuando se conoce. Un destete con cría identificada comprueba la madre, finca, especie y fecha de nacimiento.

El estado sigue el último diagnóstico de gestación, parto o aborto **por fecha del evento**, con desempate determinista. Una monta, inseminación o celo no confirma gestación. Un diagnóstico positivo puede guardar una fecha prevista indicada por el usuario; la aplicación no la convierte automáticamente en un parto confirmado.

El parto conserva crías totales, nacidas muertas y dificultad. No crea animales ni inventa sus progenitores: cada cría se registra posteriormente y se relaciona con la madre. El destete no modifica automáticamente la curva de pesaje; su peso pertenece al evento de destete.

Un animal con historia sanitaria o reproductiva no puede cambiar especie, sexo ni nacimiento mediante la edición general. Esta regla protege la coherencia de eventos ya registrados. La corrección formal de identidad y de eventos requiere otro recorrido de auditoría.

## Escritura, permisos y trazabilidad

Los eventos son registros históricos: se añaden y consultan, sin PUT/DELETE públicos en este módulo. El autor se toma de la sesión y la finca del animal, nunca del formulario. Las escrituras se ejecutan en la transacción serializable del repositorio y generan AuditLog.

Cada envío usa SubmissionId como identidad persistente. La repetición del mismo contenido devuelve 200 sin crear otra fila; un envío nuevo devuelve 201. Cambiar el contenido de un identificador guardado devuelve 409. En sanidad/reproducción también se comprueba el autor. La producción conserva el contrato previo sin columna de autor; su repetición exige igualdad del resultado y propiedad del animal.

Cuando se pierde la respuesta, el formulario bloquea los campos y conserva tanto el identificador como el cuerpo completo. El reintento usa ese cuerpo, incluso cuando los controles HTML están deshabilitados. Esto evita duplicar eventos por una desconexión. Una recarga todavía pierde ese envío pendiente en memoria; no hay borrador persistente.

Permisos nuevos: clinical.list/create y reproduction.list/create, junto con animals.get. La producción utiliza production.list/create. El acceso se restringe a fincas asignadas; una ficha ajena devuelve 404. El seeder incorpora los permisos a los roles previstos: Admin/Administrador tienen acceso; Employee y Veterinario pueden registrar sanidad/reproducción; SoloLectura consulta. No se introducen permisos de eliminación clínica.

Después de actualizar una instalación existente, ejecutar --seed para incorporar los permisos. No hace falta una migración de esquema para este incremento.

## Contratos nuevos

| Ruta | Operación |
| --- | --- |
| GET /api/animals/{id}/clinical?page=1&pageSize=10 | Historial, retiro actual y últimos cambios de salud |
| POST /api/animals/{id}/clinical | Evento sanitario |
| GET /api/animals/{id}/reproduction?page=1&pageSize=10 | Historial y estado reproductivo |
| POST /api/animals/{id}/reproduction | Evento reproductivo |
| GET /api/animals/{id}/production?page=1&pageSize=10 | Producción paginada del animal |
| POST /api/animals/{id}/production | Resultado productivo con envío idempotente |

El nuevo formulario registra un resultado principal por envío. La API original conserva OperationId para obtener varios productos del mismo sacrificio, como carne y piel. Swagger contiene los esquemas; los errores de validación y negocio siguen RFC 7807.

## Demostración y límites de entrega

1. Registrar un tratamiento conocido y mostrar producto, dosis, última administración y fecha de liberación.
2. Intentar leche durante el retiro: 409, mensaje en el formulario y ninguna producción nueva. Intentar sacrificio: confirmar la acción y comprobar que el rechazo conserva el animal activo.
3. Registrar producción después del retiro y comprobar cantidad/unidad en el historial.
4. Registrar un diagnóstico positivo y luego un parto: el estado pasa de gestación confirmada a parto registrado.
5. Abrir una cría y explorar madre y abuelos mediante el árbol.
6. Mostrar paginación real y un reintento tras perder la respuesta, con un único evento guardado.

No se implementan consumo automático de inventario, lotes farmacológicos, control de almacenamiento de semen, prescripción, revisión de tratamientos históricos incompletos ni corrección/reversión de eventos. Esas entidades siguen disponibles en el modelo; su presencia no equivale a un flujo terminado.

Este bloque satisface la parte clínica, reproductiva y de retiro de la ficha 360. Dashboard, mapa de potreros, alertas globales, inventario visual, exportaciones XLSX/PDF y evidencia final siguen detallados en [estado de implementación](fase4-estado.md).
