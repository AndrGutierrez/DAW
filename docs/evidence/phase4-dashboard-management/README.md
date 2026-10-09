# Evidencia de dashboard y catálogos

Revisión del 8 de octubre de 2026. [Comportamiento, accesos y permisos](../../fase4-dashboard-catalogos.md) · [resumen estructurado](validation-summary.json).

| Verificación | Resultado |
| --- | --- |
| Frontend / Vitest | 131 aprobadas; filtros aplicados/cancelados, borradores y permisos, cuatro altas de catálogo, conflicto de edición y confirmación de archivado |
| Build | TypeScript/Vite y contenedor Nginx de producción aprobados |
| PostgreSQL / navegador | Corrida inicial: 18 aprobadas, tres fallos y una omisión de una prueba exclusivamente móvil. Seis repeticiones focalizadas aprobadas. Después del último ajuste del encabezado/texto: seis recorridos visuales/funcionales aprobados. 21 casos distintos con resultado final exitoso; no se presenta como una única corrida limpia |
| Accesibilidad | 68 combinaciones de pantalla/tema/viewport sin infracciones axe detectadas; comprobaciones adicionales de filtros y formularios. No constituye certificación WCAG completa |
| Conservación | Cinco archivados confirmados con HTTP 204 y consulta de sus filas retenidas; bloqueo de potrero referenciado por lote comprobado |
| Móvil | KPI en dos columnas, encabezado compacto, filtros modales, sin desbordamiento horizontal en los recorridos comprobados |

Los tres fallos iniciales fueron de sincronización/selectores de las pruebas: el texto de pendientes aparecía en dos lugares; la navegación completa al siguiente catálogo interrumpió una solicitud DELETE antes de recibir su respuesta. La prueba ahora espera la respuesta HTTP 204 de cada recurso, además del cambio visual. Se conservan el log inicial y las repeticiones; no se excluyeron casos ni se añadieron reintentos automáticos.

Cobertura V8 global actual: **44,59% de líneas / 30,71% de ramas**. Los E2E no se suman a esta cobertura. Los reportes anteriores de conservación/BCV siguen disponibles con su fecha y alcance.

Capturas inspeccionadas: [dashboard móvil](dashboard-mobile.png), [dashboard escritorio](dashboard-desktop.png), [reproducción con evaluación móvil](reproduction-evaluated-mobile.png), [reproducción con evaluación escritorio](reproduction-evaluated-desktop.png), [estado sin evaluación](reproduction-empty-mobile.png). Son pantallas del entorno de pruebas con datos efectivamente registrados; no se incorporaron fixtures a la base de demostración.

Se conservan logs comprimidos, cobertura y JSON de medición sin credenciales. No se publican trazas de navegador, entornos privados ni respaldos.

## Reproducir

Desde src/frontend, con Node compatible:

~~~bash
npm ci
npm run test:coverage -- --maxWorkers=2
npm run build
~~~

Los escenarios de mutación requieren una instancia desechable separada: E2E_BASE_URL=http://localhost:18089, E2E_ISOLATED_DATABASE=1 y las credenciales locales del seed. Mantener BCV_AUTO_SYNC=false en ese proyecto. Ejecutar npm run test:e2e -- e2e/dashboard-management.spec.ts e2e/analytics.spec.ts e2e/evaluation.spec.ts e2e/quality.spec.ts --workers=1. Eliminar únicamente los contenedores/volúmenes de ese proyecto al concluir.
