# Fase 4: ficha del animal y pesaje consecutivo

Este segundo incremento conecta el registro y la edición del animal, sus fotografías privadas y el seguimiento de peso con la SPA. Desarrolla la curva de GDP, la carga con compresión y el pesaje consecutivo descritos para el grupo 3 en los [lineamientos del profesor](https://gramirezsunet.github.io/desarrolloAplicacionesWeb/REAF-F4/).

El tercer incremento amplía esta ficha con historial clínico, retiro, reproducción, producción y árbol genealógico. Sus reglas se explican en [sanidad y reproducción](fase4-sanidad.md); el alcance restante está en [estado de implementación](fase4-estado.md).

## Registro y edición

El formulario utiliza los catálogos de la API. No requiere copiar identificadores:

- La finca determina los lotes y potreros disponibles.
- La especie determina las razas y los lotes compatibles.
- Elegir un lote propone su potrero asignado.
- La búsqueda de progenitores utiliza finca, especie y sexo; excluye el propio animal y pagina los candidatos en el servidor.
- Al editar se conservan identificación oficial, RFID, genealogía, ubicación y observaciones, aunque solo se modifique el nombre.
- La finca permanece fija en la edición. Un traslado requiere su propio proceso.

Estas selecciones facilitan la captura, pero la autorización y las reglas se vuelven a comprobar en la API. Se mantienen las restricciones existentes de actividad de catálogos, identificación única por finca, sexo y edad de progenitores, ausencia de ciclos y conservación de referencias productivas.

Se agrega una regla de coherencia: cambiar el nacimiento no puede dejar un pesaje histórico anterior a la nueva fecha. Corregir el nacimiento a otra fecha que respete el historial sigue siendo posible si las restantes reglas lo permiten.

Los errores de validación se asocian al campo. Los errores de negocio se muestran junto al formulario y mediante notificaciones. Un fallo conserva los valores. Cancelar un formulario modificado solicita confirmación; cerrar o recargar su página utiliza la advertencia del navegador. Esto no equivale a un borrador persistido ni a un bloqueo de todas las rutas internas de la SPA.

## Fotografías

El cliente admite JPEG, PNG y WebP de hasta 20 MB, prepara una copia JPEG y limita su lado mayor a 1920 píxeles. Se utiliza Canvas con calidad 0,82 y fondo blanco para transparencias. Las imágenes de más de 40 megapíxeles se rechazan después de decodificar. El resultado debe cumplir el límite de 5 MB de la API.

La vista previa muestra el tamaño original y el de la copia. Una fotografía pequeña no necesariamente reduce su tamaño al cambiar de formato. La preparación evita conservar los metadatos del archivo original, pero no impide que la propia imagen revele información.

La API conserva sus comprobaciones de tipo, tamaño y firma. Las fotografías se consultan con autorización y se presentan mediante URLs Blob. La galería muestra fecha y orden de carga. Eliminar requiere confirmación, permiso photos.delete y rol administrativo tanto en la interfaz como en la API.

## Evolución de peso y GDP

La ganancia diaria se calcula en Core.Application:

    GDP = (peso de la fecha actual - peso de la fecha anterior) / días entre fechas

Se usan fechas distintas y decimales; el resultado se redondea a cuatro decimales. Un primer pesaje no tiene GDP. Una disminución de peso produce un valor negativo, que se conserva.

Cuando existen varias mediciones de un mismo día, todas permanecen en el historial. El registro creado más recientemente representa esa fecha; un empate se resuelve por identificador. El listado, la ficha y la curva utilizan el mismo criterio para el último peso.

La API entrega como máximo 60 puntos diarios. Calcula también el intervalo anterior al primer punto visible cuando existe. El historial acepta page y pageSize, con tamaño predeterminado 20 y máximo 100; devuelve conteo y registros de esa página. La paginación se resuelve en el servidor. La lectura actual materializa los pesajes del animal para calcular la serie; con volúmenes mayores, la agregación y la lectura pueden optimizarse en PostgreSQL sin cambiar el contrato.

Recharts presenta la curva de peso y la de GDP, tooltips y acceso mediante teclado. Los controles anterior/siguiente muestran la fecha, el peso y la GDP del punto seleccionado. La tabla permite revisar los registros que sustentan la curva. El peso al nacer se presenta como característica de la ficha y no se añade como un pesaje inventado.

El aviso de bajo rendimiento requiere que el usuario defina un objetivo de GDP. No se inventa un umbral universal: su pertinencia depende del manejo y del animal. El objetivo de esta pantalla no se guarda todavía como política de finca y no produce alertas globales ni notificaciones en segundo plano.

## Registro individual y consecutivo

La SPA usa POST /api/animals/{id}/weights para ambos recorridos. El servidor deriva la finca del animal, comprueba acceso y permisos, permite nuevos registros solo para animales activos y reutiliza las reglas de fecha, peso, condición corporal y sacrificio del servicio de pesajes.

Cada envío incluye un SubmissionId generado en el cliente. Se guarda como identidad del WeightRecord mediante un constructor que conserva la inmutabilidad del identificador. No se necesita una columna ni una tabla nueva.

Una repetición del mismo identificador, autor y contenido devuelve el registro existente. Un identificador usado con otro contenido o autor produce 409. La creación devuelve 201; una confirmación repetida devuelve 200. La transacción serializable y la clave primaria evitan dos registros con el mismo identificador. Las operaciones concurrentes pueden obtener 409 y confirmar después mediante un reintento.

Si se pierde la respuesta y no puede confirmarse el resultado, el formulario bloquea la edición de los valores y conserva el identificador. Reintentar comprueba ese mismo envío. No se permite avanzar ni omitir durante un envío pendiente o incierto.

En el pesaje consecutivo:

1. El usuario elige la finca y, opcionalmente, el lote; busca y selecciona animales activos.
2. La selección se conserva entre páginas y búsquedas, hasta 50 animales.
3. Se registra un animal por vez. La cola avanza únicamente tras confirmar la respuesta del servidor.
4. La fecha se conserva para el siguiente animal y el campo de peso recibe el foco.
5. Omitir requiere confirmación y no crea un peso.
6. El cierre muestra registros confirmados y animales omitidos.

Cada pesaje es una operación independiente. Un fallo posterior no revierte los animales ya pesados. La cola pendiente y los campos no se guardan como un borrador entre recargas; los pesos confirmados sí permanecen en PostgreSQL.

Las rutas CRUD anteriores de /api/weights conservan el contrato de registros históricos. La restricción de animal activo corresponde al nuevo recorrido operativo, que evita pesar en manga a un animal dado de baja. La identificación de un envío no es una deduplicación permanente después de que un administrador elimine o modifique el registro.

## Demostración

Registrar un ejemplar con identificación y genealogía; modificar solo su nombre y comprobar que las demás características se mantienen. Preparar y subir una fotografía grande; revisar el tamaño, su acceso privado y la confirmación de eliminación.

Registrar pesos en dos fechas distintas, cambiar a GDP y consultar los puntos. Añadir un segundo pesaje de una misma fecha y comprobar que se conserva el anterior en la tabla. Definir un objetivo y revisar el aviso.

Seleccionar dos animales para el pesaje consecutivo, provocar un error de valor y guardar ambos sin recargar. Las pruebas automatizadas añaden el caso de una respuesta perdida después de guardar y la concurrencia real contra PostgreSQL. Los resultados y comandos están en [verificación](fase4-verificacion.md).
