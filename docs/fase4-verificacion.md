# Verificación de los incrementos de Fase 4

Fecha: 7 de octubre de 2026.

Se verificó la rama codex/phase4-cattle-spa en un entorno aislado. Los resultados cubren sesión persistente, consulta y edición del ganado, fotografías, crecimiento, pesaje consecutivo, sanidad, reproducción, retiro, producción por animal, ocupación de potreros y traslados. El [estado de implementación](fase4-estado.md) distingue estos recorridos de la entrega completa.

## Resultados ejecutados

| Verificación | Primer incremento | Segundo incremento | Tercer incremento | Cuarto incremento |
| --- | --- | --- | --- | --- |
| Core.Tests: dominio, infraestructura y pipeline HTTP | 108 aprobadas | 116 aprobadas | 124 aprobadas | 134 aprobadas |
| UnitTests: Core.Application con xUnit y Moq | 18 aprobadas | 42 aprobadas | 72 aprobadas | 91 aprobadas |
| Cliente con Vitest | 13 aprobadas | 28 aprobadas | 28 aprobadas | 41 aprobadas |
| Playwright en desktop y mobile | 12 aprobadas | 34 aprobadas | 46 aprobadas | 60 aprobadas |
| TypeScript y Vite | Aprobados | Aprobados en Windows y Docker | Aprobados en Windows y Docker | Aprobados en Windows y Docker |
| Imágenes API y Nginx | Construidas e iniciadas | Construidas e iniciadas | Construidas e iniciadas | Construidas e iniciadas |
| Dependencias frontend | Sin vulnerabilidades reportadas | Sin vulnerabilidades reportadas al instalar Recharts | Sin cambios de dependencias | Sin cambios de dependencias |
| PostgreSQL | 43 tablas | 43 tablas, sin migraciones de eliminación | 43 tablas, sin cambios de esquema | 43 tablas, sin cambios de esquema |

El cuarto incremento tiene 225 pruebas .NET aprobadas en total; el tercero tenía 196 y el segundo 158. Los conteos no son porcentajes de cobertura; no se ha medido ni se afirma cobertura global del 100 %. La línea base anterior al primer incremento tenía 94 pruebas Core.Tests.

## Entorno y evidencia

PostgreSQL 15, API .NET 10 y SPA compilada servida por Nginx, bajo el proyecto Compose daw-phase4. Se utilizan puertos, volúmenes y credenciales de demostración propios. Las pruebas de navegador se ejecutan contra localhost:18086 sobre HTTP con la excepción explícita de Development. Esto no demuestra una publicación con HTTPS.

Las pruebas WebApplicationFactory utilizan una base EF InMemory aislada. Las pruebas UnitTests utilizan Moq y referencian Core.Application; no utilizan EF ni una base física. Playwright comprueba integración contra PostgreSQL real. La distinción importa: una prueba InMemory no demuestra el comportamiento de concurrencia de PostgreSQL.

Se revisaron visualmente el listado y el editor en escritorio, el formulario móvil y la ficha con Recharts en tema oscuro. Las comprobaciones de navegador verifican ausencia de desbordamiento horizontal en los recorridos principales. Sanidad y el mapa de potreros se revisaron también en escritorio y en móvil oscuro. El detalle de ocupación se revisó con filtros por lote. La revisión no equivale a una auditoría completa de accesibilidad ni de todos los navegadores.

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
