# Evidencia del módulo de auditoría

Verificación del 8 de octubre de 2026, ampliando la [revisión integral](../../fase4-auditoria-integral.md).

- Backend: 237 UnitTests y 169 Core.Tests; reportes TRX comprimidos de esta ejecución.
- Frontend: 117 Vitest; [cobertura V8](frontend-coverage-summary.json), con todos los módulos incluidos. El visor tiene 100% de líneas / 76% de ramas; el total 40,04% / 24,41%.
- Navegador: 133 Playwright aprobadas en PostgreSQL desechable, una omisión intencional para el caso exclusivo móvil en escritorio. Incluye los seis casos nuevos de auditoría, filtrado real, cambios anterior/posterior, permisos y accesos.
- Accesibilidad: [escritorio](accessibility-desktop.json) y [móvil](accessibility-mobile.json), con 48 combinaciones esenciales sin infracciones detectadas; dos detalles de auditoría adicionales pasan axe en la prueba del módulo. Las comprobaciones automatizadas no sustituyen una revisión completa de accesibilidad con usuarios.
- Docker API/SPA construidos y ejecutados; la migración de índices se aplicó conservando los datos de la demo. EF no detecta cambios de modelo pendientes.
- [Resumen de resultados y límites](validation-summary.json), [decisiones del módulo y conservación de información](../../fase4-registros-auditoria.md), [captura de la consulta lateral](../../screenshots/audit-log-detail.png).

Los reportes de rendimiento de este directorio son muestras locales de la navegación de pruebas; no acreditan el p75 de usuarios reales. No contienen tokens, contraseñas o URLs privadas. La captura muestra un registro sintético histórico ya existente; una IP ausente anterior no se recupera retroactivamente.
