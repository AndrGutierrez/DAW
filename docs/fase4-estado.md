# Estado de implementación de Fase 4

La rama codex/phase4-cattle-spa incluye sesión persistente, gestión del animal, sanidad/reproducción/producción, pesaje, potreros y traslados, sistema visual, inventario, dashboard y reportes. **El cierre de la entrega todavía requiere auditoría final, evidencia y preparación de la defensa.**

| Criterio | Implementado | Trabajo pendiente |
| --- | --- | --- |
| SPA React, Vite y TypeScript | React 18, Tailwind, navegación, módulos del animal, potreros, inventario, dashboard y reportes; cargas, errores, toasts y diseño móvil | Auditoría y recorrido final |
| AuthContext | Cookie persistente, JWT en memoria, renovación, logout, usuario, roles, permisos y autorización de los nuevos flujos | Defensa de la alternativa de sesión y evidencia final |
| ThemeContext y UI | Paleta semántica de granja en ambos temas, preferencia persistida, controles compartidos e iconos SVG; ocho pestañas reales y paneles laterales | Auditoría completa de accesibilidad; fuentes/iconos externos siguen como investigación |
| Dashboard | Valoración, mínimos/máximos, rotación condicionada a historial suficiente, leche por finca/lote actual, peso por edad al pesaje y diagnósticos con denominadores; oculto y prohibido para Employee | Evidencia y explicación de los límites históricos |
| UnitTests y Moq | Proyecto separado con referencia a Core.Application; sanidad, GDP, idempotencia, retiro, ubicación, stock y métricas | Cobertura porcentual real y evidencia seleccionada |
| Integración | PostgreSQL, API y SPA; consultas paginadas, transacciones serializables, exportaciones con lectura consistente y RFC 7807 | Recorrido final completo |
| Ficha 360 del grupo 3 | Identificación, ubicación/traslados, genealogía, notas, galería, sanidad, retiro, reproducción, producción y curva de peso/GDP | Corrección auditada de eventos y evidencia final |
| Bajo rendimiento | Aviso al comparar la última GDP con un objetivo explícito en la ficha | Política por finca/animal y alertas persistentes generales |
| Potreros y lotes | Ocupación, capacidad, densidad, permanencia conocida, residentes y filtros; traslados con autor, origen esperado y motivo | Planificación de rotación, umbrales de permanencia y traslado masivo de lotes |
| Inventario | UI de productos, categorías, existencias, límites/ubicación y entradas/salidas auditadas; vínculo opcional al animal | Lotes farmacológicos/caducidad y conversión de consumo clínico requieren un flujo específico |
| Reportes y fotografías | XLSX/PDF de sanidad y producción; imágenes privadas, compresión, vista previa y eliminación administrativa confirmada | Seleccionar evidencia final; no se ha validado rendimiento de exportación con 10.000 filas |
| Evidencia | Pruebas automatizadas, documentación de decisiones y limpieza de fixtures | Capturas finales, demostración, defensa y preparación del quiz |

Se conserva el dominio ganadero y su modelo. Productos, lotes farmacológicos, proveedores, semen y alimentación mantienen su utilidad. Una tabla sin interfaz no se presenta como módulo terminado ni se elimina por ese motivo. La única migración de este incremento amplía la precisión de las cantidades de movimientos; permanecen 43 tablas.

Las decisiones y límites están en [animales y pesaje](fase4-animales.md), [sanidad y reproducción](fase4-sanidad.md), [potreros y traslados](fase4-potreros.md), [interfaz de la finca](fase4-ui.md), [inventario, dashboard y reportes](fase4-operaciones.md) y [sesión persistente](fase4-sesion.md). Los resultados ejecutados están en [verificación](fase4-verificacion.md).
