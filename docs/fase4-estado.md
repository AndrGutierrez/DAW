# Estado de implementación de Fase 4

Este incremento establece la sesión persistente y el primer recorrido de consulta del ganado. **No constituye todavía la entrega completa de Fase 4.**

| Criterio | Implementado | Trabajo pendiente |
| --- | --- | --- |
| SPA React, Vite y TypeScript | React 18, Tailwind, login, navegación sin recarga, listado y ficha básica; estados de carga y error; diseño móvil | Formularios de alta/edición, operaciones productivas y pesaje consecutivo |
| AuthContext | Recuperación por cookie, JWT en memoria, renovación, logout, usuario, roles y permisos | Ampliar la demostración de autorización a cada nuevo flujo |
| ThemeContext | Azul UNET y tema oscuro; preferencia persistida | Extender la revisión visual a las pantallas futuras |
| Dashboard | Sin implementación en este incremento | Valores de inventario, umbrales, rotación, leche por lote y métricas ganaderas con sus denominadores |
| UnitTests y Moq | Proyecto separado que referencia únicamente Core.Application; pruebas aisladas de cambio sanitario y validación de consultas | Ampliar casos de uso, éxitos y errores conforme se añadan los flujos; medir cobertura real |
| Integración | PostgreSQL, API y SPA en Nginx; búsqueda y paginación del ganado en el servidor | CRUD desde interfaz con errores por campo y toasts |
| Ficha 360 del grupo 3 | Identificación, ubicación, último peso, fotografías privadas y progenitores | Curva de peso/GDP, eventos clínicos, reproducción y alertas de retiro |
| Potreros | Entidades y API existentes conservadas | Vista interactiva de ocupación, capacidad y carga por hectárea |
| Exportaciones y fotografías | Consulta privada de fotografías | Compresión en cliente, subida desde ficha y reportes XLSX/PDF |
| Evidencia de entrega | Pruebas automatizadas del incremento y documentación de la decisión de sesión | Capturas finales, colección de recorridos, demostración y defensa completas |

El orden de los siguientes incrementos es: ficha 360 con pesajes y GDP; flujos clínicos/reproductivos y retiro; potreros e insumos útiles para el animal; dashboard y exportaciones; auditoría final de la rúbrica.

El modelo se mantiene. Productos, lotes farmacológicos, proveedores, semen y alimentación conservan su utilidad potencial. No se presenta un módulo pendiente como funcional ni se elimina una tabla por carecer todavía de interfaz.
