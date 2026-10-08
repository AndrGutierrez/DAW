# Evidencia de revisión de Fase 4

Ejecuciones del 8 de octubre de 2026. Esta carpeta contiene reportes reales y capturas del entorno local aislado; no contraseñas, tokens ni la configuración .env.

## Resultado

| Suite | Resultado | Alcance |
| --- | --- | --- |
| tests/UnitTests | 122 Passed, 0 Failed | Application con mocks Moq, validadores y cálculos; sin conexión a PostgreSQL |
| tests/Core.Tests | 141 Passed, 0 Failed | Dominio, aplicación, EF InMemory y pipeline HTTP; no sustituye pruebas PostgreSQL |
| Vitest | 43 Passed | Sesión y validaciones cliente |
| Playwright dirigido | 10 Passed | Curva diaria por lote, pérdida de peso, objetivo explícito, pestañas y legibilidad, cada caso en desktop/mobile |
| Comprobación final de curva | 2 Passed | Repetición desktop/mobile después de componer los selectores con utilidades responsivas Tailwind |

Las suites .NET suman **263 pruebas**. La regresión completa anterior tuvo 94 casos Playwright; este incremento ejecutó los casos afectados, no una nueva regresión completa de todo el sistema.

[Salida del runner](test-run.txt), [navegador](browser-run.txt), [frontend](frontend-run.txt), [reporte UnitTests TRX](unit-tests.trx) y [reporte Core.Tests TRX comprimido](core-tests.trx.gz).

## Cobertura medida

| Ejecución / ensamblado | Líneas | Ramas |
| --- | --- | --- |
| UnitTests / Core.Application | 43,35 % | 47,21 % |
| UnitTests / Core.Domain | 30,92 % | 25,00 % |
| Core.Tests / Core.Application | 91,81 % | 64,46 % |
| Core.Tests / Core.Domain | 57,21 % | 75,00 % |
| Core.Tests / Infrastructure | 8,15 % | 73,38 % |
| Core.Tests / Presentation.API | 88,45 % | 72,03 % |

Son resultados **separados** del collector, no porcentajes que puedan sumarse o promediarse. El reporte bruto UnitTests tiene 639/1585 líneas (40,31 %) y Core.Tests 3341/21835 (15,30 %). Infrastructure incluye código de migraciones generado; no se excluyó para inflar el total. No se afirma cobertura global del 100 % ni se deduce de los 263 Passed.

[Unit coverage Cobertura XML](unit-coverage.cobertura.xml.gz) y [Core coverage Cobertura XML](core-coverage.cobertura.xml.gz). Se comprimieron sin modificar su contenido. En Git Bash, por ejemplo:

~~~bash
gzip -dc docs/evidence/phase4-audit/unit-coverage.cobertura.xml.gz > artifacts/unit-coverage.xml
~~~

Para reproducir con SDK .NET 10:

~~~bash
dotnet test tests/UnitTests/UnitTests.csproj -c Release --collect:"XPlat Code Coverage" --logger "trx" --results-directory artifacts/unit-tests
dotnet test tests/Core.Tests/Core.Tests.csproj -c Release --collect:"XPlat Code Coverage" --logger "trx" --results-directory artifacts/core-tests
~~~

El SDK también se puede ejecutar en el contenedor descrito en docs/setup.md. Los reportes actuales corresponden a ejecuciones Docker en /src. La cobertura de integración se repitió después de corregir el lector JSON de la prueba para los enums en cadena del contrato.

## Capturas verificadas

- [Ubicación desktop](location-desktop.png): padding de 26 px.
- [Ubicación móvil](location-mobile.png): padding de 20 px; formulario de traslado accesible y sin desbordamiento.
- [Leche diaria por lote desktop](daily-lot-milk-desktop.png).
- [Leche diaria por lote móvil](daily-lot-milk-mobile.png).

La curva usa fixtures identificados de prueba (P4-MILK), con 1,234 y 2,345 L en días distintos. El día intermedio sin registros no se inventa como cero. Los fixtures se eliminan por API en afterEach; las capturas permanecen como evidencia del recorrido.

Ver [auditoría](../../fase4-auditoria.md) para pendientes y diferencias deliberadas con las fuentes.
