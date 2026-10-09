# Cierre técnico de la evaluación de Fase 4

> Actualización vigente: [conservación de registros y consulta automática BCV](fase4-conservacion-bcv.md). Sustituye los apartados anteriores de borrado físico y cotización exclusivamente manual. Los conteos de pruebas aquí consignados conservan la fecha y el alcance de esa revisión.


> Actualización posterior: consultar la [auditoría integral](fase4-auditoria-integral.md) y la [evidencia de UI/testing/rendimiento](evidence/phase4-ui-quality/README.md). Incorporan Classroom y el quiz, 107 pruebas frontend, distribución de pesos y los conteos/coberturas actuales. Los resultados de este documento se conservan como evidencia de la revisión anterior.


Revisión del 8 de octubre de 2026. Fuentes: [REAF-F4](https://gramirezsunet.github.io/desarrolloAplicacionesWeb/REAF-F4/), su checklist transversal y Grupo 3, y el documento **Instrucciones y Evaluación 09 Octubre .docx**. El instrumento vigente tiene cuatro criterios de 10 puntos; la [guía de defensa](fase4-evaluacion-09-octubre.md) los relaciona con la implementación. Esta revisión no asigna una nota ni sustituye la sustentación.

## Indicadores reproductivos defendibles

La API filtra eventos del período y bovinas de las fincas accesibles. No estima diagnósticos de animales sin registros.

| Indicador | Numerador y denominador | Exclusiones y alcance |
| --- | --- | --- |
| Preñez en hembras evaluadas | Hembras con último diagnóstico positivo sin parto/aborto posterior / hembras con último diagnóstico concluyente | Una observación por hembra. Último incierto excluye a la hembra del denominador; sin concluyentes devuelve null. No representa la prevalencia de todo el rebaño |
| Fertilidad de servicios evaluados | Hembras con diagnóstico positivo posterior a su último servicio / hembras con diagnóstico concluyente posterior a ese servicio | Una observación por hembra servida. Último servicio por monta/inseminación y último diagnóstico posterior, ambos dentro del período. Sin diagnóstico o con último incierto queda pendiente, visible y fuera del denominador. No es la tasa estándar de preñez por ciclo de 21 días |

Fecha, fecha de creación e ID ordenan eventos del mismo día; no hay hora clínica registrada. Un servicio anterior al período no integra esta cohorte. Parto/aborto posterior cierra el estado de preñez, pero no cambia un diagnóstico positivo obtenido tras el servicio para el resultado observado de fertilidad. Los conteos brutos de diagnósticos permanecen como detalle y no sustituyen estas cohortes.

Implementación: ReproductionAnalytics, OperationsReader, AnalyticsService y DashboardPage. Las tarjetas y secciones muestran porcentajes, cantidades evaluadas y pendientes. La consulta del estado reproductivo de una ficha también restringe eventos a su finca actual.

## Dashboard entre sesiones

GET /api/analytics/changes transmite Server-Sent Events mediante fetch autenticado con Bearer. El servidor invalida indicadores después de escrituras API exitosas; el cliente vuelve a consultar las fuentes autorizadas y conserva la vista mientras carga. El evento solo contiene una revisión, sin animales, usuarios, fincas o credenciales.

El stream requiere los mismos permisos del dashboard, usa JWT en cabecera, se cierra al expirar el token o después de 60 segundos y reconecta con autorización vigente. Heartbeat cada 15 segundos, reconexión progresiva, cancelación al ocultar/cerrar y consulta de respaldo cada 60 segundos. La prueba con dos contextos de navegador demuestra que una producción registrada por Employee actualiza el KPI de Admin sin recarga, en menos de ocho segundos.

El broker está en memoria de **una instancia API**. Escrituras SQL externas o varias réplicas requieren otro mecanismo; el respaldo periódico no las convierte en eventos. No es un sistema de notificaciones históricas ni un WebSocket.

## Referencia USD/Bs con fecha

Los precios del catálogo siguen en USD de referencia. Admin puede registrar una cotización de bolívares por USD y fecha efectiva, consultando [BCV](https://www.bcv.org.ve/). El dashboard convierte costo, valor de referencia y categorías a Bs con dos decimales y muestra fecha, fuente e ingreso manual. La cotización mantiene seis decimales y se persiste con autor, fecha de creación y auditoría; las correcciones son nuevas entradas, sin borrar historia.

El servidor selecciona la última fecha efectiva no posterior a la consulta y, entre correcciones del mismo día, la más reciente. Una tasa futura no sustituye la vigente antes de su fecha. Cero, valores negativos o excesivos y fechas inválidas generan RFC 7807. El identificador de envío hace idempotentes los reintentos.

**No hay una tasa vigente inventada ni verificación automática del BCV.** Sin referencia configurada la API responde 204 y Bs queda deshabilitado con explicación. El acceso automatizado al sitio oficial no estuvo disponible en esta revisión; por ello se identifica explícitamente el registro manual. La prueba utiliza valores sintéticos exclusivamente en la base desechable, no en la demostración del equipo.

Migración DatedExchangeRates aditiva: siete migraciones, 44 tablas (43 de aplicación y una de EF), 72 claves foráneas. No elimina tablas ni reinicia datos existentes. El [ERD](../db/diagram/README.md) procede del esquema migrado.

## Pruebas actuales

| Verificación | Resultado | Alcance |
| --- | --- | --- |
| UnitTests | 235 Passed | xUnit/Moq estricto, referencia solo a Application; reproducción, genealogía, producción/retiro, idempotencia, tasas y lectores simulados |
| Core.Tests | 156 Passed | Pipeline HTTP y EF InMemory, autorización, persistencia/auditoría y eventos SSE |
| Vitest | 65 Passed | Estado/sesión y parser de stream fragmentado; TypeScript y build aprobados |
| Playwright | 16/16 Passed | Una corrida de portada, filtros/analítica y evaluación en desktop/mobile con PostgreSQL físico aislado |
| Newman | 30 solicitudes / 44 assertions, cero fallos | Cuentas, revocación, CSRF/cookies, objetivos, plano, cohortes, tasa y rechazo anónimo SSE |
| Compose vacío / EF | Siete migraciones; sin cambios de modelo pendientes | Inicialización con volumen nuevo, sin reutilizar la base existente |

Cobertura de **Core.Application**: UnitTests 68,56 % líneas / 67,88 % ramas; Core.Tests 88,69 % líneas / 58,76 % ramas. No se suman y la integración no acredita aislamiento Moq. El documento de evaluación no fija un mínimo numérico. Se mantienen reportes reales, con rutas no cubiertas, en [evidencia](evidence/phase4-evaluation/README.md).

## Entrega que realiza el equipo

Ensayar navegación, dos sesiones y reglas de rechazo; explicar cohortes y origen manual de la tasa; presentar código y reportes. La defensa presencial y envío del formulario del profesor quedan a cargo del equipo. HTTPS y acceso desde otro computador requieren verificación en el entorno que se vaya a presentar. No están acreditados por una ejecución HTTP local.

La rama está publicada en el [PR #8](https://github.com/AndrGutierrez/DAW/pull/8), como borrador para revisión. La instancia local http://localhost:18086 usa la versión nueva con siete migraciones y conserva su volumen; no contiene tasas sintéticas. El proyecto PostgreSQL aislado de pruebas fue retirado, incluidos únicamente sus dos volúmenes identificados por etiquetas de Compose. main y el workflow CI no se modificaron.
