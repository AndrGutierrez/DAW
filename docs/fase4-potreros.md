# Potreros, capacidad y traslados

Este incremento utiliza Paddock, Lot, Animal y AnimalMovement del modelo existente. No agrega tablas ni cambia el esquema. Su propósito es convertir la ubicación del ganado en una operación verificable: ocupación real, límite de capacidad y motivo de cada traslado.

## Qué significa la ocupación

La ocupación cuenta animales con Status = Active y Animal.PaddockId igual al potrero consultado. Un animal vendido, fallecido, transferido o extraviado no consume un cupo operativo.

Animal.LotId indica su grupo productivo. Lot.PaddockId es una referencia organizativa; no sustituye la ubicación física ni mueve automáticamente sus integrantes. Una asignación implícita confundiría el número de animales presentes. El formulario de traslado permite elegir ambas referencias explícitamente.

La capacidad es el máximo de animales configurado por la finca. La superficie se expresa en hectáreas. La densidad se calcula como ocupación / superficie y conserva decimales; no se calcula cuando falta la superficie.

## Mapa y permanencia

/paddocks presenta un mapa esquemático de tarjetas seleccionables. La búsqueda por nombre/código, la finca, el estado y la paginación se aplican en PostgreSQL antes de materializar los resultados. Los residentes se consultan en otra página del servidor, de diez animales. Sus lotes presentes se muestran como controles seleccionables con recuento; filtrar por lote o por animales sin lote conserva la restricción del potrero antes de contar y paginar.

El indicador usa texto y color: disponible por debajo del 80 %, advertencia desde el 80 %, completo al 100 % y excedido por encima del máximo. Sin capacidad configurada se muestra una condición desconocida. El 80 % es un aviso visual de margen operativo; el bloqueo utiliza solamente el máximo configurado.

La permanencia corresponde a días transcurridos desde la última entrada física registrada. Cambiar únicamente de lote no reinicia esa fecha. Una nueva ubicación asignada desde el registro/edición de la ficha también produce AnimalMovement con fecha de registro, motivo y autor.

La mayor permanencia mostrada en la tarjeta utiliza las entradas conocidas. Los animales sin entrada registrada se cuentan por separado; no se inventa una fecha a partir de Animal.UpdatedAt, del nacimiento o del potrero de referencia del lote. Cero días significa una entrada registrada hoy. Las fechas de estos nuevos traslados se toman del día UTC del servidor.

## Regla del servidor

La comprobación comparte AnimalLocationPolicy entre la creación/edición original de animales y el nuevo traslado. Así, cambiar de pantalla o llamar directamente a la API no evita el límite.

Antes de guardar se valida finca, especie del lote, referencias activas y capacidad. El recuento excluye al mismo animal durante la edición y cuenta una reactivación como ocupación activa. Un destino completo rechaza la operación con 409 y RFC 7807.

PaddockDefinition rechaza una capacidad menor que la ocupación actual y la desactivación de un potrero con animales activos. El mantenimiento usa los permisos existentes: Admin y Employee pueden crear/editar potreros en las fincas a las que tienen acceso. El DELETE administrativo conserva la restricción de rol y permiso; Employee recibe 403. No se reducen sus permisos operativos para acomodar la interfaz.

AnimalMovementService exige un animal activo, finca accesible/activa y un motivo. Deriva animal, finca, autor y fecha; recibe la ubicación de origen esperada y rechaza un formulario obsoleto si otro usuario la cambió. La interfaz permite actualizar la ubicación antes de volver a capturar el traslado.

El cambio del animal, el historial y la auditoría se guardan en una transacción serializable. Dos llegadas al último cupo no pueden confirmarse juntas: una debe recibir 409. La escritura conserva las demás propiedades de la ficha.

## Reintentos

POST /api/animals/{id}/movements recibe SubmissionId. La primera escritura devuelve 201; repetir el mismo identificador, autor, origen, destino y motivo devuelve 200 sin duplicar el movimiento. Un contenido distinto devuelve 409.

Si se pierde la respuesta después de guardar, la interfaz bloquea el cuerpo pendiente y reenvía exactamente el mismo registro. Puede confirmar el movimiento aunque la ubicación actual ya haya cambiado. El identificador pendiente vive en la página; no se ofrece recuperación del formulario después de cerrar el navegador.

GET /api/animals/{id}/movements devuelve el historial paginado. GET /api/paddocks/page devuelve el mapa y GET /api/paddocks/{id}/residents los animales activos presentes. Todas las lecturas se limitan a fincas asignadas; un detalle ajeno devuelve 404.

## Cómo defenderlo y qué falta

La capacidad configurada permite controlar el límite operativo solicitado para evitar nuevas asignaciones excesivas. No representa una determinación agronómica automática: un animal cuenta como una cabeza, sin equivalencias por peso/especie, estimación de forraje, clima o disponibilidad de agua. La densidad por hectárea es descriptiva; la finca debe establecer una capacidad adecuada.

El mapa no representa coordenadas ni superficie geográfica a escala. Los días describen la permanencia individual registrada, no un historial continuo del suelo o una recomendación automática de descanso. No se incluyen traslado masivo de lotes, planificación de rotación o umbrales de permanencia configurables.

No hay edición ni eliminación pública individual del historial de movimientos. El DELETE administrativo del animal mantiene el comportamiento previo del modelo: sus movimientos se eliminan por la relación en cascada. Los registros de auditoría existentes permanecen; esta limitación debe considerarse antes de eliminar una ficha.

La verificación de concurrencia se realiza contra PostgreSQL físico, además de las pruebas aisladas de Application y del pipeline HTTP. Los resultados actuales están en [verificación](fase4-verificacion.md); las tareas restantes están en [estado](fase4-estado.md).

## Plano espacial y permanencia configurable — 8 de octubre

Cada potrero puede guardar MapX, MapY, MapWidth y MapHeight como porcentajes de un plano 100 × 100. La API exige los cuatro valores o ninguno, precisión de cuatro decimales y límites coherentes. El editor declara que es un esquema de distribución, no cartografía GPS ni medición de hectáreas. El seed nuevo incluye dos posiciones ilustrativas en la finca DEMO; actualizar una instalación existente conserva los potreros y no inventa sus posiciones.

Seleccionar una finca carga todos sus potreros activos configurados mediante GET /api/paddocks/map, incluso si las tarjetas están filtradas o en otra página. Color de relleno: capacidad actual; borde: permanencia frente a MaxStayDays. Con fecha conocida, menos del 80 % es verde, desde 80 % ámbar y desde el límite rojo. Sin fecha o sin límite, estado neutro. Las fechas desconocidas se informan aparte y nunca se infieren de una edición. Un borde verde solo describe las fechas conocidas.

El plano admite clic, Enter y Espacio y abre el mismo panel lateral de residentes/traslados. La geometría se configura numéricamente; no se implementó edición por arrastre, GIS, validación de solapamientos, recomendación agronómica de carga ni traslado masivo.
