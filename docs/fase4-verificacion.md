# Verificación de los incrementos de Fase 4

Fecha: 7 y 8 de octubre de 2026.

Se verificó la rama codex/phase4-cattle-spa en un entorno aislado. Los resultados cubren sesión persistente, consulta y edición del ganado, fotografías, crecimiento, pesaje consecutivo, sanidad, reproducción, retiro, producción por animal, ocupación de potreros, traslados y la interfaz de la finca. El [estado de implementación](fase4-estado.md) distingue estos recorridos de la entrega completa.

## Resultados ejecutados

| Verificación | Primer incremento | Segundo incremento | Tercer incremento | Cuarto incremento | Quinto incremento: UI |
| --- | --- | --- | --- | --- | --- |
| Core.Tests: dominio, infraestructura y pipeline HTTP | 108 aprobadas | 116 aprobadas | 124 aprobadas | 134 aprobadas | Sin cambios de backend; no repetidas |
| UnitTests: Core.Application con xUnit y Moq | 18 aprobadas | 42 aprobadas | 72 aprobadas | 91 aprobadas | Sin cambios de backend; no repetidas |
| Cliente con Vitest | 13 aprobadas | 28 aprobadas | 28 aprobadas | 41 aprobadas | 41 aprobadas |
| Playwright en desktop y mobile | 12 aprobadas | 34 aprobadas | 46 aprobadas | 60 aprobadas | 70 verificadas (68 + 2 reejecutadas) |
| TypeScript y Vite | Aprobados | Aprobados en Windows y Docker | Aprobados en Windows y Docker | Aprobados en Windows y Docker | Aprobados en Windows y Docker |
| Imágenes API y Nginx | Construidas e iniciadas | Construidas e iniciadas | Construidas e iniciadas | Construidas e iniciadas | API conservada; Nginx construido e iniciado |
| Dependencias frontend | Sin vulnerabilidades reportadas | Sin vulnerabilidades reportadas al instalar Recharts | Sin cambios de dependencias | Sin cambios de dependencias | Sin cambios de dependencias |
| PostgreSQL | 43 tablas | 43 tablas, sin migraciones de eliminación | 43 tablas, sin cambios de esquema | 43 tablas, sin cambios de esquema | 43 tablas, sin cambios de esquema |

El cuarto incremento tiene 225 pruebas .NET aprobadas en total; el tercero tenía 196 y el segundo 158. El quinto modifica solamente el frontend, por lo que no repite las suites .NET. Los conteos no son porcentajes de cobertura; no se ha medido ni se afirma cobertura global del 100 %. La línea base anterior al primer incremento tenía 94 pruebas Core.Tests.

## Entorno y evidencia

PostgreSQL 15, API .NET 10 y SPA compilada servida por Nginx, bajo el proyecto Compose daw-phase4. Se utilizan puertos, volúmenes y credenciales de demostración propios. Las pruebas de navegador se ejecutan contra localhost:18086 sobre HTTP con la excepción explícita de Development. Esto no demuestra una publicación con HTTPS.

Las pruebas WebApplicationFactory utilizan una base EF InMemory aislada. Las pruebas UnitTests utilizan Moq y referencian Core.Application; no utilizan EF ni una base física. Playwright comprueba integración contra PostgreSQL real. La distinción importa: una prueba InMemory no demuestra el comportamiento de concurrencia de PostgreSQL.

Se revisaron visualmente el listado y el editor en escritorio, el formulario móvil y la ficha con Recharts en tema oscuro. Las comprobaciones de navegador verifican ausencia de desbordamiento horizontal en los recorridos principales. Sanidad y el mapa de potreros se revisaron también en escritorio y en móvil oscuro. El detalle de ocupación se revisó con filtros por lote. La nueva interfaz se revisó en acceso, listado, ficha, editor y potreros, tanto en escritorio como en móvil y ambos temas. La revisión no equivale a una auditoría completa de accesibilidad ni de todos los navegadores.

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

## Verificación reproducible

Los comandos de .NET, frontend y E2E están en [la guía de sesión](fase4-sesion.md#verificación-reproducible). La suite de navegador requiere credenciales de administrador y empleado de una base de prueba aislada; no se deben usar datos de producción.

La suite de animales elimina sus pesos y fotografías al terminar. Los casos clínicos generan un manifiesto opcional mediante E2E_CARE_FIXTURE_OUTPUT: sus eventos son históricos y no tienen DELETE público. En esta verificación se limpiaron exclusivamente los identificadores de fixtures creados por la suite, en la base aislada, después de comprobar su prefijo y propiedad. Las dos ejecuciones completas de esta verificación crearon 36 animales clínicos de prueba: se eliminaron mediante el manifiesto validado, conservando los datos de demostración. Los casos de potreros limpian sus animales, lotes y potreros mediante la API; se comprobó que no quedaran fixtures de ambos bloques. Una ejecución clínica sin limpieza conservará esos animales como datos de prueba. Los perfiles y trazas pueden contener datos y cookies del entorno de prueba. La colección Postman existente conserva el contrato anterior de autenticación.

El propósito, el comportamiento y los límites del nuevo recorrido se explican en [animales y pesaje consecutivo](fase4-animales.md).

## Sanidad, reproducción y producción

Los seis escenarios nuevos de navegador se ejecutan en escritorio y móvil sobre PostgreSQL:

- Tratamiento con producto, dosis y retiro guardados; rechazo de leche y sacrificio durante el período y conservación del animal activo.
- Diagnóstico positivo seguido de parto; cambio del estado reproductivo y navegación por el árbol genealógico.
- Respuesta clínica perdida después del 201; campos bloqueados, reintento del cuerpo original y una sola fila persistida.
- Historial de 11 eventos: diez en la primera página y uno en la segunda, con solicitud al servidor. Employee registra un cambio de salud con motivo, fecha de actualización e historial.
- Persistencia de los cinco tipos de evento sanitario y los siete reproductivos; búsqueda de descendientes limitada a la madre.
- Tratamiento y leche enviados simultáneamente: uno devuelve 201, el otro 409 y solamente queda una de las dos operaciones.

Las pruebas Moq comprueban días inclusivos, última administración, retiro cero, períodos superpuestos, historia incompleta, snapshot del catálogo, colisiones/repetición, autorización antes de leer y tratamientos retroactivos incompatibles con leche o sacrificio. La reproducción comprueba sexo, reproductor, cantidades y repetición con autor.

El pipeline HTTP comprueba el límite final y la liberación al día siguiente en las rutas original y nueva; eventos con auditoría, identidad protegida, estado reproductivo por fecha, producción idempotente y fincas no asignadas. InMemory cubre ese pipeline; la carrera entre tratamiento y leche se verifica separadamente con PostgreSQL físico.

El alcance y las limitaciones se explican en [sanidad y reproducción](fase4-sanidad.md), incluyendo el bloqueo conservador del registro de leche y la ausencia de venta/descarte, corrección de eventos y consumo automático de inventario.

## Potreros y traslados

Los siete escenarios nuevos se ejecutan en escritorio y móvil contra PostgreSQL:

- Mapa con ocupación activa, capacidad, superficie, densidad descriptiva y permanencia desde una entrada registrada; traslado y consulta desde la ficha del animal.
- Destino completo: rechazo 409 y conservación de los valores capturados.
- Dos animales compiten por el último cupo: una creación 201 y un rechazo 409, con un único ocupante persistido.
- Administrador y empleado mantienen potreros según sus permisos existentes; Employee conserva create/update y recibe 403 al intentar DELETE.
- Respuesta perdida después del 201: reintento del mismo cuerpo, respuesta 200 y una sola fila de movimiento.
- Otro usuario cambia la ubicación: rechazo del origen obsoleto, actualización explícita de la ficha y nuevo traslado desde el origen vigente.
- Filtro de residentes por lote aplicado en el servidor antes del conteo y la paginación.

Las pruebas Moq cubren autorización, capacidad, estado activo, compatibilidad de finca/especie, identidad del envío y origen esperado. El pipeline HTTP comprueba también las rutas originales de creación/edición del animal, reactivación, reducción de capacidad, desactivación de un potrero ocupado y ausencia de fechas inventadas para registros anteriores. Los cambios solamente de lote conservan la fecha de entrada física. Las referencias antiguas entre fincas no se incluyen en la ocupación consultada.

La concurrencia por el último cupo se demuestra con PostgreSQL real; InMemory no demuestra esa garantía. No se añadieron migraciones. Después del reinicio se recuperó Docker conservando los volúmenes existentes. El alcance y sus límites se explican en [potreros y trazabilidad](fase4-potreros.md).

## Sistema visual y protección de editores

Los cinco escenarios de UI se ejecutaron en escritorio y móvil:

- Texto y estados semánticos en ambos temas, incluyendo salud crítica, etiquetas y acciones: contraste de al menos 4.5:1 en los elementos seleccionados.
- Un editor de animal modificado conserva los valores al cancelar una salida mediante menú, Atrás del navegador o logout; confirmar la salida no guarda la ficha.
- El editor de potrero conserva la capacidad capturada al cancelar la navegación y admite alternar el checkbox con teclado. Descartar no modifica la capacidad persistida.
- Las pestañas muestran un solo panel, admiten flechas/Inicio/Fin y Enter/Espacio, conservan valores al alternarlas y recuperan la selección al recargar.
- El detalle de ocupación abre como diálogo lateral, contiene el foco, cierra con Escape y devuelve el foco al activador; cancelar un descarte conserva el motivo de traslado. No hay desbordamiento horizontal del documento.

El escenario existente de creación/edición verifica que elegir lote conserva el potrero vacío hasta que el usuario lo asigna expresamente. Los 60 casos anteriores continúan aprobados: sesión persistente, fotos privadas, edición, pesaje, sanidad/reproducción, retiro, producción, capacidad y traslados. El total actual es de 70 escenarios verificados. En esta revisión la ejecución completa aprobó 68; los dos casos de acceso directo todavía esperaban Genealogía visible por defecto. Tras actualizar esa expectativa para seleccionar la pestaña y comprobar su persistencia, ambos casos pasaron en la reejecución dirigida, sin cambios adicionales en la aplicación.

La regresión completa creó 18 animales clínicos de prueba, identificados mediante su manifiesto. La limpieza comprobó propiedad y ausencia de descendientes externos antes de eliminarlos, conservando la demostración. Se verificaron cero fixtures clínicos, de potreros y de UI restantes. La suite de UI limpia sus animales y potreros por API.

Después del ajuste final de contraste de los campos se repitió el escenario de colores en ambas resoluciones: dos casos aprobados. La comprobación también mide las ayudas de entrada (al menos 4.5:1) y los bordes de los controles seleccionados (al menos 3:1). Los resultados no sustituyen una auditoría completa de accesibilidad. El alcance y los límites están en [interfaz de la finca](fase4-ui.md).


## Inventario, dashboard y reportes (7 y 8 de octubre, fecha local)

- .NET: 109 pruebas UnitTests/Moq y 140 Core.Tests, 249 aprobadas. Incluyen precisión del saldo, insuficiencia, saldo obsoleto, reintentos, permisos, vínculos entre fincas, protección de referencias y rechazo de una exportación con más de 10.000 filas. La rotación no se calcula sobre el día incompleto de apertura.
- Frontend: 41 pruebas Vitest aprobadas, TypeScript y build Vite correctos en Windows/Git Bash y Docker. El build avisa del chunk de ExcelJS cargado bajo demanda. npm audit del frontend: cero vulnerabilidades conocidas en el árbol instalado.
- Playwright: 84 escenarios aprobados en una ejecución completa, escritorio y móvil, con PostgreSQL físico. Los siete escenarios nuevos por resolución cubren reintento tras perder la respuesta de un retiro, saldo obsoleto con conservación del formulario, dos retiros por el mismo saldo, permisos del empleado, edición de catálogos/límites y valoración, exportaciones clínicas/productivas y actualización del dashboard tras un minuto mediante reloj controlado.
- Reanudación del 8 de octubre: 14 escenarios dirigidos de operaciones aprobados sobre las imágenes finales, después del ajuste de espaciado móvil. Se verificó el inventario cargado a 390 px sin desbordamiento horizontal. Las exportaciones se volvieron a descargar y validar.
- Exportaciones: ocho archivos descargados (clínico/producción × XLSX/PDF × escritorio/móvil). Se verificaron fechas Excel tipadas, cantidades numéricas, filtros, inmovilización del encabezado, acentos y ausencia de fórmulas. Una observación que comienza con =SUM(A1:A2) permanece como texto. Se inspeccionaron las hojas mediante Artifact Tool y los PDF mediante extracción de texto y renderizado Poppler. Estas verificaciones usan datos de prueba; no equivalen a pruebas de carga o compatibilidad con todas las versiones de Excel.
- Base: migración aplicada, StockMovements.Quantity con precisión 14/4, 43 tablas; esquema/ERD con 72 claves externas. Limpieza transaccional de 38 productos/categorías/inventarios, 58 movimientos y 42 animales de los escenarios de esta sesión, con manifiestos, comprobación de códigos/fechas y rechazo de descendientes externos. Cero animales P4 y productos P4-OPS restantes; DEMO-001 y DEMO-F4-AURORA conservados.

La ejecución dirigida de la reanudación generó otros 14 productos/categorías/inventarios, 22 movimientos, dos animales, dos eventos sanitarios y dos registros de producción. Se retiraron mediante sus 14 manifiestos y comprobaciones de propiedad, junto con 92 registros de auditoría asociados. Se volvió a confirmar que no quedaran animales P4 ni productos P4-OPS y que los dos animales de demostración siguieran presentes.

El alcance, fórmulas, supuestos y límites están en [operaciones](fase4-operaciones.md). No se modificó .github ni se publicó la rama. Falta la revisión final de entrega y evidencia académica.

## Pulido de UI y acceso (8 de octubre)

- Recursos: Phosphor React 2.1.10, Source Sans 3 variable local y Radix Popover 1.2.0 integrados. Se incluyen licencias de los recursos. La fuente fue confirmada cargada por document.fonts; el login no solicitó recursos externos ni el chunk del selector avanzado. Se comprobó la preferencia de movimiento reducido con animation-name: none.
- Frontend: TypeScript y Vite aprobados en Windows/Git Bash y en la imagen Docker; 43 Vitest aprobadas. Los dos casos nuevos distinguen expiración autenticada de inicio anónimo y logout explícito. npm audit del frontend: cero vulnerabilidades conocidas en el árbol instalado.
- Navegador: 94 recorridos aprobados en una ejecución completa de escritorio y móvil contra PostgreSQL. Los diez casos añadidos cubren mostrar/ocultar contraseña con foco y selección conservados, Enter sin duplicación durante envío, errores asociados a campos, avisos de credenciales/conexión con datos conservados, expiración y retorno al destino, búsqueda de animales con 21 candidatos y segunda página, teclado y el selector dentro del diálogo de inventario. Se corrigió y verificó el retorno de foco en ese diálogo; también se comprobó el cierre al pulsar un área exterior visible, sin cerrar el panel.
- Revisión visual: login claro en escritorio y móvil, aviso de credenciales móvil y selector en tema oscuro. Se comprobaron anchos de documento de 390/1365 px iguales al viewport y límites verticales del selector. Las pruebas de contraste seleccionado en ambos temas, pestañas, foco, checkbox, cambios sin guardar y panel de potreros pasaron dentro de la regresión completa.
- Exportaciones: las ocho descargas de XLSX/PDF de la regresión volvieron a pasar validación de fechas, cantidades, acentos y texto sin fórmulas.
- Limpieza: la suite nueva retiró sus animales sin historial mediante la API. Se retiraron además 14 productos/categorías/inventarios, 22 movimientos y 20 animales de operaciones/sanidad mediante los manifiestos de esta ejecución, con comprobaciones de propiedad y referencias externas; se eliminaron 226 auditorías asociadas. Cero animales P4 y productos P4-OPS restantes; DEMO-001 y DEMO-F4-AURORA conservados.

No se cambió el backend, el esquema ni .github. Las suites .NET no se repitieron para este pulido exclusivo del frontend. El alcance y límites se explican en [pulido de UI](fase4-ui-pulido.md). Estos resultados no sustituyen una auditoría completa de accesibilidad ni el cierre académico de Fase 4.

## Revisión de entrega: ubicación, alertas y curva diaria por lote

El 8 de octubre de 2026 se corrigió Ubicación para reutilizar animal-details: padding de 26 px en escritorio y 20 px a 390 px. Se comprobó la apertura de traslado y ausencia de desbordamiento. No se cambió el padding global de los paneles.

Se agregó aviso automático ante descenso del último peso diario frente al anterior, separado de objetivos positivos explícitos. La analítica incorporó series diarias de leche por finca/lote actuales, con IDs, litros y fechas; los días ausentes son huecos. Los selectores usan composición responsiva Tailwind y la tabla permite consultar los valores. Se ajustaron los mensajes de conflicto del catálogo y el máximo de 100 caracteres de categoría.

- .NET: 122 UnitTests/Moq y 141 Core.Tests, **263 Passed**. Las nuevas pruebas de catálogo ejercen las implementaciones reales de Application y verifican éxitos, categorías inválidas, SKU duplicado, precios y unidad con inventario. Core.Tests prueba el nuevo contrato JSON, filtro de finca y lectura por IDs.
- Cliente: TypeScript y build Vite/Docker aprobados; **43 pruebas Vitest Passed**.
- Navegador: **10 pruebas dirigidas Passed** en desktop/mobile contra API y PostgreSQL físicos: curva diaria, pérdida de peso, objetivos, pestañas y legibilidad. Después del ajuste responsivo de selectores se repitió la curva: **2 Passed**. No se reejecutó la regresión completa anterior de 94 casos.
- Cobertura: collector real, reportes separados de UnitTests y Core.Tests. Core.Application tiene 43,35 % de líneas en la suite aislada y 91,81 % en Core.Tests; no se suman los porcentajes ni se afirma 100 % global.
- Fixtures: pesos/animales/lotes/producción de los nuevos casos se eliminaron por API; no se alteraron los animales de demostración. No se ejecutó seed ni migración en la instancia vigente.

Se conservaron [TRX, salidas y cobertura completas comprimidas](evidence/phase4-audit/README.md), además de capturas de Ubicación y la curva diaria. La [auditoría](fase4-auditoria.md) diferencia lo que falta para entrega, las desviaciones acordadas y las ampliaciones de negocio.

## Portada y referencia del profesor — 8 de octubre de 2026

Se revisó el repositorio de ejemplo en b0d3b9ad67ee0f4aa2b60335f168ed5bf314c4c4 y la página REAF-F4. Se adaptó el inicio por rol, la prioridad de los KPI ganaderos y el acceso al reporte con los filtros del dashboard. La galería README y la guía ahora muestran la SPA.

TypeScript, build Docker/Vite y 55 pruebas Vitest aprobados. Playwright dirigido: portada por rol, login directo, recuperación de sesión, destino con pestaña/fragmento, curva diaria/KPI/filtros y actualización. Los [reportes de este incremento](evidence/phase4-home/README.md) registran los conteos finales. No se reejecutaron las suites .NET ni toda la regresión del navegador; no hubo cambios de backend.

## Gestión de usuarios y cierre técnico — 8 de octubre de 2026

Se leyó el instrumento nuevo del profesor para el 9 de octubre: 40 puntos, cuatro criterios de 10. Se implementaron cuentas/roles/fincas/permisos, desactivación y restablecimiento con SecurityStamp y revocación; objetivos GDP persistentes/avisos por finca; plano espacial y permanencia configurable; servicio initialize y Postman operativo/cookie/CSRF.

- .NET: 153 UnitTests/Moq + 144 Core.Tests, 297 Passed. Cobertura Core.Application separada: UnitTests 48,29 % líneas/50,37 % ramas; Core.Tests 88,32 % líneas/58,28 % ramas.
- Cliente: 61 Vitest Passed, TypeScript y build Docker/Vite correctos. Se mantiene el aviso de tamaño del chunk de ExcelJS cargado bajo demanda.
- Navegador dirigido: 49/50 en la corrida y 2/2 en la reejecución del selector tras esperar la segunda página; 50 escenarios distintos comprobados. Incluye seis casos nuevos de usuarios/seguimiento/plano en desktop/mobile. No equivale a repetir toda la regresión histórica.
- Newman: 26 requests, 37 assertions, cero fallos. Comprueba también cookie HttpOnly, CSRF, refresh y cierre, sin incluir el entorno secreto en Git.
- PostgreSQL: volumen vacío aislado, Compose en un comando, initialize correcto, seis migraciones, login 200 y dos posiciones DEMO. Actualización existente conservada; esquema/DER regenerados, 43 tablas y 72 FK. EF declara sin cambios de modelo pendientes.
- Limpieza: pesos/animales/potreros nuevos se eliminaron por API y objetivos restaurados; cuentas temporales, incluidas las dos de corridas interrumpidas, se conservaron desactivadas.

[Reportes completos comprimidos y resumen sin credenciales](evidence/phase4-final/README.md), [guía de evaluación](fase4-evaluacion-09-octubre.md) y [gestión de usuarios](fase4-usuarios.md). La publicación remota y la defensa no se presentan como realizadas.

## Cierre posterior: requisitos web e instrumento vigente

Revisión del 8 de octubre de 2026: cohortes reproductivas bovinas, stream SSE autorizado, referencia USD/Bs fechada de ingreso manual y aislamiento Moq ampliado.

- .NET: **235 UnitTests y 156 Core.Tests Passed**. Cobertura de Core.Application aislada: 68,56 % líneas / 67,88 % ramas; integración: 88,69 % / 58,76 %.
- Cliente: **65 Vitest Passed**, TypeScript y Docker/Vite correctos. ExcelJS permanece bajo demanda con aviso de tamaño de chunk.
- Navegador: **16/16 Passed en una corrida**, portada/analytics/evaluation en escritorio y móvil sobre PostgreSQL aislado. Una producción de Employee actualiza otra sesión Admin por SSE sin recargar.
- Newman: **30 solicitudes, 44 assertions, cero fallos**. Resumen sin credenciales; referencias sintéticas solo en el entorno desechable.
- PostgreSQL: siete migraciones desde volumen nuevo, 44 tablas/72 FK; EF sin cambios de modelo pendientes. Actualización aditiva de la demostración existente, sin reiniciar su volumen.

Los [reportes de este incremento](evidence/phase4-evaluation/README.md) conservan los alcances separados. Los [métodos y límites](fase4-cierre-evaluacion.md) explican cohortes observadas, broker de una instancia y tasa manual. La presentación/formulario siguen siendo actos del equipo.
