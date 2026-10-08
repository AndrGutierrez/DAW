# Evidencia de cierre de evaluación — 8 de octubre de 2026

Esta corrida complementa los reportes históricos phase4-final; no los reescribe. La [matriz vigente](../../fase4-evaluacion-09-octubre.md) corresponde al instrumento del profesor de 40 puntos. Las [fórmulas y límites](../../fase4-cierre-evaluacion.md) describen el sistema evaluado.

| Comprobación | Resultado actual | Alcance |
| --- | --- | --- |
| UnitTests | **235 Passed** | Application aislada con xUnit/Moq estricto, sin EF/PostgreSQL |
| Core.Tests | **156 Passed** | Pipeline HTTP + EF InMemory; no sustituye PostgreSQL físico |
| Vitest | **65 Passed** | Sesión, estado y stream fragmentado; TypeScript/build correctos |
| Playwright | **16/16 Passed en una corrida** | home, analytics y evaluation; desktop/mobile sobre API y PostgreSQL aislados |
| Newman | **30 requests / 44 assertions; cero fallos** | Colección Phase4; resumen sanitizado |
| Arranque vacío | **Siete migraciones y seed**, servicios saludables | Proyecto Compose separado y volumen nuevo |
| EF | **Sin cambios de modelo pendientes** | Modelo y migraciones coherentes |
| Esquema/ERD | **44 tablas / 72 FK** | Exportado de PostgreSQL migrado; 43 tablas de aplicación y una de EF |

Cobertura de Core.Application: **UnitTests 68,56 % líneas / 67,88 % ramas**; **Core.Tests 88,69 % líneas / 58,76 % ramas**. No se suman porcentajes y no se atribuye integración al aislamiento Moq. El instrumento no fija un porcentaje mínimo. Los reportes completos conservan las rutas no cubiertas.

## Archivos reproducibles

- [TRX UnitTests](unit-results.trx.gz) / [TRX Core.Tests](integration-results.trx.gz).
- [Cobertura UnitTests](unit-coverage.cobertura.xml.gz) / [cobertura Core.Tests](integration-coverage.cobertura.xml.gz).
- Salidas comprimidas: [UnitTests](evaluation-unit-corrected.log.gz), [Core.Tests](evaluation-integration-corrected.log.gz), [cliente](evaluation-client-current.log.gz), [navegador](evaluation-browser-final.log.gz), [Compose vacío](evaluation-empty-current.log.gz) y [EF](evaluation-model-corrected.log.gz).
- [Resumen Postman](postman-summary.json), sin cuerpos autenticados, contraseñas, cookies, tokens ni entorno privado.
- [Arranque físico y aislamiento](empty-start-summary.json).

Descomprimir con gunzip o un lector compatible. Los casos negativos aprobados comprueban rechazos esperados; no son fallos de la suite.

## Pruebas nuevas

La lógica aislada comprueba genealogía/ciclos/identidad, producción/unidades/sexo, retiro y sacrificio irreversible, reproducción e idempotencia, cohortes con incertidumbre y orden temporal, ámbito de lectores y referencia fechada de cambio. HTTP prueba auditoría/persistencia de tasa, RBAC del stream y filtrado de bovinas accesibles. El broker prueba entrega, desacoplamiento y escrituras que invalidan indicadores.

El navegador verifica que Employee registra producción en otra sesión y Admin recibe el KPI actualizado sin recargar; comprueba preñez/fertilidad observadas y pendientes; registra una tasa sintética y convierte USD/Bs sin modificar precios del catálogo. La suite evaluation exige E2E_ISOLATED_DATABASE=1 y localhost:18089. Los registros con historia se conservan durante la corrida y se retiran con el volumen desechable del proyecto; no se introducen tasas sintéticas en la demostración del equipo.

Estos 16 casos no equivalen a repetir toda la regresión histórica. No acreditan despliegue HTTPS, múltiples réplicas API, tasa BCV automáticamente verificada, accesibilidad completa ni defensa/formulario del equipo.

La actualización local aprobó [build e inicio](evaluation-final-deploy.log.gz). El volumen de demostración conserva sus datos: siete migraciones, 44 tablas y cero tasas inventadas. Se comprobó la retirada completa del proyecto aislado y sus dos volúmenes; http://localhost:18086 permanece funcionando. La rama está publicada en el [PR #8](https://github.com/AndrGutierrez/DAW/pull/8), en borrador para revisión.
