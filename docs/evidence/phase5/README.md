# Evidencia de implementación — Fase 5

Fecha: 12 de octubre de 2026. Proyecto ganadero del grupo 3. Las pruebas con escritura se ejecutaron en un proyecto Compose desechable distinto de la demostración del equipo.

## Resultados y alcance

| Verificación | Resultado | Alcance |
| --- | --- | --- |
| UnitTests | 250 aprobadas | xUnit/Moq con dependencia simulada; incluye versiones e idempotencia del inventario y autorización de avisos/identificación |
| Core.Tests | 199 aprobadas | Pipeline HTTP/EF InMemory. No reemplaza las pruebas de transacciones PostgreSQL |
| Frontend, revisión de filtros | 198 aprobadas | Vitest; build TypeScript/Vite y mínimos de cobertura aprobados |
| Playwright, corrida completa desde base vacía | 157 aprobadas, 1 omisión intencional | Escritorio/móvil con PostgreSQL 15. La omisión corresponde al caso móvil Más dentro del proyecto desktop |
| Verificación posterior de enlaces | 24 aprobadas | phase5 y operations en ambas resoluciones; incluye apertura de finca/producto desde el aviso con filtros previos activos |
| Plano y capturas finales | 24 aprobadas | phase5 y paddocks en ambas resoluciones; se verificó el ajuste de presentación del plano |
| Filtros por mes, año y últimos días | 4 aprobadas | Dashboard/reportes en escritorio y móvil; consultas de la demostración local sin crear registros de negocio, con accesibilidad y ausencia de desbordamiento horizontal |
| HTTP/EF, revisión posterior | 3 aprobadas | Endpoints de identificación y notificaciones después de ajustar el enlace del producto |
| Newman | 16 solicitudes y 16 aserciones, cero fallos | Colección Cattle-Management con las credenciales temporales fuera del Git |
| Docker, corrección del 13 de octubre | API 68,5 MB; SPA 74,8 MB | Suma de capas descomprimidas; API autocontenida con compresión single-file. La medición anterior de 58,8/29,5 MB no acreditaba este límite |
| Compose | DB, API y SPA saludables; initialize terminado con éxito | Base nueva, migraciones y siembra anteriores al arranque HTTP |
| Workflow | actionlint aprobado | Comprobación local. Ejecución remota del cambio pendiente de publicar la rama/PR |
| Esquema y diccionario | 44 tablas, 507 columnas, 72 FK | Exportación física PostgreSQL, diagramas y diccionario regenerados |
| Dependencias frontend/generador | npm audit sin vulnerabilidades reportadas | Incluye corrección compatible de sharp a 0.35.5 en el generador de diagramas |

La corrida completa pasó después de corregir dependencias de los fixtures: puerto 18089 fijado en pruebas antiguas, filtro perdido al volver a entrar con Employee y posiciones de un fixture que se superponían al plano configurado. Se conserva la distinción entre esa corrida y las verificaciones posteriores. No se suman sus casos como pruebas diferentes.

## Demostración local

La configuración privada existente sirve la versión actualizada en http://localhost:18080. DB, API y SPA están saludables. La base local tenía migraciones anteriores pendientes; se aplicaron al inicializar. Se conservaron sus 8 filas de animales y 4 de inventario. La huella de las columnas originales del inventario coincide antes/después: la comparación excluye únicamente Version y los tres campos nuevos de conservación de registros. Esta comprobación no equivale a una huella de todas las tablas. Véase demo-preservation.json.

## Cobertura

La cobertura V8 mantiene todos los archivos fuente, incluidos módulos sin pruebas. La medición global de la revisión de filtros es 55,49% de líneas y 39,56% de ramas; consultar coverage-summary.json para los valores exactos de esta revisión. No acredita 100% global.

| Módulo nuevo | Líneas | Ramas |
| --- | --- | --- |
| Identificación y NDEF | 100% | 96,66% |
| AnimalScanner | 100% | 76,04% |
| AnimalLabel | 100% | 75% |
| NotificationCenter | 88,88% | 78,57% |
| Cálculo de períodos | 100% | 100% |
| PeriodFilter | 96,42% | 97,43% |

Hay mínimos globales y mínimos separados para estos módulos en vite.config.ts. Las pruebas Playwright no se agregan a los porcentajes V8. Las coberturas de .NET se registran por suite y tampoco se suman entre sí.

## Pruebas físicas y defensa

La carga de imagen QR utiliza la biblioteca real y un PNG con el identificador del animal; no es solo una simulación del valor de entrada. Las operaciones NFC se prueban con dobles NDEF para contenido, permisos, cancelación y opción de no sobrescribir una etiqueta. No acreditan lectura/grabación en un dispositivo físico ni compatibilidad de todos los aretes RFID pecuarios.

Se requiere un ensayo con dispositivo NFC compatible, NDEF y HTTPS, más la sustentación del equipo y la corrida/captura remota de GitHub Actions. No se presenta una corrida histórica de otro commit como validación del cambio.

## Archivos

Los informes .NET, logs seleccionados, tamaños de imagen y resumen de cobertura se guardan aquí sin contraseñas, tokens ni trazas de navegador. Los archivos gzip contienen su informe completo. Las capturas muestran datos sintéticos de prueba y el panel de filtros en la demostración local.

- [Guía de implementación y defensa](../../fase5-implementacion-y-verificacion.md).
- [Diccionario de datos](../../../db/data-dictionary.md).
- [Avisos en móvil](notifications-mobile.png).
- [Etiqueta QR](qr-desktop.png).
- [Destino visual del traslado](transfer-map-desktop.png).

- [Filtros en escritorio](period-filters-desktop.png).
- [Filtros en móvil](period-filters-mobile.png).

La revisión de filtros conserva el rango personalizado y añade mes, año y últimos 7/30/90/180/365 días, más una cantidad de 1 a 367 días. Los extremos son inclusivos y el mes/año actual termina hoy. El cálculo usa fechas de calendario UTC para coincidir con el contrato existente de la API. Los filtros se consultan al aplicar; cancelar conserva el período anterior. Las pruebas de contraste esperan a que termine la animación del panel. No se repitió toda la suite Playwright después de esta mejora; se conserva su corrida completa anterior y se distingue el seguimiento de cuatro casos.
