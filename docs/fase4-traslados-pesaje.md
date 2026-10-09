# Traslados de grupos y selección completa en Pesaje

## Uso

El módulo **Traslados** (/transfers) está en la navegación principal; en móvil se encuentra en **Más** cuando no está entre los cuatro accesos visibles. Requiere consulta de animales, fincas, lotes y potreros, y actualización de animales. Admin y Employee pueden usarlo cuando cuentan con esos permisos y con acceso a la finca.

Selecciona una finca, filtra por **Lote de origen** o busca por arete/nombre/identificación. Puedes marcar animales individuales o usar **Seleccionar todos los resultados**. La selección completa consulta todas las páginas, conserva selecciones previas de la misma finca y no agrega duplicados. Cambiar de finca limpia la selección; cambiar un filtro cancela una selección completa que siga en curso.

Después elige el potrero de destino y escribe el motivo. El lote de cada animal se conserva por defecto; puedes elegir un lote compatible con la especie de todos los seleccionados o retirar la asignación de lote. Los animales que ya tienen ese potrero y lote se excluyen explícitamente. Se muestra la ocupación prevista, incluyendo solamente las nuevas entradas físicas. El servidor vuelve a validar la capacidad y las ubicaciones.

El botón de confirmación guarda el grupo completo y muestra los animales confirmados con acceso a su historial. El límite es 500 movimientos efectivos por operación; grupos mayores requieren dividir la selección y el formulario lo indica antes de confirmar. La fecha sigue siendo la fecha actual registrada por el servicio de movimientos existente.

En **Pesaje**, el mismo botón selecciona todos los resultados de la búsqueda aplicada, incluyendo otras páginas. También hay **Limpiar selección**. Se retiró el límite de 50 de la cola local; cada peso se sigue enviando y confirmando individualmente, sin inventar mediciones ni guardar por seleccionar.

## API y garantías

POST /api/animal-movements/batch recibe farmId, toPaddockId, reason, changeLot, toLotId y animals. Cada miembro lleva animalId, submissionId y las ubicaciones de origen esperadas. El listado paginado de animales ahora incluye lotId y paddockId para capturar esas referencias al seleccionar.

La operación reutiliza las reglas del traslado individual dentro de una sola transacción Serializable: finca accesible/activa, animal activo, destino válido, lote compatible, capacidad y comparación del origen esperado. Un fallo revierte las ubicaciones, movimientos y auditorías del grupo. Cada animal conserva un registro con autor, fecha, motivo y origen/destino; no se modifica la referencia de potrero del catálogo del lote.

Los identificadores de envío pertenecen a cada movimiento y permiten repetir la misma solicitud sin duplicar registros, incluso si el animal tuvo otro traslado después. Una respuesta de red incierta bloquea el borrador y reintenta el mismo contenido. Un conflicto confirmado conserva la selección y permite actualizar para seleccionarla nuevamente.

No se añaden entidades ni migraciones. La paginación de la selección no constituye una instantánea bloqueada: si cambia la cantidad o se repiten filas durante la lectura se rechaza la selección incompleta, y las ubicaciones vuelven a comprobarse al guardar. La capacidad mostrada es informativa; la transacción del servidor es la autoridad.

## Verificación

[Evidencia y resultados](evidence/phase4-transfers/README.md). Los recorridos que crean y trasladan animales utilizan PostgreSQL desechable, separado de la demostración local.
