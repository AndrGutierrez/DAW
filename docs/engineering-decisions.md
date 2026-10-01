# Registro de decisiones técnicas

Este registro explica qué se decidió, por qué, para qué sirve, cómo se comprobó y qué limitaciones permanecen. Debe actualizarse con cada cambio relevante de la implementación para mantener la documentación alineada con el código. Las funcionalidades planificadas deben distinguirse de las que ya están disponibles.

## Identidad del ganado, pertenencia a un hato y estado de salud

**Decisión:** Modelar `Herd` y `Animal` como entidades del dominio con un GUID y una fecha de creación UTC. Identificar a los animales por su arete, asociar cada animal con un hato y representar explícitamente su estado de salud actual.

**Por qué:** Una explotación ganadera necesita distinguir a cada animal, conocer su pertenencia a un hato y consultar su condición actual. Mantener estos conceptos en el dominio independiza el modelo de HTTP y de la tecnología de almacenamiento. Normalizar los aretes y rechazar duplicados sin distinguir mayúsculas y minúsculas evita registrar varias veces el mismo identificador.

**Para qué:** Ofrecer un flujo de registro ganadero: crear un hato, registrar un animal, consultar su información y actualizar su estado de salud.

**Evidencias:** [`Core.Domain/Cattle`](../src/Core.Domain/Cattle), [`CattleCatalogService`](../src/Core.Application/Cattle/CattleCatalogService.cs), [controlador de animales](../src/Presentation.API/Controllers/AnimalsController.cs), [colección de Postman](../postman/Cattle-Management.postman_collection.json) y [`DomainAndApplicationTests`](../tests/Core.Tests/DomainAndApplicationTests.cs).

**Limitaciones:** El modelo conserva el estado actual de cada animal; no mantiene un historial de tratamientos o cambios de salud. Los registros se almacenan en memoria y no son persistentes.

## Inyección de dependencias y registros compartidos en memoria

**Decisión:** Registrar el servicio de catálogo y el repositorio como scoped, el validador de registros como transient y el normalizador de aretes sin estado mutable como singleton. Mantener un almacén singleton en memoria con acceso sincronizado para compartir los registros entre peticiones.

**Por qué:** Un singleton no debe retener un servicio de negocio scoped. Mientras no exista almacenamiento persistente, las peticiones HTTP independientes necesitan consultar los registros creados en peticiones anteriores.

**Para qué:** Mantener ciclos de vida adecuados por petición, permitir un acceso consistente a los registros ganaderos y disponer de un punto donde sustituir el almacenamiento mediante un futuro adaptador de persistencia.

**Evidencias:** [`Program.cs`](../src/Presentation.API/Program.cs), [`InMemoryCattleStore`](../src/Infrastructure/Cattle/InMemoryCattleStore.cs) y [`DependencyInjectionUsesExpectedLifetimes`](../tests/Core.Tests/ApiIntegrationTests.cs).

**Limitaciones:** El almacén es mutable aunque sea singleton. Su bloqueo protege las operaciones sobre los diccionarios, pero reiniciar el proceso elimina los datos. No ofrece transacciones de base de datos ni almacenamiento duradero. Todavía no existe un `DbContext`.

## Un contrato de errores HTTP predecible

**Decisión:** Mantener `ExceptionMiddleware` registrado antes de los controladores y verificar su integración mediante pruebas que envían peticiones HTTP a la aplicación real.

**Por qué:** Quienes consumen la API necesitan respuestas de error consistentes, independientemente del endpoint que falle. Invocar directamente la clase del middleware comprueba su lógica, pero no demuestra que la cadena de procesamiento HTTP la utilice. Las pruebas de integración HTTP verifican los registros reales de `Program.cs` y detectan posibles problemas de integración.

**Para qué:** Comprobar de forma reproducible el mapeo a 404/400/500, los campos de RFC 7807, el tipo de contenido `application/problem+json` y las respuestas 500 que ocultan detalles internos.

**Evidencias:** [`Program.cs`](../src/Presentation.API/Program.cs), [`ExceptionMiddleware`](../src/Presentation.API/Middleware/ExceptionMiddleware.cs), [`ApiIntegrationTests`](../tests/Core.Tests/ApiIntegrationTests.cs) y la [colección de Postman](../postman/Cattle-Management.postman_collection.json).

**Limitaciones:** El middleware maneja las excepciones generadas por componentes posteriores de la cadena de procesamiento. No redefine todas las respuestas del enrutamiento o de la validación automática de modelos. El endpoint de errores deliberados solo está habilitado en Development. Los errores inesperados conservan su detalle de diagnóstico en los registros del servidor y muestran un mensaje genérico al cliente.

## Mantenimiento de la documentación

Por cada cambio relevante, añadir o revisar una entrada que responda: **¿Qué cambió? ¿Por qué? ¿Qué necesidad operativa o técnica atiende? ¿Dónde está el código? ¿Cómo se verificó? ¿Qué limitaciones permanecen?** Después, actualizar la [guía del producto y su implementación técnica](product-and-technical-guide.md) y el [README](../README.md) cuando sus descripciones dejen de corresponder con el sistema. Las capacidades planificadas deben explicarse por separado del comportamiento disponible.

## Explicar el producto y sus decisiones de arquitectura

**Decisión:** Mantener una guía detallada del producto y su implementación técnica, un resumen e instrucciones de ejecución en el README y este registro de decisiones.

**Por qué:** Quien lee el repositorio necesita comprender el flujo ganadero, las razones de la arquitectura y cómo reproducir el comportamiento del sistema. La documentación conecta las necesidades del producto con las decisiones de implementación y las comprobaciones ejecutables. Así, quienes mantienen el sistema pueden modificarlo sin perder ese contexto.

**Para qué:** Ayudar a quienes consumen la API a ejecutar el flujo ganadero y permitir que quienes mantienen el sistema relacionen los objetivos funcionales y de calidad con el código y las pruebas.

**Evidencias:** [Guía del producto y su implementación técnica](product-and-technical-guide.md), [README](../README.md) y tabla de objetivos, implementación y verificación de la guía. Los enlaces y el formato de la documentación se comprueban antes de publicar el cambio.

**Limitaciones:** La documentación describe la implementación actual y las comprobaciones registradas. No convierte los registros en memoria en datos persistentes ni incorpora funcionalidades pendientes. Debe actualizarse cuando cambie el comportamiento o la arquitectura.
