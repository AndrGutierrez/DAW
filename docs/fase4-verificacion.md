# Verificación de los incrementos de Fase 4

Fecha: 6 de octubre de 2026.

Se verificó la rama codex/phase4-cattle-spa en un entorno aislado. Los resultados cubren sesión persistente, consulta y edición del ganado, fotografías, crecimiento y pesaje consecutivo. El [estado de implementación](fase4-estado.md) distingue estos recorridos de la entrega completa.

## Resultados ejecutados

| Verificación | Primer incremento | Segundo incremento |
| --- | --- | --- |
| Core.Tests: dominio, infraestructura y pipeline HTTP | 108 aprobadas | 116 aprobadas |
| UnitTests: Core.Application con xUnit y Moq | 18 aprobadas | 42 aprobadas |
| Cliente con Vitest | 13 aprobadas | 28 aprobadas |
| Playwright en desktop y mobile | 12 aprobadas | 34 aprobadas |
| TypeScript y Vite | Aprobados | Aprobados en Windows y Docker |
| Imágenes API y Nginx | Construidas e iniciadas | Construidas e iniciadas |
| Dependencias frontend | Sin vulnerabilidades reportadas | Sin vulnerabilidades reportadas al instalar Recharts |
| PostgreSQL | 43 tablas | 43 tablas, sin migraciones de eliminación |

El segundo incremento tiene 158 pruebas .NET aprobadas en total. Los conteos no son porcentajes de cobertura; no se ha medido ni se afirma cobertura global del 100 %. La línea base anterior al primer incremento tenía 94 pruebas Core.Tests.

## Entorno y evidencia

PostgreSQL 15, API .NET 10 y SPA compilada servida por Nginx, bajo el proyecto Compose daw-phase4. Se utilizan puertos, volúmenes y credenciales de demostración propios. Las pruebas de navegador se ejecutan contra localhost:18086 sobre HTTP con la excepción explícita de Development. Esto no demuestra una publicación con HTTPS.

Las pruebas WebApplicationFactory utilizan una base EF InMemory aislada. Las pruebas UnitTests utilizan Moq y referencian Core.Application; no utilizan EF ni una base física. Playwright comprueba integración contra PostgreSQL real. La distinción importa: una prueba InMemory no demuestra el comportamiento de concurrencia de PostgreSQL.

Se revisaron visualmente el listado y el editor en escritorio, el formulario móvil y la ficha con Recharts en tema oscuro. Las comprobaciones de navegador verifican ausencia de desbordamiento horizontal en los recorridos principales. La revisión no equivale a una auditoría completa de accesibilidad ni de todos los navegadores.

Los archivos .env, capturas, perfiles del navegador y trazas permanecen fuera del control de versiones. No se modificó .github.

## Sesión persistente

El navegador comprueba ausencia de tokens en localStorage, sessionStorage y document.cookie; continuidad al recargar, abrir otra pestaña y cerrar/reabrir un perfil; logout comunicado entre pestañas; tema; credenciales incorrectas; búsqueda mediante API; URL directa y fotografías privadas mediante Blob.

Sobre PostgreSQL, seis intentos simultáneos de renovar una credencial producen una renovación válida y cinco rechazos 401. La reutilización posterior se rechaza y el sucesor válido puede renovarse. Las pruebas HTTP comprueban CSRF, atributos de cookies, hash en almacenamiento, compatibilidad con credenciales anteriores y aislamiento de fincas.

Los límites de revocación y de duración permanecen descritos en [la decisión de sesión](fase4-sesion.md).

## Animales, fotografías y pesajes

Los 17 escenarios de navegador se ejecutan en escritorio y móvil. Además de los seis de sesión, comprueban:

- Registro y edición con conservación de identificación oficial, RFID, nacimiento, lote, potrero, progenitor y notas.
- Fotografía original de más de 5 MB, compresión JPEG antes de subir, lado mayor de 1920 px y copia inferior al original y al límite de la API.
- Confirmación al eliminar una fotografía, incluyendo cancelación.
- GDP calculada con fechas y pesos reales; varios registros de un día conservados; objetivo explícito y aviso.
- Dos animales pesados consecutivamente sin solicitudes de navegación del documento; un peso inválido conserva la captura.
- Pérdida de respuesta después de una creación exitosa: el reintento devuelve 200 y queda un único registro.
- Identificación duplicada: error y valores del formulario conservados.
- Seis envíos concurrentes del mismo SubmissionId en PostgreSQL: una creación 201, respuestas restantes 200 o 409, confirmación posterior y un único peso.
- Animal que cambia a vendido después de formar la cola: rechazo al pesarlo y omisión explícita sin crear un registro.
- Empleado que registra pesos y consulta fotografías, con eliminación denegada tanto en la API como en la interfaz.
- Edición de nacimiento rechazada cuando invalidaría un pesaje anterior.
- Historial con 21 registros: solicitud de la página siguiente al servidor y conservación de la vista GDP.

Las pruebas Moq comprueban cálculo por días transcurridos, pérdidas de peso, ausencia de mediciones inventadas, selección determinista del día, separación de animales/fincas, autorización antes de leer, paginación y límites. La curva acotada conserva la GDP del primer punto visible utilizando su intervalo anterior.

Las pruebas de registro comprueban propiedad derivada de la ficha, identidad del envío, repetición sin escritura, colisiones de contenido/autor, rechazo de animales no activos, falta de acceso y valores inválidos. El pipeline HTTP verifica también autoría, auditoría, normalización y coherencia con el último peso de la ficha.

## Reproducción

Los comandos de .NET, frontend y E2E están en [la guía de sesión](fase4-sesion.md#verificación-reproducible). La suite de navegador requiere credenciales de administrador y empleado de una base de prueba aislada; no se deben usar datos de producción.

La suite crea sus propios animales y elimina sus pesos y fotografías al terminar. Los perfiles y trazas pueden contener datos y cookies del entorno de prueba. La colección Postman existente conserva el contrato anterior de autenticación.

El propósito, el comportamiento y los límites del nuevo recorrido se explican en [animales y pesaje consecutivo](fase4-animales.md).
