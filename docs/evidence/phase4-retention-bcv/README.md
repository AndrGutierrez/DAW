# Evidencia: conservación y BCV automático

Revisión del 8 de octubre de 2026. [Resumen estructurado](validation-summary.json) · [comportamiento y decisiones](../../fase4-conservacion-bcv.md).

| Verificación | Resultado y alcance |
| --- | --- |
| UnitTests | 237 aprobadas; aislamiento xUnit/Moq de Application |
| Core.Tests | 188 aprobadas; incluye archivado/restauración, permisos, seed, genealogía y proveedor BCV/fallos; EF InMemory no sustituye PostgreSQL |
| Vitest | 123 aprobadas; interacción, confirmación cancelada, restauración rechazada y conservación de valoración ante fallo |
| Build | TypeScript/Vite y Docker de API/SPA aprobados |
| PostgreSQL / Playwright | Primera regresión: 137 aprobadas, dos fallos y una omisión exclusiva móvil. Repetición focalizada: ocho aprobadas, incluyendo los dos casos afectados. Resultado final de 139 casos distintos aprobado; no se presenta como una única corrida limpia |
| Accesibilidad | 52 combinaciones esenciales de pantalla/tema/viewport sin infracciones axe detectadas; dos detalles de papelera adicionales aprobados. No constituye certificación WCAG completa |
| Migraciones | Nueve aplicadas en base vacía y demo existente; cero cambios de modelo pendientes |
| Esquema | 44 tablas / 72 FK; SQL y DER regenerados desde PostgreSQL |
| BCV demo | 874,7321 Bs/USD con fecha valor 2026-10-08; consulta automática real a BCV Today y fuente exacta guardada |

Los fallos iniciales están identificados: la medición móvil ocurrió durante la animación de opacidad del lateral; la prueba espera ahora fuentes y animaciones finitas antes de medir contraste. Usuarios/escritorio recibió ERR_NO_BUFFER_SPACE de Chromium, con pantalla sin recursos; el recorrido completo de creación, restablecimiento y desactivación pasó después en ambos tamaños. Se conservan los logs inicial y de repetición; no se excluyeron los casos ni se añadieron reintentos automáticos.

V8 global: **41,96% de líneas / 25,97% de ramas**. Papelera: 95,45% / 82,05%. La valoración tiene 69,23% / 60%. Los recorridos E2E no se suman a la cobertura V8. Los reportes .NET conservan la cobertura por suite en formato Cobertura; no se presenta como 100%.

Los TRX y XML se comprimen para mantener sus resultados íntegros. Los JSON de rendimiento no incluyen tokens ni cuerpos de solicitud. Las trazas de Playwright, .env, credenciales y copia de seguridad privada no se publican. Las cotizaciones sintéticas de pruebas están limitadas al proyecto daw-phase4-empty, desechable; la demo usa la referencia real indicada.

## Reproducir

Desde Git Bash, con Docker Desktop y .env configurado:

~~~bash
docker compose up --build -d --wait
MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd -W):/src" -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet test DAW.slnx --collect:"XPlat Code Coverage"
npm ci --prefix src/frontend
npm run test:coverage --prefix src/frontend
npm run build --prefix src/frontend
~~~

Playwright necesita una instancia desechable separada, E2E_BASE_URL=http://localhost:18089 y E2E_ISOLATED_DATABASE=1, además de las credenciales del seed aportadas localmente. Desactivar BCV_AUTO_SYNC en esa instancia para que las pruebas no dependan de servicios externos. Ejecutar npm run test:e2e desde src/frontend con esas variables. Retirar únicamente su proyecto/volúmenes al concluir; nunca usar la base de demostración para las pruebas de mutación.

[Detalle de papelera en escritorio](archive-detail-desktop.png) · [detalle móvil](archive-detail-mobile.png) · [cobertura frontend](frontend-coverage-summary.json). Los archivos performance y accessibility identifican cada viewport. La defensa presencial, publicación/entrega de Classroom y acceso desde el equipo final deben comprobarse por el equipo.
