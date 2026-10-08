# Verificación de portada y documentación

Ejecución: 8 de octubre de 2026. Incremento de frontend/documentación; la API y el esquema de base de datos no cambiaron.

| Verificación | Resultado | Alcance |
| --- | --- | --- |
| TypeScript | Aprobado | tsc -b |
| Vitest | 55 Passed | 43 casos existentes + 12 de destino/inicio seguro según rol/permisos |
| Build Docker/Vite | Aprobado | Imagen frontend actualizada y servida por Nginx en el entorno local |
| Playwright dirigido | 16 Passed | 8 casos en desktop/mobile: inicio Admin/Employee, login directo/KPI reales, destino con pestaña/fragmento, curva por lote/reporte filtrado, persistencia, contraseña y expiración |
| Actualización periódica | 2 Passed | Saldo real modificado por API; tarjeta/tabla actualizada después de avanzar el reloj del navegador 60 segundos, desktop/mobile |

Son **18 comprobaciones Playwright**. No se ejecutó toda la regresión del sistema ni las suites .NET en este incremento. Las [263 pruebas .NET y cobertura](../phase4-audit/README.md) corresponden al incremento anterior; no se presentan como reejecutadas aquí.

[Reporte frontend/build](frontend-run.txt), [reporte navegador](browser-run.txt) y [limpieza acotada de fixtures](fixture-cleanup.txt). Se normalizaron códigos de color, retornos de terminal y espacios finales para guardar texto legible; los resultados se conservan. El build mantiene el aviso de tamaño de chunks; exportadores y gráficos se cargan de forma diferida.

## Capturas

- [Acceso claro](../../screenshots/login-light.png): campos y controles sin credenciales rellenadas.
- [Dashboard claro](../../screenshots/dashboard-light.png): datos de demostración de la API, período/finca y acciones.
- [Dashboard oscuro](../../screenshots/dashboard-dark.png): mismos indicadores con tokens del tema.
- [Dashboard móvil](../../screenshots/dashboard-mobile.png): captura completa de la página a 390 px; tarjetas en columna, gráficos/tablas y navegación móvil.

Las capturas de escritorio tienen 1440 × 1000 px. Se verificó ausencia de desbordamiento de la página a 1440 y 390 px; las tablas extensas conservan scroll interno. No se afirma certificación universal de accesibilidad o navegadores.

## Aislamiento de datos de prueba

La curva diaria crea P4-MILK por API y elimina sus animales/lotes/producción en afterEach. La prueba de actualización crea dos insumos/existencias con prefijos P4-OPS y manifiestos UUID propios. Se limpió exclusivamente ese conjunto en una transacción con comprobación de SKU, categoría, finca DEMO y dependencias; el historial trazado exige esta limpieza del entorno de prueba. Se comprobó cero registros restantes de esos fixtures y conservación de DEMO-001 y DEMO-F4-AURORA.

## Reproducción

Con el entorno aislado en marcha y credenciales E2E en variables de entorno, ejecutar desde src/frontend en Git Bash:

~~~bash
node node_modules/typescript/bin/tsc -b
node node_modules/vitest/vitest.mjs run
node node_modules/@playwright/test/cli.js test e2e/home.spec.ts e2e/analytics.spec.ts e2e/session.spec.ts e2e/ui-polish.spec.ts --grep 'home respects role|admin direct login|requested animal tab|daily milk chart|persistent session survives reload|password visibility|expired session'
node node_modules/@playwright/test/cli.js test e2e/operations.spec.ts --grep 'dashboard refreshes visible inventory'
~~~

El último comando deja fixtures con historial de insumos, identificados en manifiestos del directorio temporal. Ejecutarlo solamente contra la base de pruebas y revisar esos UUID antes de limpiarlos; no hacer DELETE general por prefijo en una base operativa. Ver [runbook](../../setup.md) para levantar el entorno y [decisiones de adaptación](../../fase4-referencia-profesor.md).
