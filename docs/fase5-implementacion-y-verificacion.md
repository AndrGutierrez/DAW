# Fase 5: implementación y verificación

Revisión del 12 de octubre de 2026. El dominio continúa siendo la gestión ganadera del grupo 3, identificada en [REAF-F4](https://gramirezsunet.github.io/desarrolloAplicacionesWeb/REAF-F4/). La asignación admite otros proyectos notificados al facilitador.

## Correspondencia con las observaciones

| Observación | Implementación | Comprobación |
| --- | --- | --- |
| Pruebas unitarias e integración | Casos de permisos, finca, etiquetas, datos obsoletos, cámara/NFC simulado, lectura QR real y navegación; mínimos de cobertura en Vitest | xUnit/Moq, HTTP/EF InMemory y Playwright con PostgreSQL 15, distinguiendo sus alcances |
| Notificaciones por rol | Encabezado con sanidad, retiro, crecimiento, capacidad e inventario; filtros por categoría y enlaces al registro | El servidor vuelve a verificar permisos y fincas. No se confía en el rol que declara el navegador |
| Concurrencia del inventario | Versiones persistidas y verificadas junto al saldo esperado; token de concurrencia EF, transacción Serializable e idempotencia | Dos retiros simultáneos: un éxito y un 409; volver al mismo saldo no habilita una escritura obsoleta |
| Potreros y traslados visuales | Plano seleccionable desde Traslados, ocupación proyectada, origen/destino y actualización de datos | Selección gráfica, confirmación, capacidad, reversión del lote y conservación del historial |
| QR/NFC | Lectura QR por cámara o imagen, identificación manual/lector externo, etiquetas descargables y lectura/grabación NDEF | Decodificación de un PNG QR real contra API/PG; NFC probado con dobles de la API del navegador |

Los avisos son condiciones activas calculadas desde datos persistidos, con vista limitada a 25 por categoría y total explícito. No constituyen un historial persistido de notificaciones. El plano es esquemático; no representa coordenadas GPS.

## Integridad y compatibilidad

La migración VersionedInventory añade solamente FarmInventory.Version, con valor inicial cero. No borra tablas, animales, existencias ni historiales. Cada modificación mediante la aplicación incrementa la versión. El cliente actual envía la versión al editar límites y registrar movimientos.

ExpectedVersion es opcional para conservar compatibilidad con los clientes anteriores; estos mantienen la comprobación de saldo y la transacción, pero no detectan por sí solos una secuencia que vuelve al mismo saldo. Los clientes actuales sí detectan ese caso. Un reintento con el mismo SubmissionId se reconoce antes de rechazar su versión antigua y nunca vuelve a descontar existencias.

Las etiquetas contienen un identificador del animal, sin credenciales ni permisos. La consulta exige seleccionar una finca accesible y vuelve a aplicar autorización en el servidor. Una coincidencia ambigua entre arete, RFID e identificación oficial se rechaza. Los QR de otros sitios no redirigen el navegador automáticamente.

## Cuatro dimensiones del dominio

| Dimensión | Modelo real | Impacto y límite |
| --- | --- | --- |
| Identificación comercial | Products.SKU/Name/Brand y Animals.InternalTag/OfficialId/Rfid | Identificación de insumos y trazabilidad bovina. Las identificaciones del animal se restringen por finca; la etiqueta interna utiliza su UUID |
| Gestión financiera | Products.CostPrice/Price y ExchangeRates | Costos y valores de referencia del inventario por finca; conversión USD/Bs con fecha. No equivale a implementar una contabilidad completa |
| Control y umbrales | FarmInventory.Stock/MinStock/MaxStock/Version y StockMovements | Reposición, exceso, saldo e historial de consumo. Los movimientos conservan cantidad, autor e idempotencia |
| Logística y almacén | FarmInventory.Location, Farms, Paddocks, Lots y AnimalMovements | Ubicación de insumos, distribución y capacidad del ganado; un traslado confirma el grupo completo o lo rechaza |

El [diccionario de datos](../db/data-dictionary.md) incluye todas las columnas físicas, tipos, nulabilidad, claves, índices únicos, CHECK y reglas de eliminación. Las tablas InventoryCategories y Users implementan los conceptos del ejemplo genérico mediante el catálogo real e Identity. La existencia de otras tablas modeladas no acredita por sí sola un flujo de negocio implementado.

## Contenerización y presupuesto

La exigencia se encuentra en 03_Tarea_Asignacion_Fase5_y_Rubrica.pdf: página 2, requisito 2; se repite en la rúbrica de la página 8. El material teórico la desarrolla en la sección 5.2, página 4. Son documentos originales de Classroom, conservados fuera del Git.

El backend se publica autocontenido para Linux musl, con el runtime incluido y sin herramientas de diseño EF en producción. El contenedor final utiliza runtime-deps Alpine. No se aplica trimming ni Native AOT a los controladores o EF. La compilación selecciona x64 o arm64 según la arquitectura del contenedor de construcción; esta verificación utiliza x64.

La SPA se compila con Node 24 y se sirve con Nginx. La comprobación usa el tamaño local completo informado por Docker, en bytes: cada imagen debe ser estrictamente menor que 120000000 bytes. No se confunde el tamaño comprimido de un registro con el tamaño local de la imagen.

~~~bash
bash scripts/compose.sh up --build -d --wait
bash scripts/compose.sh ps -a
bash scripts/verify-image-size.sh daw-api daw-nginx
~~~

Los nombres de imagen dependen de COMPOSE_PROJECT_NAME. Los servicios existentes son db, blazorapp (API), nginx (SPA) e initialize. initialize migra y siembra antes de servir; termina con éxito y no permanece como servicio HTTP. DB, API y SPA tienen comprobaciones de salud; Nginx espera a la API saludable. La API comprueba conectividad con la base. No implica que el healthcheck pruebe cada función de negocio.

La API se expone además en 127.0.0.1:5000 por defecto; API_HTTP_PORT permite cambiar ese puerto. La SPA conserva NGINX_HTTP_PORT de la configuración privada. El esquema de ejemplo del profesor utiliza 8080/5000/5432; una instalación existente puede conservar otros puertos publicados.

## CI y entrega

GitHub Actions se ejecuta en push a main, pull_request hacia main y ejecución manual. Corre backend/cobertura, frontend/cobertura/build y una pila PostgreSQL 15 con Newman y toda la suite Playwright. Verifica también el presupuesto de ambas imágenes. La publicación de los artefactos compilados depende del éxito de los tres trabajos. Los informes de diagnóstico pueden publicarse aunque falle una prueba; los artefactos de entrega no.

El workflow se valida localmente con actionlint. La corrida remota de este cambio y su captura requieren publicar la rama/PR. No se utiliza una ejecución histórica de Fase 4 como evidencia de este código.

Las pruebas que escriben datos se ejecutan contra un proyecto Compose separado, con sus propios volúmenes y credenciales temporales. E2E_ISOLATED_DATABASE expresa esa intención; no crea ni aísla una base por sí sola.

## Sustentación sugerida

1. Levantar la pila, mostrar las tres comprobaciones de salud y medir las imágenes con el script.
2. Mostrar un aviso de sanidad o existencias; abrir su registro y explicar el filtrado por permisos/finca.
3. Descargar un QR de un animal y cargar la imagen desde el lector; demostrar un rechazo de finca incorrecta.
4. Seleccionar un animal, elegir su destino en el plano y contrastar ocupación actual/proyectada antes de confirmar.
5. Explicar un test de inventario mediante Arrange/Act/Assert: saldo y versión inicial, dependencia Moq, intento obsoleto y verificación de que no escribió.
6. Mostrar en PostgreSQL la prueba de retiros concurrentes, el único movimiento confirmado y su auditoría.
7. Presentar el diccionario y relacionar las cuatro dimensiones con ganado e insumos.

## Verificaciones que requieren el entorno de defensa

Web NFC necesita NDEF, dispositivo compatible y HTTPS. No todos los aretes RFID pecuarios son NFC; un lector externo puede aportar la identificación mediante el campo manual. La grabación solicita una etiqueta vacía y no sobrescribe contenido existente. Las pruebas automatizadas de NFC verifican integración y cancelación con dobles, no una lectura física. Referencia: [Web NFC en Chrome](https://developer.chrome.com/docs/capabilities/nfc).

La cámara QR necesita permiso y contexto seguro; la carga de imagen ofrece otro flujo comprobado. El ensayo físico, acceso HTTPS desde otro equipo, ejecución remota de CI y defensa oral conservan verificaciones propias. La [evidencia de esta revisión](evidence/phase5/README.md) informa conteos y límites de cobertura sin afirmar 100% global.

## Filtros de período

Dashboard y reportes comparten PeriodFilter. Se puede elegir un mes, un año, últimos 7/30/90/180/365 días, una cantidad personalizada de 1 a 367 días o fechas manuales. El rango calculado aparece antes de consultar y puede convertirse en fechas editables. Mes y año actuales terminan hoy; los días recientes incluyen hoy. Los años bisiestos y cambios de año se calculan con fechas UTC, como exige el contrato existente de la API.

La implementación está en src/frontend/src/api/periods.ts y src/frontend/src/components/PeriodFilter.tsx. La revisión completa del frontend pasó 198 pruebas y el build; cuatro escenarios Playwright verificaron consultas reales en escritorio/móvil, respuesta HTTP 200, foco, accesibilidad y desbordamiento. Las pruebas de navegador no crean registros de negocio. Los resultados y capturas se encuentran en docs/evidence/phase5.
