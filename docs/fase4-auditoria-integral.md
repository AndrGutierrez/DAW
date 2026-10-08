# Auditoría integral de Fase 4

Revisión del 8 de octubre de 2026. Este informe contrasta requisitos, implementación y evidencia; no asigna una calificación ni acredita la entrega en Classroom. Sustituye los conteos anteriores para esta revisión. [Evidencia y reproducción](evidence/phase4-ui-quality/README.md).

## Fuentes y prioridad

Se inventariaron los 19 archivos encontrados en la carpeta de la materia: 17 documentos del profesor y dos guías de estudio del equipo. Se extrajo el texto de todos; se leyeron íntegramente los materiales de Fase 4, las asignaciones de Fases 1–3 y los instrumentos generales, y se contrastaron los antecedentes restantes mediante búsquedas temáticas. Se inspeccionaron visualmente las cuatro páginas del PDF de Fase 4 y las seis del quiz, incluida su última página en blanco. Esto no equivale a una revisión visual página por página de los PDF antiguos. Las guías privadas no son fuente normativa.

La [página REAF-F4](https://gramirezsunet.github.io/desarrolloAplicacionesWeb/REAF-F4/) se revisó incluyendo los datos dinámicos del Grupo 3 y los diez puntos del checklist, no solo el HTML visible inicial. También se contrastó el README actual del [repositorio de ejemplo](https://github.com/gramirezsunet/desarrolloAplicacionesWeb); la adaptación de su estructura y ejemplos está documentada en [referencia del profesor](fase4-referencia-profesor.md). El video aportado se examinó como inspiración visual, sin convertir sus funciones agrícolas en requisitos nuevos.

El [inventario con SHA-256](evidence/phase4-ui-quality/source-inventory.json) identifica las fuentes revisadas sin publicar sus archivos privados. Los requisitos de Classroom proceden del texto compartido por el usuario; no se simuló acceso a su cuenta.

Los instrumentos tienen ponderaciones diferentes y deben presentarse por separado:

| Instrumento | Criterios y peso | Uso en esta revisión |
| --- | --- | --- |
| Classroom, Fase 4 / Semana 4 | Arquitectura React 35%; REST 35%; testing frontend 20%; rendimiento/UI 10% | Incluye expresamente pruebas de componentes y Core Web Vitals; las pruebas C# y los KPI ganaderos no los sustituyen |
| Instrucciones y Evaluación 09 Octubre | Cuatro criterios de 10 puntos, cada uno con dos aspectos de 5 | Componentización; HTTP/estado/seguridad; tarjetas/gráficos/filtros; xUnit/Moq |
| Asignación práctica de Fase 4 | SPA 20; Context/temas 20; dashboard 15; xUnit/Moq 15; integración 10 = 80 | Mantiene requisitos anteriores y exige Azul UNET en el tema |
| Sistema de Evaluación / PDF | Práctica 80 y evaluación teórica 20; referencia a cobertura completa | No convierte una suite aprobada en 100% de cobertura ni elimina las diferencias anteriores |

No se suman estas ponderaciones. La decisión de qué instrumento determina la nota corresponde al profesor.

## Classroom: correspondencia completa

| Requisito | Implementación | Evidencia y límite |
| --- | --- | --- |
| React 18, componentes y reutilización | React 18.3.1, TypeScript, Vite, React Router; Field, Controls, SidePanel, SearchPicker, TableScroll y componentes del animal; rutas cargadas con lazy/Suspense | Build de producción aprobado; 107 pruebas Vitest y recorridos reales de navegación. No se cambió a React 19 |
| Estado local/global eficiente | AuthContext y ThemeContext; borradores locales; hooks de recursos con AbortController, cancelación de consultas obsoletas y conservación de datos al actualizar | Pruebas DOM de carga, reintento, cancelación, formularios y filtros; sesión persistente comprobada tras recarga/cierre |
| REST, JWT y errores | Cliente fetch central, Bearer en memoria, refresh HttpOnly/CSRF, renovación coordinada, RFC 7807 y errores asociados por campo | API protegida, permisos/finca en servidor; 401/403/409, revocación, reintentos e idempotencia comprobados. fetch es una alternativa permitida por la teoría |
| Testing unitario/integración frontend | Vitest + Testing Library + user-event + jsdom; componentes esenciales montados e interacción del usuario, además de funciones aisladas | Login, editor animal, dashboard, pesaje, Field, filtros, recursos, diagnósticos y tabla desbordada. Playwright añade integración navegador/API/PostgreSQL; no sustituye la suite de componentes |
| Core Web Vitals y carga/respuesta | web-vitals registra CLS/LCP/INP/FCP/TTFB; fetch mide recepción de respuesta y useResource incluye lectura/decodificación de datos; p75 local de HTTP y de carga de datos | Mi cuenta → Ver rendimiento de la aplicación, solo Admin. Exportación JSON sin tokens, rutas, IDs, cuerpos ni entradas DOM. Métricas ausentes aparecen pendientes; no como cero |
| Calidad UI/UX | Superficies neutras, paleta semántica, tipografía local, Phosphor, navegación móvil con Más, foco/teclado, mensajes accionables, respeto de movimiento reducido | 44 comprobaciones axe sobre once pantallas, dos temas y dos tamaños sin infracciones detectadas; no constituye certificación WCAG completa ni estudio de usabilidad de campo |

La instrumentación observa el documento actual. No atribuye automáticamente LCP/CLS a cada navegación SPA ni pretende medir un percentil 75 de toda una población con un único navegador. Las solicitudes canceladas, errores y respuestas HTTP se distinguen. El historial mantiene hasta 100 muestras en memoria y se limpia al salir. No se envía telemetría a terceros.

## Evaluación del 9 de octubre y asignación anterior

| Criterio del 9 de octubre | Correspondencia |
| --- | --- |
| SPA/componentes: herramientas 5 + modularidad/estado 5 | src/frontend; React/Vite/Tailwind integrado, rutas y contextos separados, controles reutilizables y vistas adaptables |
| API/estado: comunicación/errores 5 + JWT/seguridad 5 | AuthContext, session, useResource, cliente autorizado, permisos/roles en servidor y feedback contextual |
| Dashboard: tarjetas 5 + gráficos/filtros/integración 5 | Cuatro KPI reales: leche registrada, bovinos con peso/edad comparable, preñez observada y existencias críticas; gráficos, tablas y filtro finca/período; SSE entre sesiones y consulta de respaldo |
| xUnit/Moq: lógica 5 + aislamiento/cobertura 5 | 236 UnitTests con repositorios simulados, Arrange–Act–Assert y Verify; 156 Core.Tests adicionales; reportes TRX y Cobertura separados |

La asignación de 80 puntos tiene estos mismos ejes más Context API/tema. Se cumple el estado y la persistencia del tema, pero la paleta solicitada por el usuario se aparta del Azul UNET literal. El dashboard conserva costo, valor de referencia, umbrales y rotación trazable, adaptados a insumos de finca. No se etiqueta valor de catálogo como venta o ganancia realizada. Los nombres ProductService/IProductRepository/PowerBIDashboard.jsx del ejemplo se adaptan a CrudService<Product, ProductRequest>/IManagementRepository/DashboardPage.tsx; se demuestra la misma responsabilidad con implementación propia, sin copiar nombres artificialmente.

Tailwind 4 está integrado con el plugin de Vite y su configuración CSS. La ausencia del archivo tailwind.config.js de un ejemplo antiguo no implica ausencia de Tailwind. Las clases responsivas y la hoja de estilos forman parte del build probado.

## REAF-F4: diez requisitos transversales

| Punto | Estado | Implementación / matiz |
| --- | --- | --- |
| 1. JWT seguro | Implementado | Token en memoria; cookie HttpOnly, SameSite y CSRF para renovación; Secure en HTTPS. HTTP localhost no acredita un despliegue HTTPS remoto |
| 2. RFC 7807 | Implementado | title/detail y errores de campos; avisos accesibles persistentes en formularios y feedback de operaciones. Un error que debe corregirse permanece visible en su contexto |
| 3. Guards RBAC | Implementado | RequireSession y controles de rol/permisos de cada vista; API mantiene la autorización efectiva aunque se omita la UI |
| 4. Validación cliente | Implementado con alcance | Required/tipos/fechas/rangos y relaciones locales: máximo > mínimo, precisión decimal, geometría completa dentro del plano, período permitido. Reglas cruzadas de finca, historia, retiro y concurrencia se validan en servidor; no se afirma duplicación total de FluentValidation ni uso de Zod |
| 5. Búsqueda/paginación servidor | Implementado en listas extensas | Animales, selectores, registros clínicos/producción y residentes usan consultas paginadas. Series analíticas y agregados se solicitan por período; los catálogos de selección no se presentan como paginación universal |
| 6. Responsivo | Implementado y probado | Escritorio/móvil, cinco accesos móviles incluyendo Más; tablas con scroll accesible por teclado solo cuando desbordan; sin desbordamiento horizontal de página en las pantallas verificadas |
| 7. Carga/skeleton | Implementado | Estados de sesión, rutas y recursos; se conservan datos durante actualización y se ofrecen reintentos; reserva de espacio del dashboard al cargar |
| 8. USD/Bs y tasa oficial | Parcial en la demostración | Conversión, dos decimales, fecha efectiva, historial y auditoría implementados; entrada manual explícita. Falta registrar una referencia oficial verificable en la instancia de demostración. Sin ella Bs queda deshabilitado |
| 9. Docker | Implementado | PostgreSQL 15 + inicialización .NET 10 + API + Nginx con docker compose up --build -d --wait; volumen vacío y entorno existente comprobados. No se promete que una descarga/build en frío tarde menos de dos minutos |
| 10. Defensa E2E | Preparado, pendiente de realización | Guion y evidencia técnica disponibles. Ensayo presencial, acceso desde el equipo de presentación, entrega de enlace/formulario y sustentación corresponden al equipo |

## Grupo 3: trazabilidad ganadera

| Recomendación | Evidencia | Alcance que se debe explicar |
| --- | --- | --- |
| GDP y avisos de rendimiento | AnimalGrowthService, curva Recharts, objetivos por finca/animal y GrowthAlerts | Diferencia de peso / días entre fechas distintas; último pesaje diario para curva, historial íntegro. Avisos calculados de registros, no historia de notificaciones |
| Sanidad y retiro | Historia clínica, tratamientos y WithdrawalPolicy; bloqueos para leche/sacrificio en API | El retiro farmacológico no equivale a caducidad del insumo. No se elimina sacrificio para reactivar animales |
| Carga por hectárea | Animales presentes / hectáreas, capacidad y traslados persistidos | Conteo de animales, no unidades animales normalizadas ni recomendación agronómica profesional |
| Ficha 360° | Pestañas de resumen, sanidad, reproducción, producción, genealogía navegable, ubicación, crecimiento y fotografías | Una pestaña activa; URL directa, teclado y preservación de formularios. Fotografías con fecha y acceso privado |
| Mapa de potreros/lotes | Plano configurable, capacidad/permanencia y residentes en panel lateral | Geometría esquemática persistida, no GPS/CAD. Permanencia desde última entrada registrada; datos faltantes declarados |
| Pesaje en manga | Captura consecutiva sin recarga, selección/búsqueda y envío idempotente | Alta velocidad por registros sucesivos, no integración física con una báscula ni traslados masivos |
| Fotos comprimidas | PhotoUploader/compress y API privada | Compresión cliente y URL autenticada; no se publican uploads privados mediante Nginx |
| Excel/PDF | reportExport, reportes lácteos/sanitarios y selección finca/período | Librerías cargadas bajo demanda; exporta registros autorizados. No certifica formatos regulatorios externos |
| Leche diaria por lote | milkByDay y milkByCurrentLot | Litros comparables; lote actual del animal, no reconstrucción histórica del lote del ordeño |
| Distribución de peso/edad | WeightDistributionByAge, tabla y gráfico con selector de cohorte | Conteos en rangos de 100 kg y 600+; último peso por bovino en el período y edad en esa fecha; nacimiento desconocido excluido. El promedio por edad también se conserva |
| Preñez/fertilidad | ReproductionAnalytics y dashboard | Cohortes observadas con numeradores/denominadores y pendientes. No se extrapola al rebaño completo ni se llama tasa estándar de preñez de 21 días |

La distribución de pesos era una brecha: un promedio no muestra distribución. Se incorporó un agregado real del backend y se contrastaron conteos de la interfaz con API en ambos tamaños. No se añadió una tabla o datos inventados para cubrirla.

## Cómo interpretar el cumplimiento y la cobertura

La revisión no calcula un porcentaje de cumplimiento ni predice una nota. El documento **Instrucciones y Evaluación 09 Octubre** exige un «nivel razonable de cobertura», y Classroom pide cobertura adecuada de componentes esenciales; ninguno fija 100% de líneas. Los cuatro criterios del documento tienen implementación y evidencia. El dato de cobertura global se publica como medición técnica, no como porcentaje de requisitos cumplidos.

La referencia a cobertura completa pertenece al material anterior. La ausencia de una tasa verificada es un pendiente para demostrar Bs; JWT y colores son decisiones frente a fuentes que difieren; los permisos del catálogo se contrastan con un antecedente de Fase 3. Estas categorías no equivalen a funcionalidades ausentes de los cuatro criterios vigentes.

## Auditoría persistida y alcance

AuditLogs registra autor, fecha UTC, acción, entidad/ID, finca cuando corresponde y snapshots JSON anteriores/nuevos de escrituras realizadas por ManagementRepository. La gestión de usuarios registra creación, actualización y restablecimiento de contraseña; este último guarda el evento y revocación de sesiones, nunca la contraseña. Las pruebas de integración verifican persistencia de eventos y ausencia de contraseñas en el registro.

La ampliación del 8 de octubre incorpora el visor administrativo /auditlogs, API filtrada/paginada, detalle lateral de cambios y eventos de login/rechazo/logout. Las escrituras cubiertas registran la IP de conexión; las cabeceras del cliente solo se aceptan desde proxies confiables configurados. Los eventos históricos sin IP conservan ese dato ausente. SQL externo y lecturas ordinarias siguen fuera del registro. Consultar [alcance, eliminación y límites de auditoría](fase4-registros-auditoria.md). El visor es una mejora autorizada del sistema, no un requisito específico identificado en los instrumentos de Fase 4.

Mi cuenta se abre desde el avatar del encabezado, sin entrada en la barra lateral o Más. El icono de perfil aparece al pasar el cursor o al enfocar con el teclado, y el enlace respeta la protección de cambios sin guardar.

## Contradicciones y cumplimiento parcial

1. **JWT persistente:** el quiz/material presenta localStorage, mientras REAF recomienda memoria/cookies seguras. Se conserva JWT en memoria y renovación HttpOnly/CSRF, que cumple la recomendación de la página y restaura sesión. Es una decisión técnica que debe defenderse; no se declara que el profesor la haya aprobado.
2. **Identidad visual:** la asignación/rúbrica anterior exige Azul UNET #003366; el usuario pidió expresamente una paleta de finca. Se mantuvo esa decisión: neutros dominantes, verde para acciones/estados positivos y azul/ámbar/rojo/tierra según función. No es cumplimiento literal del requisito de marca institucional.
3. **Rol Employee del catálogo:** la asignación de Fase 3 permite crear productos al empleado. Actualmente ProductsController y los permisos sembrados reservan creación/edición a Admin/Administrador, coherente con la UI y el control de precios/retiro del catálogo compartido. Es una desviación del antecedente de Fase 3; no se amplió un permiso sensible silenciosamente durante el pulido de Fase 4.
4. **Cobertura:** 100% de casos aprobados no es 100% de cobertura. La medición global frontend es 36,49% de líneas / 21,81% de ramas, incluyendo módulos no montados por Vitest; Login 89,74%, editor animal 90,62%, dashboard 91,30%, pesaje 89,65% y diagnósticos 85,71% de líneas. Los recorridos E2E de esos otros módulos no se suman a V8. Core.Application: UnitTests 68,05% líneas / 67,32% ramas; Core.Tests 88,50% / 57,80%. Ninguna suite acredita la exigencia literal de cobertura completa.
5. **Cotización:** conversión implementada, pero no existe una tasa BCV vigente verificada en la demo. El sitio oficial no respondió a la consulta automatizada. Debe incorporarse una referencia fechada y comprobable por el administrador antes de demostrar Bs; las tasas sintéticas de las pruebas permanecen únicamente en la base desechable.
6. **Rendimiento real:** existen mediciones y umbrales observables. Las muestras locales no certifican Core Web Vitals del percentil 75 de usuarios reales, conexiones lentas o equipos de finca. Las rutas SPA y las exportaciones grandes requieren evaluar su uso real; ExcelJS permanece como chunk grande cargado bajo demanda.

## Verificación y siguiente paso de entrega

- Build TypeScript/Vite de producción y Docker aprobados; la instancia local conserva sus datos.
- 107 Vitest aprobadas; 236 UnitTests y 156 Core.Tests aprobadas.
- 127 Playwright aprobadas y una omisión intencional de un caso exclusivo móvil en el proyecto escritorio; PostgreSQL aislado.
- 44 comprobaciones axe sin infracciones detectadas, más foco, Escape, teclado, contraste y movimiento reducido.
- Evidencia histórica Newman: 30 solicitudes y 44 assertions aprobadas en la revisión anterior, identificada como tal; no se presenta como ejecución nueva de esta auditoría visual.

Para presentar: abrir el dashboard, contrastar un KPI con API, cambiar finca/período, registrar pesajes consecutivos, mostrar retiro y rechazo esperado, abrir potrero en lateral y finalmente demostrar revocación de una cuenta de prueba. Mostrar Mi cuenta → Rendimiento y explicar valores pendientes, límites del muestreo y p75 local. Utilizar la [guía del 9 de octubre](fase4-evaluacion-09-octubre.md) para los escenarios xUnit/Moq.

Quedan acciones de entrega y sustentación, no botones ficticios en la UI: referencia BCV comprobable, ensayo en el equipo/red de presentación, publicación del enlace requerido y explicación de las desviaciones anteriores. La revisión del PR no sustituye el envío del entregable ni integra main automáticamente.

## Actualización: visor de auditoría y conservación

La ampliación posterior a la revisión visual añade el módulo de consulta, IP y eventos de acceso, y bloquea borrar físicamente un animal con traslados. [Documentación del módulo](fase4-registros-auditoria.md). Verificación actual: 237 UnitTests, 169 Core.Tests, 117 Vitest y 133 Playwright aprobadas, una omisión exclusiva móvil y 48 combinaciones esenciales de axe sin infracciones, más dos detalles laterales. V8 actual: 40,04% de líneas / 24,41% de ramas globales; visor 100% / 76%. Las mediciones .NET y de cobertura anteriores citadas arriba conservan su alcance histórico. [Evidencia de la ampliación](evidence/phase4-audit-module/README.md).
