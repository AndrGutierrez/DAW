# Estado de implementación de Fase 4

La rama codex/phase4-cattle-spa incluye cinco incrementos: sesión persistente y consulta inicial; edición, fotografías y pesajes; sanidad, reproducción, retiro y producción; mapa de potreros, lotes presentes, capacidad y traslados; sistema visual de la finca y protección de editores. **La entrega completa de Fase 4 todavía requiere los módulos pendientes de esta tabla.**

| Criterio | Implementado | Trabajo pendiente |
| --- | --- | --- |
| SPA React, Vite y TypeScript | React 18, Tailwind, navegación, animales, editor, pesaje, sanidad, reproducción, producción y mapa de potreros; carga, errores, toasts y diseño móvil | Inventario y dashboard |
| AuthContext | Cookie persistente, JWT en memoria, renovación, logout, usuario, roles, permisos y autorización de los nuevos flujos | Demostración final completa |
| ThemeContext y UI | Paleta semántica de granja en ambos temas, preferencia persistida, controles compartidos e iconos SVG; navegación móvil inferior; ficha con accesos a ocho secciones | Extender la misma base a inventario/dashboard y completar auditoría de accesibilidad |
| Dashboard | Pendiente | Inventario, umbrales, rotación, leche por lote, peso por edad y métricas reproductivas con denominadores |
| UnitTests y Moq | Proyecto separado con referencia a Core.Application; cambio sanitario, GDP, idempotencia, eventos clínicos, retiro, capacidad y traslados | Casos de los siguientes módulos y cobertura real |
| Integración | PostgreSQL, API y SPA en Nginx; búsquedas e historiales paginados en servidor; escritura serializable y RFC 7807 | Integrar inventario, reportes y analítica |
| Ficha 360 del grupo 3 | Resumen y navegación de secciones; identificación, ubicación/traslados, genealogía, notas, galería, sanidad, retiro, reproducción, producción y curva Recharts de peso/GDP | Corrección auditada de eventos; evidencia final |
| Bajo rendimiento | Aviso al comparar la última GDP con un objetivo explícito en la ficha | Política por finca/animal y alertas generales persistentes |
| Potreros y lotes | Mapa esquemático seleccionable; ocupación activa, capacidad, animales/ha, permanencia conocida, filtros de lotes presentes y residentes paginados; traslado con origen esperado, autor y motivo; bloqueo de cupos en ambas rutas de escritura | Planificación de rotación, umbrales de permanencia y traslado masivo de lotes |
| Inventario | Productos, categorías y existencias administrados por API | Interfaz, entradas/salidas auditadas, umbrales y relación operativa con los insumos del animal |
| Exportaciones y fotografías | Compresión en cliente, vista previa, subida privada y eliminación administrativa confirmada | Reportes XLSX/PDF de sanidad y producción |
| Evidencia | Pruebas automatizadas y documentación de sesión, animales, pesajes, sanidad, reproducción, retiro y potreros | Capturas finales, recorridos, demostración, defensa y preparación del quiz |

El siguiente bloque es inventario e insumos útiles para el animal; después, dashboard y exportaciones; auditoría final de la rúbrica.

Se conserva el modelo. Productos, lotes farmacológicos, proveedores, semen y alimentación mantienen su utilidad. Una tabla sin interfaz no se presenta como un módulo terminado ni se elimina por ese motivo.

Las decisiones y límites están en [animales y pesaje](fase4-animales.md), [sanidad y reproducción](fase4-sanidad.md), [potreros y traslados](fase4-potreros.md) [interfaz de la finca](fase4-ui.md) y [sesión persistente](fase4-sesion.md). Los resultados ejecutados están en [verificación](fase4-verificacion.md).
