# Estado de implementación de Fase 4

La rama codex/phase4-cattle-spa incluye tres incrementos: sesión persistente y consulta inicial; edición, fotografías y pesajes; sanidad, reproducción, retiro y producción por animal. **La entrega completa de Fase 4 todavía requiere los módulos pendientes de esta tabla.**

| Criterio | Implementado | Trabajo pendiente |
| --- | --- | --- |
| SPA React, Vite y TypeScript | React 18, Tailwind, navegación, animales, editor, pesaje individual/consecutivo, sanidad, reproducción y producción; carga, errores, toasts y diseño móvil | Potreros, inventario y dashboard |
| AuthContext | Cookie persistente, JWT en memoria, renovación, logout, usuario, roles, permisos y autorización de los nuevos flujos | Demostración final completa |
| ThemeContext | Azul UNET, tema oscuro y preferencia persistida; revisión móvil de sanidad | Revisar las futuras pantallas en ambos temas |
| Dashboard | Pendiente | Inventario, umbrales, rotación, leche por lote, peso por edad y métricas reproductivas con denominadores |
| UnitTests y Moq | Proyecto separado que referencia Core.Application; cambio sanitario, validación, GDP, límites, idempotencia, eventos clínicos y retiro | Casos de los siguientes módulos y cobertura real |
| Integración | PostgreSQL, API y SPA en Nginx; búsqueda de animales/progenitores/crías; historiales paginados en servidor; escritura serializable y RFC 7807 | Integrar potreros, inventario, reportes y analítica |
| Ficha 360 del grupo 3 | Identificación, ubicación, árbol genealógico navegable, notas, galería, sanidad, retiro, estado reproductivo, producción y curva Recharts de peso/GDP | Recorrido de corrección auditada de eventos; evidencia final |
| Bajo rendimiento | Aviso al comparar la última GDP con un objetivo explícito en la ficha | Política de objetivos por finca/animal y alertas generales persistentes |
| Potreros | Catálogos y selección dependiente en la ficha | Vista interactiva de ocupación, días de permanencia, capacidad y carga por hectárea |
| Exportaciones y fotografías | Compresión en cliente, vista previa, subida privada y eliminación administrativa confirmada | Reportes XLSX/PDF de sanidad y producción |
| Evidencia | Pruebas automatizadas y documentación de sesión, animales, pesajes, sanidad, reproducción y retiro | Capturas finales, colección de recorridos, demostración, defensa y preparación del quiz |

El siguiente bloque es potreros e insumos útiles para el animal; después, dashboard y exportaciones; auditoría final de la rúbrica.

Se conserva el modelo. Productos, lotes farmacológicos, proveedores, semen y alimentación mantienen su utilidad. Una tabla sin interfaz no se presenta como un módulo terminado ni se elimina por ese motivo.

Las decisiones y límites están en [animales y pesaje](fase4-animales.md), [sanidad y reproducción](fase4-sanidad.md) y [sesión persistente](fase4-sesion.md). Los resultados ejecutados están en [verificación](fase4-verificacion.md).
