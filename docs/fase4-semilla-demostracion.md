# Semilla amplia para demostración y evaluación

El modo `--seed-demo` añade un escenario completo de ganadería al seed mínimo de `--seed`. Es una importación voluntaria por CLI; no se ejecuta durante el arranque normal ni existe un endpoint público para activarla.

## Preparación y ejecución

1. Inicializar la base con el procedimiento habitual (`--seed`).
2. Configurar `SEED_DEMO_PASSWORD` en el archivo privado `.env`, con una contraseña válida para Identity: al menos ocho caracteres, mayúscula, minúscula, número y símbolo.
3. Respaldar la base actual antes de la carga.
4. Construir la imagen de la API y ejecutar el importador:

```bash
docker compose build blazorapp
docker compose run --rm --no-deps initialize --seed-demo
```

Usar los mismos parámetros `-p`, `-f` y `--env-file` que correspondan al entorno. El comando imprime un manifiesto con fecha de referencia y cantidades, sin contraseñas. La contraseña solo se usa al crear las cuentas; una repetición conserva las contraseñas, roles, nombres, fincas y estados que haya cambiado el administrador. No agregar credenciales reales al repositorio.

## Cuentas

Cada cuenta tiene exactamente un rol inicial. Los dos administradores son cuentas normales gestionables; el superusuario previo permanece protegido e independiente.

| Usuario | Rol interno | Etiqueta en la UI | Acceso inicial |
| --- | --- | --- | --- |
| demo.admin | Admin | Administrador | Todas las fincas |
| demo.administrador | Administrador | Administrador (rol alternativo) | Todas las fincas |
| demo.capataz | Capataz | Capataz | Valle Verde y Los Llanos |
| demo.operador | Employee | Operador | Valle Verde |
| demo.operario | Operario | Operario | Los Llanos |
| demo.lectura | SoloLectura | Solo lectura | Las tres fincas nuevas |
| demo.veterinario | Veterinario | Veterinario | Las tres fincas nuevas |

Los permisos provienen del catálogo real de roles. La semilla no concede permisos adicionales ni convierte los usuarios operativos en administradores. Se pueden probar pesaje y traslados con Operador/Capataz, registros sanitarios con Veterinario y bloqueos de escritura con Solo lectura. Operario puede crear registros de campo, pero los traslados requieren actualizar animales. La creación de productos sigue reservada a los administradores.

## Datos y recorridos

- Tres fincas: Finca Valle Verde, Hacienda Los Llanos y Finca La Sierra, con códigos `MEGA-VALLE`, `MEGA-LLANO` y `MEGA-SIERRA`.
- 180 animales nuevos: 168 bovinos y 12 ovinos. Incluye reproductoras, animales de engorde, terneros con madre/padre identificados y estados activo, vendido, muerto, trasladado y perdido.
- 18 potreros con coordenadas del mapa, capacidad, superficie y permanencia máxima. 15 lotes con especie y propósito compatibles. Cada animal tiene una cadena de ingreso/rotación cuyo último destino coincide con su ficha.
- Pesajes de hasta ocho controles por animal durante siete meses, con fechas posteriores al nacimiento. Terneros pequeños tienen menos controles. Los pesos y las metas generan categorías de edad y ejemplos de ganancia por debajo de la meta.
- 60 días de ordeño para 36 bovinas, con variación por finca, animal y día. Se omite la leche durante el retiro de tratamiento. También se incluyen esquilas de ovinos.
- Historias de celo, monta/inseminación, diagnóstico positivo/negativo/incierto, servicios pendientes, partos y destetes. Las fechas de servicio, diagnóstico, parto y nacimiento mantienen su orden.
- Vacunación, desparasitación, enfermedad, tratamiento, cuarentena, mortalidad y cambios de condición sanitaria; autores con acceso a cada finca.
- Cuatro categorías, 12 productos y 36 existencias por finca/producto, con saldos bajos, normales y altos. Cada saldo se reconstruye desde una apertura, entradas, salidas y consumos sanitarios. Hay lotes de insumos, proveedor y lotes de semen como referencias de esos historiales.
- Auditoría de la importación por registro y cuenta, identificada como `Seeded`, con valores y referencias. La fecha de auditoría es la fecha real de carga; no se simulan acciones humanas antiguas ni se guardan contraseñas/hash.

Los KPI se calculan mediante los servicios de analítica existentes; no se almacenan porcentajes ni series ficticias separadas de los registros. Las cantidades, precios y eventos son datos de demostración, no recomendaciones clínicas ni cotizaciones. La tasa BCV se obtiene por el mecanismo existente; la semilla no inventa una tasa.

No se añaden fotografías o adjuntos con enlaces inexistentes. Tampoco se llenan tablas sin un recorrido útil solo para aumentar el número de filas.

## Repetición y conservación

La primera ejecución se guarda en una transacción PostgreSQL serializable, con bloqueo de importación. Un error revierte también las cuentas creadas. El manifiesto `ShowcaseSeed / livestock-showcase-v1` identifica la carga completa. Si existe, el comando devuelve `Replayed=true` sin insertar, editar ni restaurar datos. Las cantidades del manifiesto describen la importación original, no el estado actual después de cambios del usuario.

La fecha de referencia se calcula en America/Caracas al importar y permanece fija: repetir mañana no desplaza historiales ni acumula filas. Si se necesitan datos más recientes en otra evaluación, corresponde crear un nuevo escenario/versionado explícito, no reescribir esta carga.

Los códigos reservados y usuarios `demo.*` no se adoptan si ya existen sin un manifiesto completo: el comando falla antes de modificar los datos. Los registros archivados también cuentan para detectar conflictos. El seed mínimo, los datos anteriores, el superusuario y los usuarios existentes conservan su configuración.

## Verificación

Las pruebas `ShowcaseSeedIntegrationTests.cs` comprueban la población, genealogía, fechas/pesos, exclusión de leche en retiro, cadenas de movimiento, reconciliación de existencias, cálculo de KPI, acceso de los siete roles, repetición tras archivar/desactivar/cambiar contraseña y rechazo de contraseñas/configuración inválidas o conflictos de identificadores.

Las pruebas EF InMemory verifican esas reglas; la ejecución adicional sobre PostgreSQL comprueba las restricciones reales y la transacción. La evidencia de la ejecución se registra en `docs/evidence/phase4-showcase/`.
