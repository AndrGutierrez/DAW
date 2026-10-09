# Investigación de recursos para la interfaz — Fase 4

Revisión original del 7 de octubre de 2026, inicialmente sin implementar. La selección autorizada el 8 de octubre se aplica en [pulido de UI](fase4-ui-pulido.md). Las pestañas reales de la ficha y el detalle lateral de Potreros se implementaron con los componentes existentes y la plataforma web.

## Selección recomendada

| Recurso | Utilidad concreta | Coste o condición | Prioridad |
| --- | --- | --- | --- |
| [Phosphor Icons para React](https://github.com/phosphor-icons/react) | Familia uniforme para animales, sanidad, ubicación y acciones. El catálogo incluye [Cow](https://github.com/phosphor-icons/core/blob/main/assets/regular/cow.svg). | Licencia MIT. Usar una sola familia, importar los iconos necesarios y comprobar la versión elegida antes de sustituir el adaptador actual. | Alta para el siguiente pulido. |
| [Source Sans 3, Adobe](https://github.com/adobe-fonts/source-sans) | Tipografía concebida para interfaces; candidata para etiquetas, formularios, datos y tablas. | Licencia OFL 1.1. Servir WOFF2 desde la aplicación con licencia y fuente del sistema de respaldo. Revisar números, acentos y legibilidad en móvil. | Alta; cambio acotado. |
| [Radix Primitives](https://www.radix-ui.com/primitives/docs/overview/introduction) | Diálogos, menús, selectores y tooltips sin estilos, con gestión de foco y teclado; permite conservar nuestra paleta. | Añade dependencias y requiere integrar estilos. Confirmar compatibilidad de la versión elegida con React 18 y probar cada componente. | Selectiva, por ejemplo un selector con búsqueda. |
| [Motion para React](https://motion.dev/docs/react-accessibility) | Transiciones de entrada/salida o cambios de disposición, con opciones de movimiento reducido. | Añade una dependencia. Las transiciones sencillas ya pueden resolverse con CSS y movimiento reducido. | Posponer hasta completar lo funcional. |

Recomendación: Phosphor + Source Sans 3 y adopción selectiva de Radix. Motion queda reservado para una necesidad que CSS no resuelva bien. La integración de Phosphor, Source Sans 3 y el selector con Radix se realizó después de autorizarla el 8 de octubre; sus pruebas están en [verificación](fase4-verificacion.md). Las transiciones aplicadas se resuelven con CSS y movimiento reducido.

## Animales y potreros

Priorizar fotografías reales del animal mediante el flujo protegido existente. Un icono puede señalar ausencia de fotografía; una imagen decorativa no representa salud ni tamaño del potrero. Mantener el semáforo junto a cantidades y capacidad reales, con texto que explique cada estado.

La ficha muestra un panel por pestaña y conserva los valores al alternarlas. Peso y GDP permanecen dentro de Crecimiento. El detalle de ocupación abre desde el lateral derecho y ocupa el ancho disponible en móvil. Su representación sigue siendo esquemática y carece de coordenadas geográficas.

## Mejoras del login propuestas y aplicadas en el pulido

1. Mostrar/Ocultar contraseña con un botón que no envíe el formulario, nombre accesible y estado comunicado. Conservar valor, autocompletado y foco; Enter sigue enviando.
2. Aviso con icono, título, explicación y siguiente acción. Distinguir credenciales inválidas, sesión vencida y fallo de conexión sin revelar si existe una cuenta concreta.
3. Asociar errores con campos y enfocar el aviso o primer campo incorrecto. Comunicar el estado de envío y evitar solicitudes duplicadas.
4. Mantener la instrucción de contactar al administrador mientras no exista recuperación real de acceso; no ofrecer acciones que el backend no soporte.

Estas interacciones pueden realizarse con el sistema actual, sin paquetes externos. [WAI explica cómo comunicar errores y confirmaciones sin depender únicamente del color](https://www.w3.org/WAI/tutorials/forms/notifications/).

## Criterios y límites

La navegación sigue el [patrón WAI-ARIA de Tabs](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/). El panel lateral usa un diálogo nativo y los criterios de [foco, Escape y retorno al activador](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/). Cambiar de pestaña no desmonta el formulario; salir de la ficha continúa sujeto a las protecciones que tenga cada formulario.

La [página REAF-F4 del profesor](https://gramirezsunet.github.io/desarrolloAplicacionesWeb/REAF-F4/) pide para el Grupo 3 ficha 360°, gráficas GDP, ocupación de potreros y pesaje masivo. Este pulido facilita esos flujos. La revisión pendiente de inventario, dashboard, exportaciones y evidencias de entrega está en [el estado de Fase 4](fase4-estado.md).
