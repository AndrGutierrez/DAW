# Auditoría de entrega de Fase 4

> Actualización vigente: [conservación de registros y consulta automática BCV](fase4-conservacion-bcv.md). Sustituye los apartados anteriores de borrado físico y cotización exclusivamente manual. Los conteos de pruebas aquí consignados conservan la fecha y el alcance de esa revisión.


> Actualización posterior: consultar la [auditoría integral](fase4-auditoria-integral.md) y la [evidencia de UI/testing/rendimiento](evidence/phase4-ui-quality/README.md). Incorporan Classroom y el quiz, 107 pruebas frontend, distribución de pesos y los conteos/coberturas actuales. Los resultados de este documento se conservan como evidencia de la revisión anterior.


Fecha de revisión: 8 de octubre de 2026. Este documento contrasta la implementación con las fuentes; no asigna una calificación ni declara cerrada la entrega.

## Fuentes y prioridad

Se revisaron los documentos locales de Desarrollo de Aplicaciones Web: Fase 4/03_Tarea_Asignacion_Fase4_y_Rubrica.docx, el material teórico, la guía de laboratorio, DAW-0423807T Fase 4.pdf y Sistema de Evaluación y Rubricas.docx. El documento anterior de asignación define cinco criterios y 80 puntos. El nuevo instrumento Instrucciones y Evaluación 09 Octubre .docx declara 40 puntos en cuatro criterios y tiene prioridad para la presentación del 9 de octubre; ver [matriz vigente](fase4-evaluacion-09-octubre.md). El quiz corresponde a los 20 puntos restantes; un repositorio y sus pruebas no sustituyen ese examen.

También se volvió a consultar el [sitio del profesor](https://gramirezsunet.github.io/desarrolloAplicacionesWeb/REAF-F4/), sus criterios generales, los diez puntos transversales y la ficha del Grupo 3. Las instrucciones de otros grupos no se trasladan al dominio ganadero. El [contraste con su repositorio de ejemplo](fase4-referencia-profesor.md) registra la revisión concreta: inicio por rol, KPI con acciones y documentación reproducible. La portada administrativa ahora prioriza datos ganaderos; la galería y la guía describen la SPA actual.

La teoría menciona JWT en localStorage; el checklist web permite memoria o cookies seguras. Se conserva la solución de JWT en memoria y refresh persistente HttpOnly, autorizada para este proyecto. La paleta ganadera también es una decisión explícita del proyecto, aunque difiere del Azul UNET literal de la rúbrica. Estas diferencias deben explicarse en la defensa; no se presume aprobación del docente.

## Rúbrica práctica

| Criterio | Evidencia en el proyecto | Límite o cierre pendiente |
| --- | --- | --- |
| SPA React/Vite/Tailwind, 20 puntos | src/frontend desacoplado; rutas y componentes React/TypeScript, carga diferida y peticiones JSON. Tailwind compila y compone los controles de la curva diaria con grid-cols-1/md:grid-cols-2 y overflow-x-auto; el sistema visual también usa CSS y tokens compartidos | El estilo no se construye exclusivamente con utilidades. La prueba de pesaje sin navegación pertenece a la regresión completa anterior |
| Context API/sesión/tema, 20 puntos | AuthContext y ThemeContext; renovación de sesión, permisos, temas y preferencia local persistida. JWT Bearer enviado por un único cliente | Los datos del usuario se obtienen de la sesión validada en la API; no se decodifica el JWT para confiar en sus permisos en React. La persistencia se apoya en refresh cookie; la paleta sustituye Azul UNET |
| Dashboard KPI, 15 puntos | Costo y valor de referencia, mínimos/máximos, rotación con historial trazable, gráficos/tablas, leche diaria por lote, peso por edad y diagnósticos | USD es una convención de referencia, no moneda histórica persistida. Conversión USD/Bs con referencia fechada de ingreso manual desde BCV. SSE entre sesiones y respaldo de 60 segundos; broker de una instancia API |
| xUnit/Moq, 15 puntos | 235 pruebas aisladas en tests/UnitTests con referencia a Core.Application. ProductCatalogServiceTests ejerce CrudService<Product, ProductRequest>, ProductDefinition y ProductRequestValidator contra IManagementRepository simulado | Son los equivalentes del ProductService/IProductRepository del ejemplo, sin clases artificiales creadas solo para imitar nombres. Reporte Passed y cobertura real adjuntos; no se afirma 100 % |
| Integración/RBAC/errores, 10 puntos | API .NET 10, PostgreSQL 15 y Nginx; guards y permisos tanto en UI como API; RFC 7807, validaciones, conservación de valores y reintentos idempotentes | Core.Tests usa pipeline HTTP y EF InMemory; las pruebas Playwright dirigidas consumen la API y PostgreSQL físicos. Son evidencias distintas |

## Checklist transversal

| Punto del sitio | Estado verificado o limitación |
| --- | --- |
| JWT seguro | Memoria + refresh HttpOnly, CSRF, renovación y cierre. HTTP local tiene la excepción de desarrollo; HTTPS de producción queda por demostrar |
| RFC 7807 | ApiError conserva title/detail/errors; formularios y feedback traducen los conflictos conocidos. Se corrigió el mensaje de SKU/categoría duplicados, que podía mencionar un animal |
| Guards RBAC | Dashboard y mantenimiento de productos restringidos a Admin con permisos; la API vuelve a autorizarlos |
| Validación cliente | Reglas React de animal/pesaje y restricciones HTML en editores. Categoría limitada a 100 caracteres. Los conflictos entre registros y las referencias se vuelven a validar en el servidor; no se replica toda la base en el navegador |
| Búsqueda/paginación | Animales, candidatos, productos, existencias e historiales se paginan en el servidor. Catálogos pequeños y tablas analíticas agregadas tienen un tratamiento distinto |
| Responsividad | Ubicación comprobada a 390/1365 px y curva diaria en proyectos desktop/mobile; sin desbordamiento horizontal. No es una certificación de todos los navegadores |
| Cargas | Skeletons/estados y carga diferida de gráficos, búsqueda y exportadores |
| Moneda/decimales | USD/Bs con dos decimales; referencia fechada BCV registrada manualmente con seis decimales, fuente y fecha visibles. Sin tasa configurada Bs queda deshabilitado; no se afirma verificación automática |
| Docker | Compose levanta PostgreSQL/API/Nginx. initialize aplica --seed antes de iniciar API; docker compose up --build -d --wait se comprobó con un volumen vacío aislado |
| Defensa E2E | Evidencia seleccionada y recorrido propuesto abajo. Falta ensayo presencial y, si se exige video, su grabación |

## Grupo 3: capacidades y brechas

- Ficha 360: historial clínico, fotografías, genealogía navegable, reproducción, producción y peso/GDP. La compresión cliente y los reportes XLSX/PDF ya se verificaron en la regresión anterior.
- GDP: fórmula y selección por fecha en Application. Se agregó aviso automático de pérdida real de peso entre los dos últimos puntos diarios; no depende de un objetivo introducido. El objetivo se persiste por finca y animal, y Seguimiento/Dashboard recalculan avisos generales de los últimos intervalos; sin dos fechas se informa la exclusión.
- Retiro sanitario: avisos y bloqueo en servidor de leche/sacrificio durante el retiro; no es una simple decoración del dashboard.
- Potreros: tarjetas interactivas, capacidad, densidad, residentes y días desde entrada conocida. El panel lateral resuelve detalle y traslado. El plano configura posiciones/tamaños por finca, con relleno por capacidad y borde según permanencia máxima. No representa GPS ni prescribe una política agronómica.
- Pesaje en manga: cola consecutiva, foco y confirmación sin recargas; reintentos conservan el identificador.
- Analítica lechera: ahora entrega y dibuja una serie diaria por finca/lote actual. Las identidades evitan mezclar grupos con nombres iguales. Solo suma litros, informa exclusiones y muestra huecos en días sin registro. No reconstruye una asignación histórica del ordeño.
- Peso/reproducción: medios de peso y conteos por rango de edad al pesaje; preñez en hembras evaluadas y fertilidad de servicios evaluados, con numeradores, denominadores y pendientes visibles. Son cohortes bovinas observadas, no indicadores inferidos de todo el rebaño; ver [métodos](fase4-cierre-evaluacion.md).

## Trabajo restante prioritario

1. Revisar el [PR #8](https://github.com/AndrGutierrez/DAW/pull/8), publicado como borrador en el repositorio oficial; comprobar instalación/HTTPS desde otro equipo si corresponde a la entrega. main conserva su estado anterior hasta integrar la revisión.
2. Ensayar el [recorrido vigente de cuatro criterios](fase4-evaluacion-09-octubre.md), preparar la sustentación y completar el formulario presencial. Las tareas externas no se presentan como realizadas.
3. Como mejora posterior, reforzar con Moq las rutas que hoy dependen de pruebas de integración y medir rendimiento de grandes rebaños/exportaciones.

Se completaron la política persistente de GDP, los avisos por finca, el plano/umbral de permanencia, la gestión de usuarios, el arranque vacío y la actualización de Postman. Los [reportes actuales](evidence/phase4-evaluation/README.md) distinguen ejecución, cobertura y PostgreSQL.

La corrección de eventos desde la UI, el traslado masivo de lotes, los lotes farmacológicos/caducidad y alertas en segundo plano son ampliaciones del negocio. No aparecen como requisitos literales adicionales de la rúbrica práctica de esta fase. Deben planificarse sin confundirlos con sus cinco criterios.

## Recorrido de demostración

1. Iniciar sesión, mostrar/ocultar contraseña, alternar tema y recargar para comprobar recuperación de sesión.
2. Consultar una ficha: pestañas, datos, genealogía, foto preparada, curva y alerta de pérdida con fechas.
3. Pesar dos animales consecutivamente, provocar un valor inválido y confirmar sin recargar.
4. Explorar un potrero, residentes/permanencia y traslado con motivo; demostrar el límite de capacidad.
5. Registrar una entrada/salida de insumo y revisar su saldo/historial; enseñar el estado de retiro y un bloqueo sanitario.
6. Como Admin: dashboard, selector de lote para leche diaria y tabla equivalente; exportar el período filtrado.
7. Como Employee: comprobar que dashboard/mantenimiento restringido no aparecen y que la API rechaza el acceso.
8. Mostrar el reporte xUnit Passed, el mock estricto y sus verificaciones de no escritura ante conflictos.

Los casos de demostración que creen datos deben identificarlos y limpiarlos o conservarlos expresamente como datos de demo. No reutilizar fixtures eliminados como si fueran evidencia de datos actuales.

## Evidencia

[Reportes y capturas de esta revisión](evidence/phase4-audit/README.md) y [historial de verificaciones](fase4-verificacion.md). Los conteos anteriores no se presentan como pruebas reejecutadas en este incremento. La cobertura mide líneas/ramas instrumentadas; Passed mide éxito de los casos ejecutados.
