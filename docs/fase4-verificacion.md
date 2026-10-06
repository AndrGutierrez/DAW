# Verificación del incremento de sesión de Fase 4

Fecha: 6 de octubre de 2026.

Se verificó el primer incremento de la SPA con el código de la rama codex/phase4-cattle-spa. Las pruebas corresponden a sesión persistente, navegación de consulta del ganado y aislamiento de fincas. No demuestran una entrega completa de Fase 4; consultar [estado de implementación](fase4-estado.md).

## Resultados ejecutados

| Verificación | Resultado |
| --- | --- |
| Línea base anterior al cambio | 94 pruebas Core.Tests aprobadas |
| Backend y pipeline HTTP con el incremento | 108 pruebas Core.Tests aprobadas |
| Core.Application con xUnit y Moq | 18 pruebas UnitTests aprobadas, sin EF ni base física |
| Cliente de sesión | 13 pruebas Vitest aprobadas |
| Compilación de producción | TypeScript y Vite aprobados tanto en Windows como en la imagen Node de Docker |
| E2E | 12 pruebas Playwright aprobadas en los proyectos desktop y mobile |
| Dependencias frontend | npm audit sin vulnerabilidades reportadas en la ejecución |
| Esquema PostgreSQL | 43 tablas, incluyendo __EFMigrationsHistory; no se aplicó ninguna migración de eliminación |
| Formato y límites de trabajo | git diff --check sin errores; .github sin cambios |

Los números de pruebas no representan porcentajes de cobertura. No se ha medido ni se afirma cobertura global del 100 %.

## Entorno de integración

- PostgreSQL 15, API .NET 10 y SPA compilada servida por Nginx.
- Proyecto Compose aislado daw-phase4; puertos locales separados de la configuración habitual.
- Base recién migrada y sembrada con credenciales de demostración propias.
- Pruebas E2E ejecutadas contra HTTP en localhost, con la excepción explícita de Development. Esto no demuestra una publicación con HTTPS.
- Una prueba HTTP en HTTPS dentro de WebApplicationFactory comprueba el atributo Secure de las cookies.
- La revisión visual inspeccionó login móvil, listado de escritorio y ficha en tema oscuro.
- El perfil persistente de navegador de la prueba de reapertura se verifica en Chromium; las otras pantallas también se prueban con viewport y emulación móvil.

## Comportamientos comprobados

Las pruebas del navegador verificaron ausencia de tokens en localStorage, sessionStorage y document.cookie; conservación de la sesión al recargar, abrir otra pestaña y cerrar/reabrir un perfil de navegador; eliminación de la sesión y aviso a las otras pestañas al salir; búsqueda mediante solicitudes al servidor; ficha accesible por su URL directa; preferencia de tema y credenciales incorrectas; fotografías privadas cargadas como Blob y rechazadas para clientes anónimos.

Sobre PostgreSQL real, seis solicitudes simultáneas intentaron renovar el mismo token: una obtuvo 200 y las otras cinco recibieron 401. Un intento posterior de reutilización también recibió 401; el sucesor válido pudo renovarse.

Las pruebas HTTP adicionales comprobaron CSRF obligatorio, atributos de cookies, hash en almacenamiento, rechazo del hash como credencial, compatibilidad con tokens antiguos, revocación al salir, paginación acotada y ausencia de resultados y conteos para fincas no asignadas.

Las pruebas Moq comprobaron cambio de salud con trazabilidad, ausencia de historial duplicado al mantener el estado, rechazo por finca no asignada, animal inexistente o no activo, estado inválido y límites de consulta.

## Reproducción

Los comandos de backend, frontend y E2E están en [la guía de sesión](fase4-sesion.md#verificación-reproducible). La colección Postman existente conserva el contrato de autenticación anterior. Los archivos .env, perfiles del navegador, capturas y trazas no se incorporan al repositorio.
