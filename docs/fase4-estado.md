# Estado de implementación de Fase 4

La rama codex/phase4-cattle-spa incorpora SPA, sesión persistente, gestión del animal, sanidad/reproducción/producción, pesaje, potreros y traslados, inventario, dashboard, reportes y **gestión administrativa de usuarios**. La revisión del 8 de octubre completa objetivos GDP, avisos generales, plano espacial/permanencia y primer arranque automático.

El [instrumento nuevo para el 9 de octubre](fase4-evaluacion-09-octubre.md) declara **40 puntos en cuatro criterios de 10**. La rúbrica anterior de 80 puntos se conserva como referencia histórica, sin sustituir la evaluación publicada por el profesor.

| Área | Implementado | Límite o siguiente trabajo |
| --- | --- | --- |
| SPA y UI | React 18, Vite, TypeScript, Tailwind, rutas, controles compartidos, contextos, Source Sans 3, Phosphor, Radix, ambos temas, pestañas y paneles laterales | Auditoría completa de accesibilidad y compatibilidad fuera de Chromium |
| Sesión y accesos | JWT en memoria, refresh HttpOnly/CSRF, restauración y logout, roles/permisos/fincas en servidor, revocación administrativa inmediata | HTTPS y configuración del entorno público por verificar |
| Usuarios | Crear/editar, rol, fincas, permisos adicionales, desactivar, restablecer contraseña, versiones y auditoría; protecciones propias/superusuario/último admin | No envía contraseñas por correo ni permite borrado de cuentas |
| Dashboard | Cuatro KPI, gráficos y filtros reales; entrada Admin, actualización de 60 s visible y avisos GDP actuales | Polling, sin WebSocket; explicar límites de denominadores y períodos |
| Ficha animal | Identificación, ubicación/traslados, genealogía, notas, fotografías privadas, sanidad/retiro, reproducción, producción y peso/GDP | Corrección de eventos desde UI como ampliación |
| Rendimiento | Objetivo persistente de finca/animal, prioridad individual, descenso automático y resumen paginado con enlace a ficha | Avisos recalculados; no historia de notificaciones o proceso de envío |
| Potreros | Capacidad, densidad, fechas conocidas, residentes/lotes, traslados y plano configurable; relleno por capacidad y borde por permanencia | Esquema sin GPS, arrastre, recomendación agronómica ni traslado masivo |
| Inventario/reportes | Productos, categorías, existencias, límites, movimientos auditados y XLSX/PDF | Caducidad farmacológica y consumo clínico requieren otro flujo; sin prueba de carga de exportación |
| Arranque y contratos | initialize aplica migraciones/seed antes de API; probado con volumen vacío; colección Postman actualizada; ERD físico vigente | Publicación y comprobación desde otro equipo |
| Pruebas | 153 UnitTests + 144 Core.Tests, 61 Vitest; 50 recorridos dirigidos comprobados y Newman 26 requests/37 assertions; reportes de cobertura separados | Reforzar con Moq las rutas hoy cubiertas por integración; no afirmar cobertura global completa |
| Entrega académica | Fuente nueva leída, matriz de cuatro criterios, recorrido de defensa y evidencia seleccionada | Publicación remota, ensayo/defensa presencial y envío del formulario |

Se conserva el dominio ganadero y sus 43 tablas. La migración GrowthGoalsAndPaddockPlan añade configuración y aumenta precisión del umbral de GDP; no elimina tablas ni borra datos. Las posiciones de ejemplo se siembran solo en una finca DEMO nueva; las instalaciones existentes requieren configuración real del plano.

Ver [usuarios](fase4-usuarios.md), [animales](fase4-animales.md), [potreros](fase4-potreros.md), [sesión](fase4-sesion.md), [evidencia de cierre](evidence/phase4-final/README.md) y [guía de evaluación](fase4-evaluacion-09-octubre.md).
