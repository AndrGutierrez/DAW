# Estado de implementación de Fase 4

La rama codex/phase4-cattle-spa incluye dos incrementos: sesión persistente y consulta inicial; edición del animal, fotografías y seguimiento de peso. **La entrega completa de Fase 4 todavía requiere los módulos pendientes de esta tabla.**

| Criterio | Implementado | Trabajo pendiente |
| --- | --- | --- |
| SPA React, Vite y TypeScript | React 18, Tailwind, navegación, listado, ficha, alta/edición, pesaje individual y consecutivo; carga, errores, notificaciones y diseño móvil | Flujos clínicos, reproductivos y productivos |
| AuthContext | Cookie persistente, JWT en memoria, renovación, logout, usuario, roles y permisos | Extender la demostración de autorización a los siguientes flujos |
| ThemeContext | Azul UNET y tema oscuro; preferencia persistida | Revisar las futuras pantallas en ambos temas |
| Dashboard | Pendiente | Inventario, umbrales, rotación, leche por lote y métricas ganaderas con sus denominadores |
| UnitTests y Moq | Proyecto separado que referencia Core.Application; cambio sanitario, validación, GDP, límites e idempotencia | Ampliar casos con los siguientes módulos y medir cobertura real |
| Integración | PostgreSQL, API y SPA en Nginx; búsqueda y paginación de animales y progenitores; historial paginado desde servidor; formularios con errores y toasts | Integrar operaciones clínicas y productivas |
| Ficha 360 del grupo 3 | Identificación, ubicación, padres, notas, galería, pesajes y curva interactiva Recharts de peso/GDP | Historial clínico, árbol genealógico visual, reproducción y retiro |
| Bajo rendimiento | Aviso al comparar la última GDP con un objetivo explícito en la ficha | Política de objetivos por finca/animal y alertas generales persistentes |
| Potreros | Catálogos y selección dependiente en la ficha | Vista interactiva de ocupación, capacidad y carga por hectárea |
| Exportaciones y fotografías | Compresión en cliente, vista previa, subida privada y eliminación administrativa confirmada | Reportes XLSX/PDF de sanidad y producción |
| Evidencia | Pruebas automatizadas y documentación de sesión, animales y pesajes | Capturas finales, colección de recorridos, demostración y defensa completas |

El siguiente bloque es sanidad, reproducción y períodos de retiro; después, potreros e insumos útiles para el animal; dashboard y exportaciones; auditoría final de la rúbrica.

Se conserva el modelo. Productos, lotes farmacológicos, proveedores, semen y alimentación mantienen su utilidad. Una tabla sin interfaz no se presenta como un módulo terminado ni se elimina por ese motivo.

Las decisiones y límites de este incremento están en [animales y pesaje consecutivo](fase4-animales.md). Los resultados ejecutados están en [verificación](fase4-verificacion.md).
