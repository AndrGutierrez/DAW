# Fase 4: sesión persistente y base de la SPA

## Decisión y relación con la consigna

La sesión del usuario debe sobrevivir a una recarga. Para conseguirlo, la SPA conserva el JWT de acceso únicamente en memoria y utiliza un token opaco de renovación en una cookie persistente HttpOnly. Al iniciar la aplicación, el navegador presenta esa cookie al servidor y obtiene un JWT nuevo.

Esta es una implementación de sesión persistente con JWT; la persistencia no exige almacenar el mismo JWT indefinidamente. La decisión toma la alternativa de memoria y cookies seguras de los [lineamientos de Fase 4](https://gramirezsunet.github.io/desarrolloAplicacionesWeb/REAF-F4/). Si una instrucción local exige literalmente JWT en localStorage, debe explicarse esta diferencia en la defensa. Este documento no equivale a una aprobación del profesor ni garantiza una calificación.

OWASP recomienda evitar identificadores de sesión en localStorage porque JavaScript puede leerlos; HttpOnly reduce esa exposición. La cookie no elimina el riesgo de XSS: un script ejecutado dentro del sitio todavía puede efectuar solicitudes y acceder al JWT de corta duración mientras la página está abierta. Referencia: [OWASP, almacenamiento local](https://cheatsheetseries.owasp.org/cheatsheets/HTML5_Security_Cheat_Sheet.html#local-storage).

## Flujo implementado

1. La SPA solicita GET /api/auth/session/csrf. ASP.NET Core entrega un token de solicitud y una cookie antiforgery HttpOnly.
2. El login envía usuario, contraseña y X-CSRF-TOKEN mediante POST /api/auth/session/login.
3. El servidor comprueba las credenciales, emite el JWT y coloca el token de renovación en Set-Cookie. El JSON contiene accessToken, accessTokenExpiresAt y user; no contiene refreshToken.
4. AuthContext publica el usuario, roles y permisos. El cliente agrega Authorization: Bearer en solicitudes a recursos /api del mismo origen.
5. Antes de utilizar un JWT próximo a vencer, o ante un primer 401 de un recurso, el cliente renueva la sesión. Un 403 se conserva como denegación de permisos y no dispara renovaciones.
6. Al recargar se pierde el JWT de memoria. La cookie permite emitir otro y recuperar la identidad sin solicitar nuevamente la contraseña.
7. POST /api/auth/session/logout revoca el token de renovación y elimina la cookie. El navegador borra su estado solamente después de confirmar la respuesta del servidor.

Las solicitudes simultáneas comparten una renovación en cada pestaña. Web Locks serializa login, refresh y logout entre pestañas del mismo origen cuando el navegador ofrece esa API. BroadcastChannel comunica el cierre de sesión sin compartir tokens. Las respuestas pendientes se descartan cuando cambia la sesión.

## Controles y límites

| Elemento | Comportamiento |
| --- | --- |
| JWT de acceso | Memoria; cinco minutos por defecto en appsettings.json; firma, emisor, audiencia y vencimiento verificados en la API |
| Token de renovación | Opaco, aleatorio de 64 bytes; cookie HttpOnly, SameSite=Strict y Path=/api/auth/session |
| Persistencia | Siete días desde la emisión o la última renovación; cada renovación rota el token y vuelve a iniciar ese plazo |
| HTTPS | Obligatorio para el contrato del navegador fuera de localhost/loopback en Development; Secure en producción |
| CSRF | Token antiforgery de solicitud obligatorio en login, refresh y logout; SameSite es una capa adicional |
| Base de datos | Los nuevos tokens se almacenan como sha256: seguido de su hash SHA-256; nunca se acepta el hash como credencial |
| Rotación | El token anterior se revoca y el reemplazo se guarda en la misma operación SaveChanges; RevokedAt es un token de concurrencia de EF |
| Caché | Las respuestas de sesión se sirven con no-store |
| Frontend | No usa localStorage ni sessionStorage para credenciales; localStorage conserva solamente daw.theme |
| Autorización | La API sigue verificando roles, permisos por operación, cuenta activa y fincas asignadas |
| Fotografías | Se solicitan con Bearer y se presentan mediante URL de Blob revocable; no se vuelven públicas |

La protección CSRF utiliza los mecanismos de ASP.NET Core, con cabecera X-CSRF-TOKEN. Referencia: [Microsoft, prevención de CSRF](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).

**El logout no invalida criptográficamente un JWT de acceso ya emitido.** Un JWT sustraído puede conservar su validez hasta su vencimiento; permisos y cuenta activa siguen comprobándose en los recursos protegidos. Implementar revocación inmediata de cada JWT requeriría estado adicional o un identificador de sesión comprobado en cada solicitud.

La rotación rechaza reutilizaciones y solicitudes concurrentes del mismo token. Este incremento no incorpora revocación automática de toda una familia de tokens ante un replay: detectar ese replay no revoca un sucesor que ya haya sido emitido. Tampoco agrega un límite absoluto de duración a una sesión que se renueva de forma continua.

Web Locks y BroadcastChannel requieren soporte del navegador. Sin Web Locks sigue existiendo deduplicación dentro de la pestaña, pero dos pestañas pueden competir por la misma cookie; el servidor admite una sola renovación y la otra recibe 401. El caso entre pestañas se verifica en Chromium.

La ejecución HTTP local es exclusivamente una facilidad de desarrollo. Para publicar, debe existir HTTPS en el punto de entrada y una configuración de proxies confiables que permita a ASP.NET Core reconocer ese HTTPS. No basta con cambiar el entorno a Production sobre HTTP. La terminación TLS y la configuración del entorno publicado no forman parte de esta verificación local.

## Compatibilidad y conservación del modelo

Las rutas existentes /api/auth/register, /login, /refresh y /me conservan sus contratos para Postman y clientes explícitos de la API. Los clientes de esas rutas son responsables de custodiar el token de renovación que reciben. La SPA utiliza exclusivamente /api/auth/session para sus operaciones de sesión.

Los tokens antiguos guardados en texto plano se aceptan hasta su renovación, revocación o vencimiento. La siguiente renovación crea una fila con hash. No se ejecuta un borrado de sesiones o tablas como mecanismo de actualización.

No se eliminan entidades ganaderas ni se añade una migración de reducción. El cambio de concurrencia afecta al seguimiento de EF y al snapshot; no necesita cambiar la columna existente en PostgreSQL.

GET /api/animals/page añade búsqueda por arete, nombre e identificación oficial, filtro por estado y finca, conteo y paginación en el servidor. Las consultas aplican las fincas accesibles antes de contar o paginar. La página se limita a 100 registros y la búsqueda a 100 caracteres. GET /api/animals conserva su respuesta anterior.

La SPA permite consultar el listado y la ficha básica del animal, fotografías privadas y enlaces a sus progenitores. Tener entidades clínicas y reproductivas mapeadas no significa que estos flujos estén disponibles en esta interfaz.

## Cómo demostrarlo

Con el entorno iniciado:

1. Abrir la SPA e iniciar sesión con el administrador configurado.
2. Inspeccionar el JSON del login: no debe contener refreshToken.
3. Inspeccionar las cookies: daw.refresh debe ser HttpOnly, persistente y limitada a /api/auth/session. En HTTPS también debe tener Secure.
4. Inspeccionar localStorage y sessionStorage: no deben contener tokens. Cambiar el tema y comprobar que solo se conserva esa preferencia.
5. Recargar y abrir otra pestaña. Ambas deben recuperar el usuario y los animales autorizados.
6. Buscar DEMO-001 y abrir su ficha. La búsqueda debe aparecer en la solicitud /api/animals/page, y la ruta directa debe funcionar al recargar.
7. Cerrar sesión. Las pestañas abiertas deben volver al login y una nueva recarga no debe recuperar la sesión.
8. Demostrar con pruebas que login, refresh y logout sin CSRF devuelven Problem Details, y que reutilizar un token revocado produce 401.

## Explicación breve para la defensa

«La persistencia pertenece a la sesión. El JWT de acceso dura cinco minutos y existe solo en memoria. Una cookie HttpOnly conserva una credencial opaca que permite obtener un JWT nuevo al recargar. Protegemos las operaciones de esa cookie contra CSRF, rotamos la credencial al usarla y la revocamos al cerrar sesión. Así cumplimos la continuidad del acceso y reducimos la exposición de credenciales persistentes a JavaScript. HttpOnly no elimina XSS, y un JWT ya emitido conserva su vencimiento original».

Los resultados ejecutados y sus límites están en [verificación del incremento](fase4-verificacion.md).

## Verificación reproducible

Backend y pruebas aisladas Moq, desde la raíz:

    MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd -W):/src" -w /src \
      mcr.microsoft.com/dotnet/sdk:10.0 dotnet test DAW.slnx -c Release

Frontend, Node.js >= 22.12:

    cd src/frontend
    npm ci
    npm run build
    npm test

En Git Bash sobre Windows puede fijarse el shell de los scripts sin cambiar la configuración global:

    npm --script-shell="C:/Program Files/Git/bin/bash.exe" run build
    npm --script-shell="C:/Program Files/Git/bin/bash.exe" test

Pruebas E2E, contra un entorno aislado ya sembrado:

    export E2E_BASE_URL=http://localhost:18086
    export E2E_ADMIN_USERNAME=admin
    read -rsp "Contraseña de prueba: " E2E_ADMIN_PASSWORD
    export E2E_ADMIN_PASSWORD
    export E2E_EMPLOYEE_USERNAME=employee
    read -rsp "Contraseña de empleado de prueba: " E2E_EMPLOYEE_PASSWORD
    export E2E_EMPLOYEE_PASSWORD
    npx playwright install chromium
    npm --script-shell="C:/Program Files/Git/bin/bash.exe" run test:e2e

Las capturas y trazas pueden contener cookies y datos del entorno de prueba; permanecen ignoradas por Git.
