# Estado de implementación de Fase 4

La rama codex/phase4-cattle-spa incluye sesión persistente, gestión del animal, sanidad/reproducción/producción, pesaje, potreros y traslados, sistema visual, inventario, dashboard y reportes. **La auditoría y la evidencia seleccionada ya están documentadas. Persisten brechas específicas del grupo, el primer arranque automatizado, la publicación y la preparación de la defensa.**

| Criterio | Implementado | Trabajo pendiente |
| --- | --- | --- |
| SPA React, Vite y TypeScript | React 18, Tailwind, navegación, módulos del animal, potreros, inventario, dashboard y reportes; cargas, errores, toasts y diseño móvil | Primer arranque automatizado y recorrido de entrega |
| AuthContext | Cookie persistente, JWT en memoria, renovación, logout, usuario, roles, permisos y autorización de los nuevos flujos | Defensa de la alternativa de sesión y evidencia final |
| ThemeContext y UI | Paleta semántica de granja en ambos temas, preferencia persistida, controles compartidos e iconos SVG; ocho pestañas reales y paneles laterales | Auditoría completa de accesibilidad; Source Sans 3, Phosphor, selector Radix y mejoras del login aplicados |
| Dashboard | Valoración, mínimos/máximos, rotación condicionada a historial suficiente, leche por finca/lote actual, curva diaria de leche por lote actual, peso por edad al pesaje y diagnósticos con denominadores; oculto y prohibido para Employee | Ensayo y explicación de los límites históricos |
| UnitTests y Moq | 122 pruebas aisladas, cobertura medida y reporte Passed; Core.Application con catálogo, sanidad, GDP, idempotencia, retiro, ubicación, stock y métricas | Ampliar cobertura aislada de rutas hoy cubiertas por integración según su prioridad |
| Integración | PostgreSQL, API y SPA; consultas paginadas, transacciones serializables, exportaciones con lectura consistente y RFC 7807 | Primer arranque y actualización de Postman |
| Ficha 360 del grupo 3 | Identificación, ubicación/traslados, genealogía, notas, galería, sanidad, retiro, reproducción, producción y curva de peso/GDP | Corrección de eventos desde UI como ampliación posterior; ensayo de la ficha |
| Bajo rendimiento | Aviso automático de descenso real de peso con fechas y comparación de GDP con objetivo explícito | Política por finca/animal y alertas persistentes generales |
| Potreros y lotes | Ocupación, capacidad, densidad, permanencia conocida, residentes y filtros; traslados con autor, origen esperado y motivo | Plano espacial y semáforo por días de permanencia; rotación y traslado masivo como ampliaciones |
| Inventario | UI de productos, categorías, existencias, límites/ubicación y entradas/salidas auditadas; vínculo opcional al animal | Lotes farmacológicos/caducidad y conversión de consumo clínico requieren un flujo específico |
| Reportes y fotografías | XLSX/PDF de sanidad y producción; imágenes privadas, compresión, vista previa y eliminación administrativa confirmada | Seleccionar evidencia final; no se ha validado rendimiento de exportación con 10.000 filas |
| Evidencia | Pruebas automatizadas, cobertura y capturas seleccionadas, documentación de decisiones y limpieza de fixtures | Publicación, ensayo, defensa y preparación del quiz |

Se conserva el dominio ganadero y su modelo. Productos, lotes farmacológicos, proveedores, semen y alimentación mantienen su utilidad. Una tabla sin interfaz no se presenta como módulo terminado ni se elimina por ese motivo. La rama amplió la precisión de las cantidades de movimientos; permanecen 43 tablas. La revisión actual no añade una migración.

Las decisiones y límites están en [animales y pesaje](fase4-animales.md), [sanidad y reproducción](fase4-sanidad.md), [potreros y traslados](fase4-potreros.md), [interfaz de la finca](fase4-ui.md), [pulido de UI](fase4-ui-pulido.md), [inventario, dashboard y reportes](fase4-operaciones.md) y [sesión persistente](fase4-sesion.md). Los resultados ejecutados están en [verificación](fase4-verificacion.md).

La [auditoría de entrega](fase4-auditoria.md) contrasta la rúbrica de 80 puntos, el checklist transversal y las recomendaciones del Grupo 3. Los [reportes seleccionados](evidence/phase4-audit/README.md) distinguen Passed, cobertura y pruebas físicas de PostgreSQL.
