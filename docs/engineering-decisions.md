# Decisiones de ingeniería

Cada entrada describe qué cambió, por qué y para qué. Las reglas detalladas están en [modelo y CRUD](modelo-produccion-y-crud.md).

## 1. Una fuente persistente de animales

**Cambio:** retirar el catálogo operativo en memoria y las entidades duplicadas de `Core.Domain.Cattle`. El modelo `Core.Domain.Livestock` y PostgreSQL constituyen la fuente operativa. `/api/herds` se conserva como alias de lotes, con el contrato persistente `farmId`, `speciesId`, `name`, `purpose` y potrero opcional.

**Motivo:** dos modelos de animal y almacenamiento diferente por endpoint permitían respuestas contradictorias y pérdida de registros al reiniciar.

**Resultado:** GET y las escrituras comparten la misma base. Los clientes del prototipo deben actualizar sus cuerpos; no se selecciona una finca o especie por defecto para ocultar datos faltantes.

## 2. Recuperar operaciones y completar CRUD

**Cambio:** restaurar POST de animales, PUT/DELETE y PATCH sanitario. Conservar `/health-status` con `status`; `/health` recibe `healthStatus`. Proporcionar las cinco operaciones CRUD de once recursos.

**Motivo:** una API de gestión debe permitir administrar registros, no limitarse a leerlos. La desaparición de rutas anteriores no representa una decisión de negocio válida por sí sola.

**Resultado:** contratos visibles en Swagger y pruebas de cada operación. Las restricciones sobre dependencia e historia se devuelven como 409. Recuperar la ruta no implica mantener entidades duplicadas ni un contrato anterior que carecía de finca y especie.

## 3. Unificar producción

**Cambio:** `AnimalProduction` reemplaza las tablas específicas de leche, lana, sacrificio y huevos. Una fila representa un producto obtenido; `OperationId` relaciona resultados de la misma operación.

**Motivo:** un mismo animal puede generar distintos productos y un sacrificio puede tener varios resultados. Separar las tablas obliga a consultar y desarrollar cada rendimiento con estructuras diferentes.

**Resultado:** consulta y CRUD únicos; tipos/métodos/unidades coherentes, fecha no futura y cantidad positiva. El sacrificio cambia el estado en la misma transacción y no se revierte borrando una fila. Se conserva información convertible del esquema anterior y se bloquean datos ambiguos.

## 4. Insumos y existencias por finca

**Cambio:** catálogo de categorías e insumos con SKU, marca, unidad y precios; existencias, umbrales y ubicación en `FarmInventory` por finca.

**Motivo:** un mismo insumo puede estar en varias fincas con cantidades diferentes. El alimento comprado y la leche producida tienen operaciones y unidades de control distintas.

**Resultado:** no se comparte un stock global por accidente. Las categorías se relacionan con insumos usando `Restrict`; cantidades no negativas y máximo mayor que mínimo se validan en API y base.

## 5. Separar lectura y escritura en permisos

**Cambio:** catálogo de permisos `list/get/create/update/delete`, `roles.manage` para administración y roles Admin/Employee además de los roles ganaderos existentes. Todos los DELETE exigen rol administrativo.

**Motivo:** exigir `animals.get` para subir fotos o `roles.list` para asignar permisos permitía mutaciones a SoloLectura.

**Resultado:** permisos de lectura no otorgan escritura. Las membresías limitan datos operativos por finca. El alcance administrativo se verifica contra la base, no sólo contra una afirmación del token.

## 6. Fotografías privadas

**Cambio:** descarga por API autenticada y finca; nombres internos aleatorios, extensión acorde al formato, validación de firma y límites de lectura.

**Motivo:** las guardas de subida y eliminación no protegen una carpeta estática pública. Una extensión suministrada por el cliente tampoco confirma el formato real.

**Resultado:** URL utilizable únicamente con la autorización adecuada. Se mantiene almacenamiento local/volumen; la firma de formato no se presenta como una decodificación completa del archivo.

## 7. Validación y errores consistentes

**Cambio:** FluentValidation en Application para altas, cambios y autenticación; filtro HTTP asíncrono; Problem Details para excepciones, autorización y enlace de modelos.

**Motivo:** la API necesita explicar qué campo es inválido y mantener el mismo contrato entre errores 400, 401, 403, 404, 409 y 500.

**Resultado:** `errors` por campo y detalle interno oculto para 500. Las reglas se ejecutan también cuando se usa el servicio sin un controlador.

## 8. Serializable, auditoría y evidencia

**Cambio:** comprobaciones y escritura CRUD bajo Serializable en PostgreSQL; auditoría de la unidad de trabajo; conflictos concurrentes y de integridad convertidos a 409.

**Motivo:** una comprobación previa sin aislamiento puede permitir dos sacrificios concurrentes o una operación compartida por animales diferentes.

**Resultado:** una sola petición confirma la operación incompatible; la otra debe reintentarse con datos actuales. La evidencia de PostgreSQL se distingue de las pruebas InMemory. La auditoría sólo se atribuye a operaciones que pasan por `ManagementRepository`.

## 9. PostgreSQL 15 y actualización controlada

**Cambio:** Compose usa PostgreSQL 15 y un volumen separado del anterior PostgreSQL 17. Migración explícita, seed idempotente y herramientas EF en `.config/dotnet-tools.json`.

**Motivo:** aplicar las mismas condiciones de ejecución sobre las que se valida el esquema y evitar montar datos físicos de otra versión del motor.

**Resultado:** puesta en marcha reproducible, sin eliminar el volumen anterior. La nueva transformación no tiene un retorno automático fiel al esquema antiguo; una reversión de datos exige una copia verificada.
