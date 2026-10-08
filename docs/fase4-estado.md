# Estado de implementación de Fase 4

La rama codex/phase4-cattle-spa incorpora SPA, sesión persistente, gestión del animal, sanidad/reproducción/producción, pesaje, potreros y traslados, inventario, dashboard, reportes y **gestión administrativa de usuarios**. La revisión del 8 de octubre completa objetivos GDP, avisos generales, plano espacial/permanencia, primer arranque automático, cohortes reproductivas, actualización entre sesiones y referencia USD/Bs fechada.

El [instrumento nuevo para el 9 de octubre](fase4-evaluacion-09-octubre.md) declara **40 puntos en cuatro criterios de 10**. La rúbrica anterior de 80 puntos se conserva como referencia histórica, sin sustituir la evaluación publicada por el profesor.

| Área | Implementado | Límite o siguiente trabajo |
| --- | --- | --- |
| SPA y UI | React 18, Vite, TypeScript, Tailwind, rutas, controles compartidos, contextos, Source Sans 3, Phosphor, Radix, ambos temas, pestañas y paneles laterales | Auditoría completa de accesibilidad y compatibilidad fuera de Chromium |
| Sesión y accesos | JWT en memoria, refresh HttpOnly/CSRF, restauración y logout, roles/permisos/fincas en servidor, revocación administrativa inmediata | HTTPS y configuración del entorno público por verificar |
| Usuarios | Crear/editar, rol, fincas, permisos adicionales, desactivar, restablecer contraseña, versiones y auditoría; protecciones propias/superusuario/último admin | No envía contraseñas por correo ni permite borrado de cuentas |
| Dashboard | Cuatro KPI, gráficos y filtros reales; entrada Admin, SSE entre sesiones, respaldo de 60 s y avisos GDP actuales | Broker de una instancia API; cohortes observadas y referencia BCV de ingreso manual |
| Ficha animal | Identificación, ubicación/traslados, genealogía, notas, fotografías privadas, sanidad/retiro, reproducción, producción y peso/GDP | Corrección de eventos desde UI como ampliación |
| Rendimiento | Objetivo persistente de finca/animal, prioridad individual, descenso automático y resumen paginado con enlace a ficha | Avisos recalculados; no historia de notificaciones o proceso de envío |
| Potreros | Capacidad, densidad, fechas conocidas, residentes/lotes, traslados y plano configurable; relleno por capacidad y borde por permanencia | Esquema sin GPS, arrastre, recomendación agronómica ni traslado masivo |
| Inventario/reportes | Productos, categorías, existencias, límites, movimientos auditados y XLSX/PDF | Caducidad farmacológica y consumo clínico requieren otro flujo; sin prueba de carga de exportación |
| Arranque y contratos | initialize aplica migraciones/seed antes de API; probado con volumen vacío; colección Postman actualizada; ERD físico vigente | Comprobación desde otro equipo y revisión del PR #8 |
| Pruebas | 235 UnitTests + 156 Core.Tests, 65 Vitest; 16/16 recorridos en la corrida actual y Newman 30 requests/44 assertions; reportes de cobertura separados | Reforzar con Moq las rutas hoy cubiertas por integración; no afirmar cobertura global completa |
| Entrega académica | Fuente nueva leída, matriz de cuatro criterios, recorrido de defensa y evidencia seleccionada | Revisión del PR #8, ensayo/defensa presencial y envío del formulario |

Se conserva el dominio ganadero; el esquema actual tiene 44 tablas y 72 FK. DatedExchangeRates añade historial de cotizaciones de referencia, sin borrar datos. La migración GrowthGoalsAndPaddockPlan añade configuración y aumenta precisión del umbral de GDP; no elimina tablas ni borra datos. Las posiciones de ejemplo se siembran solo en una finca DEMO nueva; las instalaciones existentes requieren configuración real del plano.

Ver [usuarios](fase4-usuarios.md), [animales](fase4-animales.md), [potreros](fase4-potreros.md), [sesión](fase4-sesion.md), [evidencia de cierre](evidence/phase4-evaluation/README.md) y [guía de evaluación](fase4-evaluacion-09-octubre.md).
