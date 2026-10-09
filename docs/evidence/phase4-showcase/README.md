# Semilla amplia: evidencia del 8 de octubre de 2026

La carga optativa añadió tres fincas, 180 animales y siete cuentas de rol al entorno local conservado. El manifiesto detalla 1.347 pesajes, 2.163 rendimientos (2.151 ordeños y 12 esquilas), 369 eventos clínicos, 339 reproductivos, 360 movimientos de animales y 36 existencias por finca/producto.

## Validaciones

- Compilación de API: sin errores ni advertencias de compilación.
- 241 UnitTests y 196 Core.Tests aprobadas. Las seis pruebas nuevas cubren la carga, las reglas de negocio y las siete cuentas dentro de sus escenarios.
- PostgreSQL real: conflicto de correo al crear la segunda cuenta provoca un error intermedio. Tras la reversión quedan cero fincas MEGA, cero usuarios demo, cero animales MEGA y cero manifiestos. Se conservó el usuario que causaba el conflicto; después se corrigió su correo mediante la API administrativa para permitir la carga válida.
- Segunda ejecución PostgreSQL: `Replayed=true`, con 11.531 filas comparadas en 42 tablas idénticas antes/después.
- Conservación en el entorno principal: las 6.451 filas originales de las mismas 42 tablas mantienen sus huellas. Los historiales de sesión y la sincronización independiente de ExchangeRates se excluyen de esa comparación.
- Respaldo PostgreSQL en formato custom de 891.605 bytes, con índice legible por `pg_restore --list`, guardado fuera del repositorio.
- Login real y permisos de las siete cuentas verificados antes y después del reinicio de la API. Los administradores ven 191 animales (11 anteriores y 180 nuevos); los demás solo sus fincas asignadas.
- Analítica REST verificada por finca para los últimos 30 días: leche en 30 fechas, preñez/fertilidad con servicios pendientes, partos, existencias críticas y rotación reconstruible en las 12 existencias de cada finca.

Las credenciales privadas, el respaldo y las huellas por fila no se versionan. Los JSON públicos contienen resultados, cantidades y usuarios de demostración sin contraseñas.

## Archivos

`validation-summary.json` reúne el manifiesto y los resultados. `mega-isolated-verification.json` y `mega-demo-verification.json` muestran los accesos y KPI consultados en los dos entornos. Los archivos `mega-*-proof.json` describen reversión, repetición y conservación.

Los logs comprimidos conservan también la primera compilación fallida de la prueba por el nombre incorrecto de una propiedad (`LowStockCount`, corregido a `Critical`); la compilación y las seis pruebas finales pasan. Ese fallo de la prueba no se presenta como un éxito inicial. `mega-empty-rollback.log.gz` contiene el error intencional por correo duplicado.

El runtime Docker emite avisos previos de libgssapi/DataProtection durante el CLI. Se conserva la salida original y se comprueba el código de salida y el estado de PostgreSQL; los avisos no se presentan como errores de carga cuando el comando termina correctamente.

Estos resultados verifican la demostración y su importación. La prueba InMemory no certifica restricciones PostgreSQL: la ejecución y comparación adicionales cubren esa diferencia. No se midió nuevamente la cobertura frontend ni Core Web Vitals, porque este cambio no modifica componentes SPA.
