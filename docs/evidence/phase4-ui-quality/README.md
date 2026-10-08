# Evidencia de UI, testing frontend y rendimiento — Fase 4

Ejecuciones del 8 de octubre de 2026. Complementan, sin reescribir, las mediciones históricas de [evaluación](../phase4-evaluation/README.md). La [auditoría integral](../../fase4-auditoria-integral.md) relaciona estas pruebas con Classroom, el documento del 9 de octubre y REAF-F4.

## Resultados

| Verificación | Resultado | Archivo |
| --- | --- | --- |
| React/TypeScript/Vite de producción | Aprobado | frontend-build-tests.log.gz |
| Vitest, funciones y componentes DOM | 107 aprobadas | frontend-build-tests.log.gz; frontend-coverage-summary.json |
| xUnit/Moq UnitTests | 236 aprobadas, cero omisiones/fallos | unit-results.trx.gz; unit-coverage.cobertura.xml.gz |
| Core.Tests, HTTP/EF InMemory | 156 aprobadas, cero omisiones/fallos | integration-results.trx.gz; integration-coverage.cobertura.xml.gz |
| Playwright, suite completa | 127 aprobadas; una omisión intencional en escritorio | playwright-all.log.gz |
| Revisión posterior de layout/contraste/teclado | 19 aprobadas y una omisión: 17 casos permanentes + dos mediciones temporales de desplazamiento | layout-accessibility-recheck.log.gz |
| Axe, pantallas y temas | 44 comprobaciones, cero infracciones detectadas | accessibility-desktop.json; accessibility-mobile.json |
| Instrumentación de cliente | Valores reales y exportación sin secretos | performance-desktop.json; performance-mobile.json; diagnostics-*.png |

La omisión es el caso de navegación móvil Más dentro del proyecto desktop. No es una prueba fallida ni una pérdida de cobertura encubierta. La revisión de layout identifica el pie de página y el ancho variable del botón del filtro; reservar espacio redujo CLS de 0,1133 a 0,0103 en la medición de escritorio. La sonda temporal no se agrega a la suite permanente.

La suite completa se ejecutó sobre la versión funcional final. Después se ajustaron únicamente la reserva de altura del dashboard, el ancho del filtro y la separación del título de sección; se repitieron las comprobaciones afectadas, el build y la imagen de producción. Los logs distinguen estas ejecuciones; no se suman sus casos como si fueran pruebas diferentes.

## Testing frontend y alcance de cobertura

Vitest mide todos los archivos TS/TSX de src, incluidos módulos sin pruebas de montaje. Se excluyen tests/setup, el punto de entrada main y declaraciones de tipos. No se restringe el denominador a los archivos favorables.

| Alcance | Líneas | Ramas |
| --- | --- | --- |
| Frontend global, V8 | 435 / 1192 = 36,49% | 540 / 2475 = 21,81% |
| LoginPage | 89,74% | 82,35% |
| AnimalEditorPage | 90,62% | 59,70% |
| DashboardPage | 91,30% | 68,75% |
| WeighingForm | 89,65% | 78,78% |
| PerformancePage | 85,71% | 65,71% |
| useResource | 100% | 85% |
| Validación local | 100% | 100% |
| Core.Application en UnitTests | 68,05% | 67,32% |
| Core.Application en Core.Tests | 88,50% | 57,80% |

Las suites .NET se instrumentan por separado; no se suman sus porcentajes. Los recorridos Playwright acreditan comportamiento contra API/PostgreSQL, pero no se agregan a la cobertura V8. La nueva propiedad de distribución y sus ramas aumentan el denominador de Application respecto al reporte anterior; por ello cambian los porcentajes aunque no disminuya la cantidad de pruebas.

Los casos DOM ejercitan interacción y efectos: login con errores y doble envío, contraseña visible, enfoque del campo inválido; editor animal y catálogos compatibles, borrador tras rechazo; tarjetas del dashboard, filtros aplicados y valores desconocidos; pesaje idempotente tras respuesta incierta; cancelación/reintento de recursos; diagnósticos y guard Admin; error asociado por Field, período excesivo y foco de tabla solo si desborda.

## Accesibilidad y muestras de rendimiento

Axe revisó login, dashboard, animales, pesaje, potreros, inventario, reportes, seguimiento, usuarios, cuenta y diagnósticos, con etiquetas WCAG 2 A/AA, 2.1 AA y 2.2 AA. Son once pantallas × dos temas × dos tamaños = 44. Se verificó también ausencia de desbordamiento horizontal de página, navegación Más, Escape/foco restaurado, formularios y movimiento reducido. Esto es evaluación automática y funcional dirigida; no certifica por sí sola toda WCAG ni reemplaza pruebas con usuarios y tecnologías asistivas.

Las mediciones se obtuvieron en Chromium de Playwright, entorno de producción Nginx/API/PostgreSQL aislado, localhost y sin simular una conexión rural lenta. Se autenticó el navegador, se cargó directamente el dashboard y se navegó por SPA a Mi cuenta → Rendimiento para exportar. La medición corresponde al documento; no se reinició LCP para cada ruta.

| Muestra local | Escritorio | Móvil |
| --- | --- | --- |
| LCP | 96 ms | 108 ms |
| INP | 16 ms | 16 ms |
| CLS | 0 | 0 |
| HTTP p75 local, hasta cabeceras | 17,4 ms | 17 ms |
| Datos p75 local, lectura/decodificación incluidas | 17,8 ms | 18,6 ms |

Son muestras particulares, no objetivos garantizados ni percentiles de usuarios reales. Los JSON mantienen precisión original, cantidad de muestras y estado de cancelación; se excluyen cancelaciones al calcular p75. Los valores pueden variar entre ejecuciones y la tabla debe contrastarse con los JSON de esta revisión.

El export contiene exclusivamente métricas escalares y grupos de recursos; excluye URL, IDs, parámetros, Authorization, tokens, cookies, credenciales, cuerpos y entradas DOM de web-vitals. No sale del navegador salvo descarga solicitada. El almacenamiento de solicitudes es en memoria, hasta 100 eventos, y se limpia al cerrar sesión. Los streams SSE no son peticiones de duración finita y se excluyen.

Las páginas se cargan bajo demanda. El bundle inicial principal ronda 107 kB gzip; Phosphor, CSS y fuente local son recursos adicionales. Los gráficos, PDF y Excel se cargan al necesitarlos. ExcelJS conserva un chunk de aproximadamente 930 kB minificado / 256 kB gzip y provoca una advertencia real de tamaño; no se ocultó con un umbral artificial ni se confunde su tamaño con el bundle del login.

## Reproducción

En Git Bash, Node 22 y .env configurado, desde la raíz:

~~~bash
docker compose up --build -d --wait
cd src/frontend
export npm_config_script_shell="$(cygpath -w /usr/bin/bash.exe)"
npm ci
npm run build
npm run test:coverage
~~~

El script_shell explícito mantiene Bash al ejecutar scripts npm en Windows. En Linux se utiliza Bash disponible en ese entorno, sin cygpath. Consultar [setup](../../setup.md) para herramientas y secretos locales.

Pruebas C# desde la raíz:

~~~bash
MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd -W):/src" -w /src   mcr.microsoft.com/dotnet/sdk:10.0   dotnet test DAW.slnx --collect:"XPlat Code Coverage"
~~~

Playwright crea/modifica fixtures. Debe ejecutarse contra un proyecto Compose desechable con PostgreSQL separado, nunca contra cuentas o datos reales:

~~~bash
# Configurar estas variables en el shell privado del entorno de pruebas:
# E2E_BASE_URL, E2E_ADMIN_USERNAME/PASSWORD y E2E_EMPLOYEE_USERNAME/PASSWORD.
# E2E_ISOLATED_DATABASE=1 confirma que ese entorno es desechable.
cd src/frontend
npx playwright install chromium
QUALITY_EVIDENCE_DIR="$(pwd -W)/../../docs/evidence/phase4-ui-quality"   npm run test:e2e
~~~

Las contraseñas no se incluyen en este documento, los logs ni los reportes. La variable de aislamiento no crea una base aparte por sí sola: primero debe prepararse ese proyecto. Los puertos 18089/15439 de esta revisión pertenecieron exclusivamente al entorno desechable; 18086 conserva la demostración del equipo.

## Procedencia

source-inventory.json registra hashes de 17 documentos primarios de la materia, el quiz y la página REAF. Los documentos, quiz, video, credenciales y guías privadas no se copiaron al Git. Los archivos gzip contienen TRX/Cobertura o logs seleccionados, no trazas de navegador con formularios. Las capturas y métricas son del entorno de demostración o prueba identificado, no de una finca real.
