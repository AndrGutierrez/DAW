# Evidencia de cierre técnico — 8 de octubre de 2026

## Ejecución actual

| Verificación | Resultado y alcance |
| --- | --- |
| .NET / UnitTests | **153 Passed**, xUnit + Moq; referencia solo a Application |
| .NET / Core.Tests | **144 Passed**, pipeline HTTP + EF InMemory aislado |
| Frontend | **61 Passed**, Vitest; TypeScript/build Docker/Vite correctos |
| Navegador dirigido | **50 escenarios comprobados**, escritorio/móvil contra PostgreSQL; portada, sesión, fotos, potreros, usuarios, objetivos, plano y pulido |
| Postman / Newman | **26 solicitudes, 37 assertions; cero fallos**, colección específica Fase 4 |
| Base vacía | Compose up --build -d --wait; initialize completado, seis migraciones, login 200 y dos potreros DEMO configurados |
| EF | has-pending-model-changes: sin cambios pendientes |
| ERD físico | 43 tablas, 72 FK, exportado del PostgreSQL migrado |

La ejecución dirigida del navegador aprobó 49 de 50 casos. El caso restante de selector de madre pulsaba Fin antes de que terminara de llegar la segunda página. Se añadió una espera por el candidato esperado y se reejecutó el escenario en ambas resoluciones: 2 Passed. El resultado conserva esa distinción; no se afirma que los 50 pasaran en una única corrida ni que se reejecutaran todas las suites E2E del proyecto.

Las pruebas nuevas restauran el objetivo de finca previo y eliminan sus pesos, animales y potreros por API. Las cuentas temporales de administración se conservan inactivas para mantener auditoría. La base vacía se aisló con otro proyecto/puertos/volúmenes; no reutilizó la base de demostración existente.

## Archivos

- [TRX de UnitTests](unit-results.trx.gz) y [TRX de Core.Tests](integration-results.trx.gz).
- [Cobertura UnitTests](unit-coverage.cobertura.xml.gz) y [cobertura Core.Tests](integration-coverage.cobertura.xml.gz), formato Cobertura, gzip.
- [Resumen Postman](postman-summary.json): estados HTTP y assertions, sin requests autenticados, contraseñas, tokens o entorno privado.

Los archivos gzip contienen el reporte completo; descomprimir con gunzip o un lector compatible. Core.Application: UnitTests **48,29 % líneas / 50,37 % ramas**; Core.Tests **88,32 % líneas / 58,28 % ramas**. No sumar porcentajes ni presentar integración como aislamiento Moq.

Los reportes instrumentados incluyen rutas no cubiertas y código generado. No acreditan 100 % global, pruebas de carga, HTTPS publicado o certificación de accesibilidad. Las capturas ilustrativas del cierre están en ../../screenshots; muestran datos de demostración, no una finca real.

El [recorrido de evaluación](../../fase4-evaluacion-09-octubre.md) conecta estas evidencias con el instrumento vigente de 40 puntos.

[Resumen de arranque desde volumen nuevo](empty-start-summary.json). Se incluyen también salidas completas comprimidas de .NET, Vitest, regresión de navegador, reejecución del selector, Compose y coherencia EF. El proyecto temporal de arranque se retira después de comprobar su propiedad; la instancia de demostración en 18086 sigue disponible.
