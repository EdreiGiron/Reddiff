# ADR 0003: Persistencia con EF Core y privilegios separados

- Estado: aceptada
- Fecha: 2026-09-03

## Contexto

La API necesita acceder a PostgreSQL sin acoplar el dominio al motor de base de datos ni utilizar la cuenta administradora durante la operación normal. También debe permitir migraciones reproducibles y comprobaciones de salud que no expongan detalles internos.

## Decisión

- Entity Framework Core se utilizará únicamente desde `RedDiff.Infraestructura`.
- Npgsql será el proveedor de PostgreSQL y se configurará para PostgreSQL 18.6.
- Las versiones de paquetes .NET se administrarán centralmente en `Directory.Packages.props`.
- Las migraciones se almacenarán en `RedDiff.Infraestructura` y utilizarán el esquema `reddiff`.
- La cuenta administradora se reservará para aprovisionamiento y migraciones.
- La API utilizará `reddiff_app`, sin privilegios para crear roles, bases de datos o esquemas.
- La contraseña se suministrará mediante configuración externa y nunca se escribirá en `appsettings`.
- En ambientes distintos de desarrollo se exigirá TLS con validación completa del servidor PostgreSQL.
- La API expondrá comprobaciones separadas de vida y preparación, sin publicar mensajes de excepción ni cadenas de conexión.

## Consecuencias

La solución obtiene una frontera clara entre aplicación y persistencia, configuración reproducible y menor impacto si las credenciales de ejecución fueran comprometidas. Como costo, las migraciones deberán ejecutarse con una identidad administrativa independiente antes de iniciar una nueva versión de la API.
