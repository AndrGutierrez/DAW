# Referencia del profesor y adaptación a ganadería

Revisión: 8 de octubre de 2026. Se inspeccionaron la [página REAF-F4](https://gramirezsunet.github.io/desarrolloAplicacionesWeb/REAF-F4/) y el [repositorio de ejemplo](https://github.com/gramirezsunet/desarrolloAplicacionesWeb), revisión `b0d3b9ad67ee0f4aa2b60335f168ed5bf314c4c4`. No se ejecutaron sus scripts ni se importaron sus datos.

## Qué aporta el ejemplo

El [App.jsx del ejemplo](https://github.com/gramirezsunet/desarrolloAplicacionesWeb/blob/b0d3b9ad67ee0f4aa2b60335f168ed5bf314c4c4/src/frontend/src/App.jsx) inicia al Admin en dashboard y al empleado en catálogo. No establece una portada administrativa para todos los roles. Su [PowerBIDashboard.jsx](https://github.com/gramirezsunet/desarrolloAplicacionesWeb/blob/b0d3b9ad67ee0f4aa2b60335f168ed5bf314c4c4/src/frontend/src/components/PowerBIDashboard.jsx) presenta tarjetas, filtro por categoría, estados de existencias y gráficos próximos a los datos de gestión.

El [README de referencia](https://github.com/gramirezsunet/desarrolloAplicacionesWeb/blob/b0d3b9ad67ee0f4aa2b60335f168ed5bf314c4c4/README.md) explica producto/arquitectura, diseño y tema, galería comentada, ERD, Docker, credenciales de demostración y una tabla SUT–pruebas–reglas. Estos recursos permiten reproducir y defender el trabajo; se adaptan a los nombres y capacidades reales de este proyecto.

## Adaptación implementada

| Referencia | Decisión de este proyecto | Evidencia |
| --- | --- | --- |
| Entrada por rol | Admin con todos los permisos de indicadores entra al dashboard; Employee y Admin sin esos permisos entran a Animales. Una URL privada solicitada conserva su destino, query y fragmento después del login | auth/navigation.ts, App.tsx, LoginPage.tsx; navigation.test.ts y e2e/home.spec.ts |
| KPI con acciones | Primero producción y seguimiento bovino; después valoración y rotación de insumos. Accesos a fichas, pesaje, reportes y existencias | DashboardPage.tsx; e2e/home.spec.ts contrasta las tarjetas con la respuesta real de la API |
| Gráfico visible | Leche por día como vista inicial; selector disponible para curva por lote, peso por edad y valoración | AnalyticsCharts.tsx; e2e/analytics.spec.ts |
| Filtro y trazabilidad | Finca/período en API; Consultar producción conserva los filtros en Reportes. SSE entre sesiones y consulta de respaldo cada 60 segundos mientras está visible | PeriodFilter, ReportsPage.tsx; e2e/analytics.spec.ts y operations.spec.ts |
| Documentación demostrable | Galería de nuestra SPA, guía de uso vigente y correspondencia de pruebas con reglas | README, product-and-technical-guide.md y evidencia enlazada |

La portada resume datos ya agregados por Application, sin consultar todas las fichas en el navegador:

- **Leche registrada:** suma de litros de los días del período. Sin registros no equivale a producción cero; cantidades con otras unidades se excluyen y se informan.
- **Bovinos con pesaje comparable:** suma de conteos de los grupos de edad; cada bovino aporta su último pesaje en el período, con nacimiento válido. No representa el total de animales ni todos los pesajes.
- **Preñez en hembras evaluadas:** último diagnóstico concluyente por bovina; positivo sin parto/aborto posterior / hembras evaluadas. La sección reproductiva agrega fertilidad de servicios evaluados y pendientes. Los conteos brutos quedan en detalle; las [fórmulas y límites](fase4-cierre-evaluacion.md) evitan extrapolar a todo el rebaño.
- **Existencias críticas:** filas finca/insumo cuyo saldo actual es menor o igual al mínimo. Es estado actual y puede diferir del período seleccionado.

## Diferencias necesarias

El ejemplo es de almacén. Su indicador de rotación muestra el literal **9.66**; además aplica un costo alternativo del 70 % del precio cuando falta costo y suma existencias de productos. Aquí la rotación se calcula con salidas y saldos trazables, se usan precios reales del catálogo y no se suman dosis, litros y kilogramos como unidades homogéneas. No se copian esas fórmulas ni los valores de muestra.

La paleta forestal/marfil/tierra sigue la decisión visual del proyecto; ámbar significa atención y rojo conflicto/peligro. La sesión usa JWT en memoria y refresh HttpOnly según el checklist web y la decisión del proyecto. Las diferencias con el Azul UNET y localStorage de la teoría se explican en [auditoría](fase4-auditoria.md).

Los 31/31 casos del ejemplo describen éxito de pruebas. No prueban por sí solos cobertura de líneas. Nuestros reportes distinguen Passed y cobertura instrumentada por suite/ensamblado.

## Límite de este incremento

Cambiar la portada no completa las alertas generales de GDP, una política persistida de objetivos ni el plano espacial/umbral de permanencia de potreros. Esos puntos, el primer arranque automático desde base vacía y el cierre de la entrega siguen en [trabajo restante](fase4-auditoria.md#trabajo-restante-prioritario).

Se reejecutaron TypeScript, build Docker/Vite, 55 pruebas Vitest y las pruebas Playwright dirigidas de portada, sesión, pestaña de destino, filtros/KPI y actualización. El [reporte de este incremento](evidence/phase4-home/README.md) contiene los conteos finales. Las 263 pruebas .NET corresponden al incremento anterior; no hubo cambios de backend en esta revisión.

## Actualización posterior del mismo día

La revisión de usuarios y cierre técnico completa las brechas enumeradas en el apartado histórico anterior: objetivos GDP persistentes, avisos por finca, plano y umbral de permanencia, arranque desde base vacía y Postman. El instrumento nuevo del 9 de octubre declara 40 puntos; ver [guía vigente](fase4-evaluacion-09-octubre.md) y [evidencia actual](evidence/phase4-final/README.md). Los resultados de phase4-home se conservan como evidencia de aquel incremento, sin sustituir los actuales.

El cierre posterior añade cohortes de preñez/fertilidad, eventos SSE entre sesiones, referencia USD/Bs fechada y mayor aislamiento Moq. Ver [métodos y límites actuales](fase4-cierre-evaluacion.md) y [ejecución nueva](evidence/phase4-evaluation/README.md). Los reportes anteriores conservan su fecha y alcance.
