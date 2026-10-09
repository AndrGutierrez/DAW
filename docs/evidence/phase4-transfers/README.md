# Traslados de grupos: evidencia

Validación local del 8 de octubre de 2026. [Uso y API](../../fase4-traslados-pesaje.md).

- Frontend: 141 pruebas aprobadas en 25 archivos, incluyendo selección de 103 resultados repartidos entre páginas, capacidad del destino, conflicto de origen y reintento del mismo envío.
- Backend final: 241 UnitTests y 190 Core.Tests aprobados. Incluye regresión del endpoint de destinos para potreros activos sin coordenadas y referencias de ubicación en el listado de animales.
- PostgreSQL y navegador: cuatro casos aprobados, dos por viewport. Capacidad insuficiente revierte ubicaciones, historial y auditoría; el reintento no agrega movimientos; un conflicto del segundo animal revierte el primero. El recorrido de UI selecciona un lote de 14 animales con 12 visibles por página, comprueba selección completa en Pesaje y confirma los 14 traslados conservando el lote.
- Accesibilidad: cuatro combinaciones del nuevo módulo, claro/oscuro en escritorio/móvil, sin infracciones detectadas por axe. Se comprobó ausencia de desbordamiento horizontal. Esto no certifica cumplimiento completo de WCAG.
- API y SPA: compilaciones Docker aprobadas. La demo se actualizó conservando su volumen; las altas y movimientos de prueba pertenecen únicamente al proyecto PostgreSQL desechable.

## Fallos iniciales y correcciones

La primera ejecución de navegador falló en los cuatro casos: el test de auditoría utilizó una ruta incorrecta y la UI usó el endpoint del plano como catálogo de destinos, excluyendo potreros sin coordenadas. Se corrigió la ruta de prueba y se añadió GET /api/paddocks/destinations con ocupación real y validación de acceso a la finca. El mismo recorrido completo pasó después de corregirlo.

TypeScript detectó una opción exact no admitida por Testing Library en las nuevas pruebas de componentes; se retiró esa opción. Una ejecución de Core.Tests tuvo un fallo de bloqueo del manifiesto de recursos estáticos al lanzar otra compilación sobre los mismos archivos. La repetición secuencial final, sin compilaciones concurrentes, aprobó las 431 pruebas del backend. Los logs iniciales y finales se conservan por separado.

## Reproducción

Ejecuta las compilaciones y pruebas .NET de forma secuencial sobre el checkout. Los comandos usan las herramientas y credenciales indicadas por el runbook general; no se incluyen credenciales en esta evidencia.

1. dotnet test DAW.slnx -c Release.
2. En src/frontend: npm test -- --maxWorkers=2 y npm run build.
3. Levanta una instancia separada con PostgreSQL y semilla; configura E2E_BASE_URL, cuentas de prueba y E2E_ISOLATED_DATABASE=1.
4. En src/frontend: npm run test:e2e -- e2e/transfers.spec.ts. QUALITY_EVIDENCE_DIR permite exportar capturas.

Los logs se comprimen para conservar las salidas. Las trazas con solicitudes y las copias privadas no se publican. El límite del traslado es 500 movimientos por solicitud; el servidor vuelve a validar cada origen y la capacidad. La selección entre páginas no bloquea los registros mientras se lee.
