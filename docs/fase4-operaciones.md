# Inventario, dashboard y reportes de Fase 4

## Propósito y uso

La SPA incorpora /inventory, /dashboard y /reports. Inventario ofrece existencias por finca, búsqueda de productos, mínimos y máximos, ubicaciones, catálogo de productos y categorías. Los editores y el historial se abren en un panel lateral. El administrador mantiene productos/categorías; el empleado consulta el catálogo y registra entradas/salidas según sus permisos. Las eliminaciones administrativas requieren confirmación y las referencias existentes pueden impedirlas.

El dashboard exige rol Admin/Administrador y todos los permisos de sus fuentes, tanto en la navegación como en la API. Incluye valoración a costo y precio de referencia, límites, rotación, leche, peso bovino y diagnósticos reproductivos. Consulta los datos al aplicar filtros y cada 60 segundos mientras la página está visible. Es actualización por consultas periódicas, sin WebSocket. Los gráficos Recharts tienen tablas equivalentes para consultar sus valores.

Reportes ofrece consulta paginada de historial clínico y producción por período y finca, y exportación de todas las filas filtradas a XLSX/PDF. Las exportaciones usan una lectura PostgreSQL RepeatableRead para mantener el mismo conjunto durante el conteo y la consulta. El límite es 10.000 filas y se rechaza el exceso; no se entrega un archivo truncado. El período máximo es 367 días inclusive. No es una exportación de todas las propiedades de la ficha: conserva identificación del animal, fecha, tipo, producto actual cuando aplica, cantidad/unidad, notas, detalle y retiro registrado.

## Reglas de inventario

- Cada registro corresponde a una combinación única de finca y producto. Las cantidades usan la unidad de su producto; no se convierten unidades automáticamente.
- POST /api/inventory/{id}/movements admite entradas y salidas positivas, motivo obligatorio y saldo esperado. Una salida puede asociarse a un animal activo de la misma finca. Los eventos clínicos no descuentan inventario automáticamente porque la dosis clínica no tiene unidad persistida y no permite una conversión fiable.
- La escritura serializable confirma el cambio de saldo, el movimiento y su auditoría en una transacción. Un saldo obsoleto o insuficiente produce 409. Un UUID de envío repetido con la misma operación, autor y destino confirma el registro existente. El saldo esperado es una precondición del primer intento; no se vuelve a exigir al confirmar una operación ya guardada.
- El cliente conserva un envío de resultado incierto y lo reintenta sin duplicarlo. Mientras está pendiente, bloquea los campos. Después de un conflicto de saldo ofrece actualizarlo conservando cantidad y motivo.
- El primer movimiento crea un saldo inicial trazable. No se reconstruye el pasado del inventario anterior. Los movimientos nuevos usan la fecha civil UTC, coherente con los flujos existentes; no hay configuración de zona horaria por finca.
- Un inventario trazado conserva su saldo mediante movimientos; PUT solamente permite mantener el saldo y editar límites/ubicación. No se elimina un inventario trazado ni una finca con movimientos; el producto puede desactivarse. Un animal con salidas asociadas se conserva para mantener el vínculo del historial.

Se amplió StockMovements.Quantity de numeric(12,3) a numeric(14,4) mediante 20261008010000_StockMovementPrecision para coincidir con FarmInventory.Stock. La migración amplía capacidad y precisión sin eliminar datos. No se añadieron ni eliminaron tablas. El esquema y los diagramas publicados se actualizaron. La reversión que reduciría precisión está bloqueada; para regresar a un estado anterior se requiere una copia de seguridad compatible.

## Definición de indicadores y límites

| Indicador | Cálculo y alcance |
| --- | --- |
| Costo actual | Suma de stock actual × CostPrice del catálogo |
| Valor de referencia actual | Suma de stock actual × Price; no son ingresos ni utilidad realizada |
| Moneda | USD como convención de referencia del catálogo; el modelo no almacena moneda ni cambio histórico |
| Existencias críticas/excesivas | Stock ≤ MinStock y Stock ≥ MaxStock, incluyendo los límites |
| Rotación | Salidas registradas / ((saldo al inicio + saldo al cierre) / 2). Se reconstruyen saldos con movimientos posteriores al saldo inicial trazable |
| Historia suficiente | El saldo inicial debe ser anterior al primer día del período, cubrirlo y reconciliar con el saldo actual. Su propio día puede estar incompleto. Ajustes no interpretables, saldo medio cero o inconsistencia devuelven null |
| Leche | Solamente cantidades Milk/Liter. Las unidades incompatibles se excluyen y se informa su número |
| Leche por lote | Agrupa a los animales por su finca y lote actuales. No atribuye ordeños pasados a una ubicación histórica que no está persistida |
| Peso por edad | Último pesaje de cada bovino BO en el período, desempate por CreatedAt/Id y edad en la fecha del pesaje; fecha de nacimiento desconocida/inválida excluida con conteo visible |
| Diagnósticos positivos | Positivos / (positivos + negativos). Los inciertos se muestran y excluyen del denominador; cero concluyentes produce null. No es tasa de concepción por servicio |
| Partos | Partos registrados, crías vivas = OffspringCount − StillbornCount y mortinatos del período |

La valoración es una fotografía actual aunque el usuario seleccione otro período para movimientos/producción. Se evita presentar esa valoración como inventario histórico. No se suman cantidades de insumos de distintas unidades para producir una rotación global artificial.

## Exportación y dependencias

ExcelJS 4.4.0 produce un libro con encabezados, filtros, panel inmovilizado, fechas y cantidades tipadas. El texto recibido se escribe como texto, sin crear fórmulas. jsPDF 4.2.1 y AutoTable 5.0.8 generan PDF apaisado con encabezados repetidos y paginación. Noto Sans con su licencia se utiliza exclusivamente dentro del PDF; la fuente de la interfaz sigue la decisión existente. Se verificaron acentos españoles en ambos formatos.

Los exportadores y gráficos se cargan bajo demanda. ExcelJS genera un chunk de aproximadamente 930 kB antes de compresión: existe un aviso de tamaño de Vite, pero no se descarga al abrir inventario o iniciar sesión. La aplicación no incorpora las bibliotecas de UI propuestas en la investigación anterior. Se fijó un override de uuid 11.1.1 para la dependencia v4 de ExcelJS; las exportaciones pasaron y npm audit del frontend informa cero vulnerabilidades conocidas en el árbol instalado. Esto no constituye una auditoría de seguridad completa.

Referencias técnicas: [ExcelJS](https://github.com/exceljs/exceljs), [jsPDF](https://github.com/parallax/jsPDF), [AutoTable](https://github.com/simonbengtsson/jsPDF-AutoTable) y [Noto Fonts](https://github.com/notofonts/noto-fonts).

## Verificación y entrega

Los resultados ejecutados están en fase4-verificacion.md. InMemory verifica autorización y contratos HTTP; la carrera por el saldo y la idempotencia tras perder la respuesta se ejecutan sobre PostgreSQL físico. No se ha medido rendimiento con el límite de 10.000 filas ni cobertura porcentual. El avance funcional no equivale al cierre de la entrega: quedan revisión final de rúbrica, evidencia seleccionada, recorrido de demostración y defensa/quiz.
