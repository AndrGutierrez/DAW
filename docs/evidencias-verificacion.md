# Evidencias de verificación del núcleo ganadero

## 1. Entorno y alcance

Validación realizada el **1 de octubre de 2026, hora de Venezuela**; los registros del servidor utilizan UTC y corresponden al 2 de octubre. Entorno: SDK **.NET 10.0.401**, herramienta `dotnet-ef` **10.0.4**, imagen de ejecución ASP.NET Core 10 y **PostgreSQL 15.19**.

Las comprobaciones de base de datos utilizaron contenedores separados y bases desechables. No se ejecutaron migraciones ni borrados sobre la base personal existente. La API de verificación utiliza `http://localhost:18081`; una API anterior que siga escuchando en `18080` no representa necesariamente estos cambios. Las bases de prueba usan almacenamiento temporal: se comprobó reinicio de la API conservando su base, no supervivencia de esa base temporal después de eliminar el contenedor PostgreSQL. En la instalación normal, Compose utiliza un volumen persistente.

Los resultados describen las comprobaciones efectivamente ejecutadas. La [matriz de Fase 2 y Fase 3](fases-2-y-3.md) relaciona los requisitos con su implementación; la [guía del modelo](modelo-produccion-y-crud.md) explica las decisiones del negocio y sus límites.

## 2. Resultados

| Comprobación | Entorno | Resultado obtenido |
| --- | --- | --- |
| Compilación/publicación Docker | SDK y runtime .NET 10 | Exit 0; imagen construida y API iniciada |
| Pruebas .NET | xUnit; EF InMemory en pruebas de servicios/pipeline | **93 aprobadas, 0 fallidas, 0 omitidas** |
| Correspondencia modelo–migración | EF Core/Npgsql | `has-pending-model-changes`: sin cambios pendientes |
| Migración desde base vacía | PostgreSQL 15.19 | Cuatro migraciones aplicadas; 43 tablas, incluida la historia de migraciones |
| Categorías declaradas con `HasData` | PostgreSQL 15.19 | Base nueva: UUID constantes; actualización de base sembrada: UUID anteriores y referencias conservados |
| Actualización con producción histórica | PostgreSQL 15.19 | Leche, lana y carne conservan sus UUID y cantidades; se conserva metadata anterior en `Notes` |
| Datos históricos sin atribución individual | PostgreSQL 15.19 | Migración rechazada; tablas anteriores, huevos, rendimientos, producto e historial de migraciones permanecen intactos |
| Restricciones reales | PostgreSQL 15.19 | SKU/email únicos, categoría restringida, límites de inventario, defaults y dinero `numeric(18,2)` verificados |
| Siembra repetida | PostgreSQL 15.19 | Dos ejecuciones; permanecen 2 categorías, 4 insumos, 4 inventarios, 8 animales, 4 rendimientos y 2 usuarios de prueba |
| Exportación SQL | PostgreSQL 15.19 | Esquema y datos de negocio exportados; ambos importados correctamente en otra base vacía |
| Colección Postman/Newman | API real con PostgreSQL 15.19 | **97 solicitudes, 176 assertions, 0 fallos** |
| Colección compatible con CI de Andrés | API real con PostgreSQL 15.19 | **16 solicitudes, 16 assertions, 0 fallos**; subida, lectura autenticada y eliminación de fotografías incluidas |
| Escrituras concurrentes | API real con PostgreSQL 15.19 | Tres escenarios; cada par obtiene una respuesta 201 y una 409, sin cambios parciales |
| Valores explícitos iguales a cero | API real con PostgreSQL 15.19 | `MinStock = 0` y unidad `Kilogram` conservados al guardar y consultar |
| Persistencia tras reinicio | API real con la misma base PostgreSQL | Alta 201, edición 200, reinicio, consulta 200 conservando el valor editado; eliminación de prueba 204 |
| Fotografías y aislamiento | Pruebas .NET de API/servicios y almacenamiento | Permisos de escritura, contenido privado, 401 sin token, 404 fuera de finca y rechazo de rutas/archivos inválidos |

Las pruebas InMemory no demuestran integridad relacional ni aislamiento transaccional de PostgreSQL. Por eso las comprobaciones SQL y de concurrencia se realizaron además sobre el servidor real.

## 3. Migraciones y conversión histórica

La consulta de `__EFMigrationsHistory` devolvió:

```text
20261001043752_InitialCreate
20261002004015_AddAnimalPhotosAndUpdatedAt
20261002021355_UnifiedAnimalProductionAndInventory
20261002030136_ManagedInventoryCategorySeed
```

La prueba de actualización parte del esquema de las dos primeras migraciones y carga [LegacyProduction.sql](../tests/Core.Tests/Fixtures/LegacyProduction.sql). Tras aplicar la tercera migración, [VerifyLegacyUpgrade.sql](../tests/Core.Tests/Fixtures/VerifyLegacyUpgrade.sql) comprueba:

- Leche: 12,50 litros, UUID original y porcentaje de grasa 3,20 conservado en metadata.
- Lana: 3,25 kg y UUID original.
- Carne: tercer rendimiento conservado; el animal sacrificado queda `Dead`.
- Insumo histórico: conserva UUID, recibe SKU `LEGACY-<uuid>` y categoría procedente del enum anterior. Queda inactivo con precio pendiente; la migración no inventa su valor comercial.

En otra base, [LegacyLotProduction.sql](../tests/Core.Tests/Fixtures/LegacyLotProduction.sql) incorpora doce huevos atribuidos solamente a un lote. La migración devuelve el error:

```text
Lot-level egg records require an explicit animal allocation before this migration.
No animal will be invented.
```

[VerifyRejectedUpgrade.sql](../tests/Core.Tests/Fixtures/VerifyRejectedUpgrade.sql) confirma que no se creó `AnimalProduction`, permanecen las dos migraciones anteriores y no se alteraron los datos históricos. Este bloqueo requiere resolver la atribución antes de migrar una base que contenga esos registros.

Las tablas separadas de leche, lana, carne y huevos ya no existen en el esquema actualizado. La migración `Down` está bloqueada porque no puede reconstruir fielmente la separación anterior. Recuperar ese estado requiere restaurar una copia verificada; no se presenta como una reversión automática sin pérdida.

La cuarta migración aplica las categorías de `HasData`. Se comprobó en una base vacía y en otra que ya contenía las categorías creadas por el seeder anterior. En esta última conservó los dos identificadores anteriores; los productos mantuvieron sus referencias. La inserción por nombre no sustituye ni normaliza el UUID de una categoría existente. Los UUID constantes del modelo se usan al crear categorías ausentes en una base nueva.

## 4. Evidencia SQL disponible

- [schema.sql](../db/schema.sql): exportación real del esquema PostgreSQL 15, incluidos tipos, índices y claves foráneas.
- [seed-evidence.sql](../db/seed-evidence.sql): `INSERT` de datos de negocio obtenidos después de ejecutar dos veces el seeder. Excluye identidades, hashes de contraseña, permisos de usuarios y tokens. Sirve como evidencia de datos; no sustituye un respaldo completo ni el comando `--seed`.
- [verification.sql](../db/verification.sql): consultas de sólo lectura para comprobar versión, migraciones, tablas, índices, precisión, relaciones y siembra en la instalación propia.
- [PostgresConstraints.sql](../tests/Core.Tests/Fixtures/PostgresConstraints.sql): pruebas de restricciones dentro de una transacción que termina en `ROLLBACK`. Ejecutar únicamente en una base de prueba.

Se importaron `schema.sql` y `seed-evidence.sql` en una base adicional vacía con `psql -v ON_ERROR_STOP=1`, sin errores. Esa importación demuestra que los archivos actuales son utilizables como evidencia; no recrea las cuentas de acceso porque deliberadamente no se exportaron identidades.

## 5. Casos HTTP comprobados

La colección ejecutó login Admin/Employee, consultas sembradas, operaciones de los once recursos CRUD, ruta restaurada de salud, estados de sacrificio, aislamiento por finca y renovación de sesión. Incluyó, entre otros, estos resultados:

| Solicitud | Resultado |
| --- | --- |
| Login Admin y Employee | 200 y JWT válido |
| Catálogo protegido sin JWT | 401 con Problem Details |
| DELETE de un producto como Employee | 403; el recurso sigue existiendo |
| POST de un producto como Employee | 403 con Problem Details; catálogo reservado a administración |
| Crear producto con precio negativo | 400 y detalle de `Price`; no se crea |
| Repetir SKU o eliminar categoría referenciada | 409 |
| `POST /api/animals` | 201 y animal persistido |
| `PATCH /api/animals/{id}/health-status` | 200 e historial del cambio |
| Carne y piel con un mismo `OperationId` | Dos rendimientos del mismo sacrificio; animal `Dead` |
| Producción después del sacrificio o borrado de su registro | 409 |
| JSON inválido | 400 con Problem Details |
| Error inesperado de demostración | 500 con mensaje genérico, sin stack trace en la respuesta |
| Reutilizar refresh token ya rotado | 401 |

Cada error comprobado utiliza `application/problem+json` y los campos `type`, `title`, `status`, `detail`, `instance`. Los errores de validación añaden `errors` por campo. El endpoint de error inesperado existe para verificar el middleware en Development.

Las pruebas .NET incluyen `RegistrationPersistsDifferentHashesForTheSamePasswordAndVerifiesCredentials`: dos altas HTTP con una misma contraseña de prueba conservan hashes diferentes; Identity acepta la contraseña correcta y rechaza una incorrecta. Utiliza EF InMemory y demuestra el comportamiento del servicio de Identity, no una comparación manual de hashes. `SeededAdminCanLoginAndReadRoles` comprueba `email` en el JWT real del login. `AdministratorCannotCreateAProductWithAnInvalidSku` y `ProductDtoDoesNotBindIdentityOrCreationTimestamp` comprueban formato de SKU y campos de entrada protegidos.

### Reproducir Newman

Desde Git Bash, configurar las contraseñas en variables locales sin incluirlas en archivos versionados:

```bash
read -rsp 'Contraseña Admin: ' DAW_ADMIN_PASSWORD
printf '\n'
read -rsp 'Contraseña Employee: ' DAW_EMPLOYEE_PASSWORD
printf '\n'
npx --yes newman run postman/Cattle-Management.full.postman_collection.json \
  -e postman/Daw.postman_environment.json \
  --env-var baseUrl=http://localhost:18080 \
  --env-var "adminPassword=$DAW_ADMIN_PASSWORD" \
  --env-var "employeePassword=$DAW_EMPLOYEE_PASSWORD"
unset DAW_ADMIN_PASSWORD DAW_EMPLOYEE_PASSWORD
```

La colección crea datos de prueba. Los informes JSON completos de Newman pueden incluir credenciales y JWT; no deben publicarse sin depurarlos. Los conteos anteriores se obtuvieron del reporte real y aquí se conserva el resultado sin esos valores.

## 6. Concurrencia y persistencia

[verify-concurrency.py](../scripts/verify-concurrency.py) utiliza sólo la biblioteca estándar de Python y crea registros en la API configurada. Debe ejecutarse contra una base desechable. Se comprobaron:

1. Dos sacrificios simultáneos del mismo animal: uno se registra; el segundo devuelve 409. Hay un único rendimiento de sacrificio y el animal queda muerto.
2. El mismo `OperationId` usado simultáneamente para dos animales: sólo una operación se registra; el animal de la petición rechazada permanece activo.
3. Dos altas simultáneas del mismo producto de una operación: sólo queda una fila.

La transacción abarca las consultas que deciden la regla y la escritura. El aislamiento `Serializable` detecta conflictos; las causas PostgreSQL `40001`/`40P01`, incluso envueltas por EF/Npgsql, se traducen a 409. La petición rechazada no deja filas ni cambios parciales de estado. El cliente debe actualizar sus datos y decidir si reintenta; el servidor no repite automáticamente una operación irreversible.

[verify-persistence.py](../scripts/verify-persistence.py) crea una finca, modifica su nombre y guarda únicamente su UUID/valor esperado en un checkpoint. Tras reiniciar externamente la API, consulta ese UUID, verifica el valor editado y elimina el registro temporal. No almacena credenciales ni tokens en el checkpoint.

```bash
# Sólo en un entorno de prueba, con sus credenciales locales.
export DAW_VERIFY_BASE_URL=http://localhost:18080
read -rsp 'Contraseña Admin de prueba: ' DAW_VERIFY_ADMIN_PASSWORD
printf '\n'
export DAW_VERIFY_ADMIN_PASSWORD
python scripts/verify-concurrency.py
python scripts/verify-persistence.py prepare checkpoint.json
docker compose restart blazorapp
# Esperar a que la API vuelva a responder antes de verificar.
python scripts/verify-persistence.py verify checkpoint.json
unset DAW_VERIFY_ADMIN_PASSWORD
```

La preparación obtuvo 201/200. Después de reiniciar el contenedor de la API, la lectura obtuvo 200 conservando el nombre modificado y la limpieza obtuvo 204. Se mantuvo la misma base durante el reinicio.

## 7. Qué queda fuera de estas evidencias

Estos resultados cubren el núcleo CRUD, persistencia, producción individual y seguridad de las fases descritas. No acreditan una interfaz completa de gestión ni procesos completos de reproducción, contabilidad, alertas o tratamientos clínicos. Esos módulos tienen entidades previstas, pero requieren sus propios casos de uso y pruebas. La recuperación de fotografías eliminadas por una migración histórica anterior tampoco forma parte de esta intervención.
