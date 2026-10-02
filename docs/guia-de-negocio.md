# Guía de negocio

Esta guía describe **cómo funciona la operación ganadera** y las reglas que el sistema debe cumplir. No entra en detalles técnicos. Es la referencia para diseñar, construir y validar el producto. Todo lo aquí descrito es el modelo de negocio esperado del sistema completo; no implica que ya esté programado.

## 1. Qué es y para quién

Un sistema para llevar el control de una o varias fincas ganaderas. Responde preguntas como: qué animales hay, cómo están de salud, cuándo se reproducen, cuánto producen, qué insumos quedan y qué tareas vienen.

| Usuario | Uso principal |
| --- | --- |
| Dueño / administrador | Ve toda la operación, finanzas y usuarios |
| Veterinario | Salud, vacunación, tratamientos y reproducción |
| Capataz | Manejo diario: lotes, pesajes, movimientos y tareas |
| Operario | Registra datos de campo (pesajes, ordeñe, eventos) |
| Solo lectura | Consulta reportes sin modificar nada |

## 2. Organización

- **Finca**: unidad principal. Cada usuario accede solo a las fincas que le corresponden.
- **Potrero**: subdivisión de la finca donde pastan los animales. Permite rotar el pastoreo.
- **Lote (rebaño)**: agrupación de manejo de animales que se mueven y alimentan juntos.

Un animal pertenece a un lote y a un potrero dentro de una finca. Un lote puede pasar de un potrero a otro.

## 3. Especies y razas

La **especie** es la tabla de grupos que clasifica al ganado:

| Especie | Propósito típico |
| --- | --- |
| Bovino | Carne y leche |
| Ovino | Carne y lana |
| Porcino | Carne |
| Caprino | Carne y leche |
| Equino | Trabajo y cría |
| Avícola | Carne y huevos |
| Cunícola | Carne |
| Apícola | Miel |
| Acuícola | Peces |

Cada especie tiene un **propósito productivo** (carne, leche, lana, huevos, doble propósito) y un **período de gestación** que se usa para calcular fechas de parto. Cada especie tiene sus **razas** (por ejemplo, Brahman, Holstein, Dorper, Duroc).

## 4. El animal

Datos principales de la ficha:

- **Identificación**: arete interno (único), arete oficial/DIIO y RFID opcional.
- **Clasificación**: especie, raza y propósito.
- **Datos básicos**: sexo, fecha de nacimiento (la **edad se calcula**, no se guarda), peso al nacer, color y señas (marcas).
- **Peso actual**: se obtiene del último pesaje registrado.
- **Genealogía**: madre (dam) y padre (sire), lo que permite armar el árbol familiar.
- **Ubicación**: finca, potrero y lote.
- **Fotos y adjuntos**: un animal puede tener **varias fotos**, cada una con su fecha de subida, y documentos asociados (certificados, análisis).
- **Última actualización**: se registra cuándo se actualizó el animal por última vez, para detectar registros sin seguimiento (animales sin cambios en más de X días).

El crecimiento se mide con **pesajes** frecuentes; la diferencia de peso en el tiempo da la **ganancia diaria de peso (GDP)**, indicador de salud y productividad. Además del peso se registra el **estado corporal (ECC)** en escala 1 a 5.

## 5. Ciclo de vida del animal

| Estado | Significado |
| --- | --- |
| Activo | En producción o cría dentro de la finca |
| Vendido | Salido por venta |
| Muerto | Baja por muerte (se registra la causa) |
| Trasladado | Enviado a otra finca |
| Perdido | Sin ubicación conocida |

Un animal ya vendido o muerto no puede volver a venderse ni tratarse.

## 6. Salud

Cada evento de salud queda en el **historial clínico** del animal, no solo el estado actual.

- **Estado de salud**: sano, en observación o en tratamiento.
- **Vacunación**: vacuna aplicada, dosis, lote del biológico y **próxima dosis**, que alimenta las alertas.
- **Tratamiento**: medicamento (tomado del inventario), dosis, vía, duración y **período de retiro** (tiempo que debe esperarse antes de vender carne o usar la leche).
- **Enfermedad**: diagnóstico y control de brotes.
- **Cuarentena**: aislamiento de animales nuevos o enfermos.
- **Mortalidad**: fecha y causa.

## 7. Reproducción

1. **Celo** detectado.
2. **Servicio o inseminación**: monta natural o inseminación artificial (con lote de semen).
3. **Diagnóstico de preñez**: confirma la gestación y calcula la fecha probable de parto.
4. **Parto**: registra la cría, la dificultad y si hubo mortinatos.
5. **Destete**: separa la cría de la madre.

El estado reproductivo de la hembra (vacía, preñada, lactando, seca) se deduce de estos eventos.

## 8. Producción

Un animal produce uno o varios productos a lo largo de su vida: ordeño, esquila, recolección o sacrificio. Cada resultado tiene fecha, cantidad y unidad. Los productos de una misma operación comparten su identificador; por ejemplo, carne y piel de un sacrificio. La versión implementada registra estos resultados en una única tabla y mantiene el animal como origen. El sacrificio es irreversible y bloquea producción posterior. Las mediciones especializadas de calidad de la tabla siguiente pertenecen a la visión completa; actualmente sólo se conservan como metadatos cuando se migran registros anteriores.

| Producción | Detalle |
| --- | --- |
| Leche | Litros por ordeñe, con grasa, proteína y células somáticas |
| Lana | Peso del vellón y finura |
| Huevos | Cantidad y peso |
| Canal | Peso y rendimiento al sacrificio |

## 9. Nutrición

Se definen **raciones** (mezcla de alimentos) por especie o lote y se registra la **alimentación** diaria. Esto permite estimar consumo y costo de alimentación.

## 10. Inventario

Controla **medicamentos, vacunas, alimento e insumos**: existencias, entradas y salidas, proveedores y **vencimientos de lote**. Una vacuna o medicamento usado en un tratamiento descuenta del stock.

## 11. Movimientos y trazabilidad

Todo **traslado** de un animal entre potreros o fincas queda registrado con fecha y motivo. El historial completo del animal (nacimiento, salud, reproducción, movimientos, producción) constituye su **trazabilidad**.

## 12. Tareas y alertas

- **Tareas**: trabajo asignado a un usuario con fecha y prioridad (vacunar, pesar, mover un lote).
- **Alertas**: avisos automáticos por **vacuna próxima**, **preñez a controlar**, **stock bajo**, **baja ganancia de peso** y **mortandad elevada**.

## 13. Finanzas

Registra **compras, ventas y gastos** vinculados a animales, lotes o insumos. Permite estimar el **costo por animal, por litro o por kilo** producido.

## 14. Usuarios, roles y permisos

Modelo de permisos inspirado en **Laravel Permission** (spatie), implementado aquí con ASP.NET Core Identity y tablas propias de asignación de permisos:

- Un **rol** agrupa usuarios y permisos; un usuario puede tener varios roles.
- Un **permiso** se nombra como `recurso.accion`: `list`, `get`, `create`, `update` y `delete`; por ejemplo, `animals.create`. `roles.manage` protege la administración de permisos. Cada permiso tiene un **guard** (`web` por defecto).
- Un permiso se otorga a un **rol** (todos sus miembros lo heredan) o **directamente a un usuario**.
- El **superusuario** supera las comprobaciones de permisos y fincas. Las restricciones de integridad y negocio, como impedir un segundo sacrificio, se mantienen.

Roles sembrados: Admin, Employee, Administrador, Veterinario, Capataz, Operario y SoloLectura. Todos los DELETE requieren Admin/Administrador; Employee registra operaciones autorizadas pero no elimina ni mantiene categorías; SoloLectura consulta datos sin modificar fotos o permisos.

## 15. Multi-finca

El sistema admite varias fincas. Cada usuario ve y modifica únicamente los datos de las fincas asignadas; los datos de una finca nunca se mezclan con los de otra.

## 16. Reglas de negocio clave

- El arete interno de un animal es **único** dentro de la finca.
- La **edad** y el **peso actual** se calculan; no se editan a mano.
- No se registra un animal con **fecha de nacimiento futura**.
- Un animal **vendido, muerto o perdido** no participa en eventos nuevos.
- No se vende un animal cuyo **período de retiro** de un tratamiento no haya terminado.
- No se aplica una **vacuna o medicamento sin stock** suficiente.
- Un **diagnóstico de preñez** debe referirse a una hembra con servicio registrado.
- El **parto** se asocia a la madre y a la preñez correspondiente.
- Un usuario solo accede a **sus fincas** y a las acciones permitidas por sus roles y permisos.
- Los **catálogos** (especies, razas) son comunes a todas las fincas.
