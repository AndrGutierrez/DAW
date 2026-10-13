# Corrección del presupuesto de imagen — 13 de octubre de 2026

La corrida [38338153291](https://github.com/AndrGutierrez/DAW/actions/runs/38338153291) del PR #11 falló en Enforce image budget: la API ocupaba 132954585 bytes frente al límite estricto de 120000000. HTTP contracts y Browser regression quedaron omitidos, y Publish verified artifacts no publicó entregables. Backend y frontend sí aprobaron sus pruebas.

La medición local anterior mediante docker image inspect .Size reportaba 58775704 bytes; docker history muestra capas descomprimidas superiores a 134 MB para esa imagen local. Docker usa métricas distintas según el almacén de imágenes y tiene correcciones relacionadas en sus [notas de versión](https://docs.docker.com/engine/release-notes/29/). La cifra anterior no demostraba que la API cumpliera el requisito. El script ahora suma las capas descomprimidas y cuenta también capas base; las regresiones prueban exceso, igualdad con el límite, valores inválidos y fallo del motor.

La API conserva runtime-deps Alpine y se publica autocontenida con PublishSingleFile y EnableCompressionInSingleFile. No se aplica trimming ni Native AOT. La compresión reduce el contenido almacenado en la imagen y tiene un costo de descompresión al arrancar, como explica [.NET](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview). Se comprueban inicialización, readiness y HTTP sobre una base PostgreSQL desechable; la demostración existente utiliza sus propios contenedores y volúmenes.

Medición local de las imágenes verificadas: API 68464640 bytes; SPA 74764288 bytes. La aprobación remota debe comprobarse en la nueva corrida del PR antes de presentarla como evidencia de entrega.
