# ADR 0005: Sesión web y protección de credenciales

- Estado: aceptada.
- Fecha: 2026-09-04.

## Contexto

El cliente es una aplicación web del mismo sistema. Los requisitos exigen autenticación por usuario y contraseña, autorización por roles, control de sesión, auditoría y ausencia de credenciales expuestas. También se necesita revocar inmediatamente el acceso cuando una cuenta o rol se desactiva.

## Decisión

- Utilizar el esquema de cookies de ASP.NET Core en lugar de emitir tokens propios.
- Mantener la cookie cifrada, inaccesible para JavaScript, limitada al mismo sitio y con treinta minutos de vigencia no deslizante.
- Validar el estado persistente del usuario y del rol en cada solicitud protegida.
- Exigir tokens antifalsificación en operaciones que modifican estado y limitar intentos de inicio de sesión.
- Transformar contraseñas con PBKDF2-HMAC-SHA-256, sal aleatoria, comparación de tiempo constante y un costo de 600 000 iteraciones.
- Mantener únicamente los roles funcionales `Administrador` y `Tecnico`.
- Crear la primera cuenta mediante una operación local sin endpoint de aprovisionamiento ni clave predeterminada.
- Rotar juntas las credenciales locales de administración y aplicación de PostgreSQL, verificando ambas conexiones antes de retirar los respaldos temporales.

## Consecuencias

El navegador no necesita almacenar credenciales reutilizables. La desactivación de una cuenta surte efecto en la siguiente solicitud aunque la cookie aún no haya vencido. El cliente debe conservar cookies, solicitar un token antifalsificación y enviarlo en cada operación insegura.

La consulta de vigencia añade un acceso breve a PostgreSQL por solicitud autenticada. Si la escala futura lo exige, podrá incorporarse una caché de revocación con una caducidad corta sin cambiar el contrato HTTP.

Los tokens portadores para integraciones externas quedan fuera de este bloque. Si se incorporan, deberán provenir de un emisor compatible con OAuth u OpenID Connect y no de un generador de tokens ad hoc.
