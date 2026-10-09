# Dashboard y gestión visible de catálogos

Actualización del 8 de octubre de 2026. [Evidencia y límites](evidence/phase4-dashboard-management/README.md).

## Cambios de interacción

Dashboard mantiene únicamente un resumen de finca/período y el botón **Filtros**. El botón abre un panel modal lateral con los controles; aplicar confirma el borrador y actualiza la API, mientras cancelar o Escape conserva el período aplicado. Al cerrar se recupera el foco del botón. Los filtros no permanecen ocupando espacio en escritorio ni móvil. Las consultas en curso muestran el estado de actualización sin inventar datos.

Las cuatro KPI usan una cuadrícula de dos columnas en móvil, valores legibles, descripciones breves con su población y accesos a la siguiente acción. Leche usa azul informativo; peso usa tierra; preñez usa verde; stock crítico usa atención cuando corresponde. El hover y la entrada son transiciones breves, desactivadas con prefers-reduced-motion. No se generan tendencias o comparaciones sin registros.

Preñez y fertilidad tienen bloques separados con un indicador circular basado en el porcentaje real de la API. Se conservan numerador, denominador y pendientes. Ausencia de evaluación se muestra como ausencia de datos, no como 0%. Los detalles del cálculo y los registros de respaldo permanecen desplegables.

## Dónde se gestiona cada recurso

| Recurso | Acceso en la SPA | Gestión disponible |
| --- | --- | --- |
| Fincas | Fincas y catálogos → Fincas; /management?tab=farms | Crear, consultar/buscar, editar contacto/estado y archivar |
| Especies | Fincas y catálogos → Especies; /management?tab=species | Crear, editar código/propósito/gestación/estado y archivar |
| Razas | Fincas y catálogos → Razas; /management?tab=breeds | Crear, editar especie/propósito/origen/estado y archivar |
| Lotes | Fincas y catálogos → Lotes; /management?tab=lots | Crear, editar agrupación y potrero de referencia, archivar |
| Potreros | Potreros; /paddocks | Crear/editar espacio, límites/plano, consultar residentes y archivar |
| Categorías, productos y existencias | Inventario; /inventory | Pestañas existentes, edición y archivado; saldos mediante movimientos trazables |
| Animales | Animales; /animals | Ficha y editor, registro y archivado administrativo |
| Pesajes y crecimiento | Pesaje y ficha → Crecimiento | Captura consecutiva, historial y curva; objetivos en Seguimiento |
| Sanidad, reproducción y producción | Pestañas de la ficha del animal | Registro y consulta de eventos, reglas sanitarias y estado reproductivo |
| Fotografías y genealogía | Pestañas de la ficha del animal | Fotografías privadas/archivado; parentesco navegable |
| Traslados y ocupación | Traslados (/transfers), ficha → Ubicación y detalle de Potreros | Traslado individual o de grupos/lotes, registro de permanencia; la referencia del lote no mueve animales automáticamente |
| Personas y acceso | Usuarios; /users | Cuentas, roles, fincas asignadas, contraseña y desactivación |
| Recuperación e historial | Papelera y Auditoría | Restauración validada y consulta de cambios; sin borrado permanente |

Fincas, especies, razas y lotes existían en la API y en selectores, pero carecían de un gestor visible. El nuevo módulo cierra esa brecha. Potreros tenía alta/edición sin botón de archivado; ahora expone esa acción. No se crea una pantalla por tabla interna de Identity ni se permite alterar libremente historiales clínicos o movimientos.

## Permisos y conservación

El nuevo módulo requiere administrador y el permiso list de cada catálogo; create/update/delete controlan sus acciones. Los errores HTTP y de campo permanecen junto al borrador. Los formularios protegen cambios sin guardar. Los catálogos relacionados se cargan antes de habilitar edición; el potrero de referencia de un lote se limita a su finca. La finca de un lote existente no se cambia mediante este editor.

Archivar exige confirmación, conserva la fila y permite recuperación desde Papelera. La API sigue bloqueando dependencias activas; el frontend explica cómo resolverlas. Se reutilizan las APIs y permisos existentes: no hay una migración nueva ni cambios de autenticación.

La búsqueda y paginación de estos cuatro catálogos se realizan localmente sobre sus endpoints de lista existentes; no se presentan como paginación de servidor. Las listas extensas de animales, inventario y eventos conservan sus consultas paginadas existentes.
