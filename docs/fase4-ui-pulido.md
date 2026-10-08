# Pulido de interacción y recursos de UI

Implementado el 8 de octubre de 2026, a partir de la investigación UI autorizada por el usuario.

## Recursos aplicados

La aplicación usa Source Sans 3 variable, servida localmente como WOFF2 (170.188 bytes), con licencia OFL de Adobe y respaldo del sistema. La descarga procede del commit 87b37a2daaed80fcb8e8ccb0085c4d72ddade12e del repositorio oficial. El navegador no consulta servicios externos de fuentes. Las tablas, indicadores y cantidades utilizan cifras tabulares. Noto Sans continúa limitado a los reportes PDF.

Phosphor React 2.1.10 reemplaza el adaptador SVG anterior. Se importan individualmente los iconos utilizados; el icono de ganado es Cow y el de potreros es Farm. Se conserva un adaptador semántico para las pantallas. Los iconos decorativos tienen aria-hidden y las acciones mantienen nombres accesibles.

Radix Popover 1.2.0 se incorpora al selector de animales con búsqueda. Su compatibilidad con React 18 fue comprobada en las dependencias declaradas y en el build. El selector avanzado se carga bajo demanda; su chunk es aproximadamente 69 kB antes de compresión y 24 kB comprimido. La carga inicial no necesita el código de Radix.

No se necesita Motion para estas transiciones. CSS aplica entradas breves, estados de presión y un indicador de carga. La preferencia prefers-reduced-motion elimina las animaciones. Los selectores pequeños, checkbox y campos numéricos conservan controles nativos y el sistema visual de granja existente.

## Acceso con orientación útil

PasswordInput permite mostrar u ocultar la contraseña sin enviar el formulario ni cambiar su valor/autocompletado. El botón comunica su estado y nombre; una activación con puntero conserva foco y selección de texto en el campo. El teclado puede enfocar y activar el botón. Se avisa cuando Bloq Mayús está activado.

El login valida campos vacíos, asocia cada error mediante aria-describedby y enfoca el primer campo pendiente. Un error de credenciales muestra título, explicación y siguiente acción, con foco en el aviso. Los problemas de conexión conservan los datos y permiten volver a enviar. Los errores no indican si existe una cuenta concreta ni ofrecen recuperación de acceso sin soporte del backend.

Mientras se envía, los campos quedan en modo de lectura, el botón se deshabilita y se comunica el estado. Una guardia inmediata impide duplicar solicitudes con Enter. Una sesión autenticada cuya renovación falla con 401 comunica que terminó; el inicio anónimo normal y el logout no se describen como expiración. La navegación conserva el destino para volver a él después del acceso.

## Selección de animales

El selector se usa para madre/padre, reproductor, cría y asociación opcional de salidas de inventario. Busca en la API por arete o nombre con un retraso de 250 ms y ofrece paginación. Se conservan filtros por finca/especie/sexo, exclusión del animal editado y, cuando corresponde, descendientes de la madre. El consumo busca solamente animales activos de su finca.

El control abre un diálogo de selección, enfoca la búsqueda y permite recorrer opciones con Tab o flechas, Inicio/Fin y Enter. Elegir o pulsar Escape devuelve el foco al activador. Escape dentro del selector no cierra el panel de inventario. El contenido se renderiza dentro del diálogo nativo cuando se usa allí, para mantenerlo en su capa modal. Los resultados anteriores no se ofrecen durante una nueva búsqueda. La selección puede limpiarse expresamente.

El valor seleccionado se conserva al cerrar o cambiar de página; el editor sigue sujeto a la protección de cambios sin guardar. Los formularios bloquean también el selector mientras el envío está pendiente o incierto. No se cambia ninguna regla de negocio ni el esquema.

## Verificación y límites

Los resultados ejecutados se añaden a fase4-verificacion.md. La revisión comprueba los recorridos afectados, foco, teclado, adaptación móvil, contraste seleccionado, fuente local y movimiento reducido. No equivale a una auditoría completa con lectores de pantalla, todos los navegadores o todas las discapacidades. El aviso de Vite por tamaño del exportador ExcelJS continúa siendo una limitación del bloque de reportes, cargado bajo demanda.

Referencias: [Phosphor React](https://github.com/phosphor-icons/react), [Source Sans de Adobe](https://github.com/adobe-fonts/source-sans), [Radix Popover](https://www.radix-ui.com/primitives/docs/components/popover) y [notificaciones de formularios WAI](https://www.w3.org/WAI/tutorials/forms/notifications/).
