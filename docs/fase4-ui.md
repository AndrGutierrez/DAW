# Interfaz de la finca y sistema visual

Este incremento aplica una identidad de granja a las pantallas existentes y convierte los controles repetidos en componentes compartidos. La referencia visual orienta la jerarquía, las tarjetas y la navegación móvil; los datos y las reglas proceden del sistema ganadero existente.

## Paleta con significado

Los fondos neutros y cálidos mantienen la información legible. La identidad no ocupa toda la pantalla: el verde enfatiza acciones y selección; los colores de estado tienen significados distintos.

| Función | Color de referencia en tema claro | Uso |
| --- | --- | --- |
| Fondo | Marfil, #f6f4ee | Espacio de trabajo |
| Superficie | Blanco cálido, #fffefa | Tarjetas, formularios y controles |
| Texto | Tinta, #2e352d | Contenido principal |
| Identidad y acción | Verde bosque, #2f614c | Acción principal y navegación activa |
| Información | Azul mineral, #2d6480 | Enlaces y seguimiento de peso/GDP |
| Confirmación | Verde, #31634c | Animal activo, salud sana y operación confirmada |
| Atención | Ámbar, #85590f | Tratamiento, observación, cuarentena y aviso de capacidad |
| Error o condición crítica | Rojo, #a83c44 | Errores, salud crítica y eliminación |
| Contexto | Tierra, #885b43 | Identificación, etiquetas y contenido contextual |

Las variantes oscuras se declaran junto a las claras en styles.css. Cada estado conserva texto; el color no sustituye su nombre ni introduce porcentajes de salud sin una fórmula. El azul anterior como identidad general se reemplaza por esta paleta; el vínculo académico permanece en el contenido institucional.

## Componentes y legibilidad

Controls.tsx comparte Button, Input, Select y Textarea con referencias y atributos nativos. Field mantiene la asociación de etiqueta, ayuda y error. Se conservan los tipos, nombres y valores usados por los formularios y la API. Los selectores mantienen su comportamiento nativo de teclado y del navegador.

La familia SVG Icon usa una misma escala, trazo y estilo. Los iconos decorativos se ocultan a tecnología de asistencia; las acciones conservan sus nombres. El botón de tema indica qué tema activará. Las confirmaciones de eliminación/descarte utilizan rojo; una confirmación ordinaria utiliza el color de acción.

Se aumentó la legibilidad de etiquetas, tablas y estados. Botones y acciones de texto tienen una altura mínima de 44 px; las entradas ordinarias, 48 px. Checkbox y radio mantienen el control nativo de estado y teclado, con una presentación coherente. Se conservan foco visible, carga, errores, toasts y movimiento reducido. La tipografía usa la familia de interfaz del sistema, sin solicitudes externas de fuentes.

## Recorridos

En móvil, Principal permanece en una barra inferior; deja de ocupar varias filas sobre el contenido. Los toasts y el final del documento reservan espacio para esa barra y para el área segura del dispositivo. Actualmente muestra los cuatro recorridos existentes; al añadir módulos se deberá revisar su organización.

La ficha 360 incluye un resumen antes de los historiales y un navegador de ocho secciones: resumen, sanidad, reproducción, producción, genealogía, ubicación, crecimiento y fotografías. Son enlaces a secciones presentes en el documento, no paneles ocultos. El indicador de sección responde a desplazamiento y selección. Las comprobaciones de permisos permanecen en cada módulo.

Los potreros incorporan una ilustración SVG decorativa y conservan la ocupación, capacidad, densidad y permanencia reales. La ilustración no representa el tamaño o límite geográfico de una parcela. Los residentes, filtros y traslados siguen consultando la API; no se sustituyeron por datos de presentación.

Elegir un lote en el editor del animal ya no cambia el potrero de manera implícita. El lote productivo y la ubicación física se seleccionan por separado. La suite comprueba esa separación antes de asignar expresamente un destino.

## Cambios sin guardar

NavigationProtectionProvider registra los editores modificados y bloquea la navegación interna, incluyendo el botón Atrás. El usuario puede conservar sus valores o confirmar que quiere salir sin guardar. Cerrar sesión también consulta esa protección. Una recarga o cierre utiliza la advertencia nativa del navegador, sujeta al comportamiento de este.

La protección cubre el editor del animal y el mantenimiento de potreros. No persiste borradores después de cerrar el navegador ni cubre todavía todos los formularios clínicos o la cola de pesaje. Guardar correctamente libera la protección antes de navegar; un error conserva los datos. Cancelar un editor solicita confirmación cuando corresponde.

Se utiliza el modo de datos del React Router ya instalado para soportar useBlocker. Se conserva la configuración de rutas de App y el contrato de sesión. No se añadieron dependencias. El fundamento del bloqueo está en la [documentación oficial de React Router](https://reactrouter.com/how-to/navigation-blocking).

## Verificación y límites

Se verifican TypeScript, Vite, Vitest y los recorridos Playwright existentes contra PostgreSQL. Los cuatro nuevos escenarios se ejecutan en escritorio y móvil: contraste en ambos temas, cambios sin guardar ante menú/Atrás/logout, cancelación del potrero y checkbox con teclado, y navegación de secciones con teclado.

La comprobación de contraste mide texto/fondo de los controles y estados renderizados seleccionados, con una relación mínima de 4.5:1. No es una auditoría completa de accesibilidad. La revisión visual incluye acceso, listado, ficha, formulario y potreros en escritorio/móvil y ambos temas; no demuestra compatibilidad universal de navegadores. Los resultados ejecutados se documentan en [verificación](fase4-verificacion.md).

El incremento no cambia el backend ni la base de datos. Inventario, dashboard, alertas generales, exportaciones y evidencia final siguen pendientes según el [estado de Fase 4](fase4-estado.md).
