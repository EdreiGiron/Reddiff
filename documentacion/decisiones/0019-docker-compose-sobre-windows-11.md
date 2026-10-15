# ADR 0019: Docker Compose sobre Windows 11

- Estado: aceptada
- Fecha: 2026-10-14
- Reemplaza para el despliegue objetivo a: ADR 0018

## Contexto

LABYMED S.A. necesita instalar RedDiff en Windows evitando instalaciones separadas de IIS, PostgreSQL, .NET y Node.js. La composicion productiva existente utiliza imagenes Linux mantenidas para Nginx, ASP.NET Core y PostgreSQL.

## Decision

- El host operativo sera una PC fisica con Windows 11 Pro o Enterprise x64.
- Docker Desktop utilizara WSL 2 y el modo Linux containers.
- Docker Compose ejecutara Nginx, API, PostgreSQL, migraciones e inicializacion.
- La administracion se realizara desde PowerShell mediante scripts versionados.
- Windows publicara unicamente TCP 443 y UDP 514, restringidos por firewall.
- PostgreSQL y el puerto HTTP de la API permaneceran en redes privadas de Compose.
- Los secretos se conservaran fuera de Git y se montaran como archivos.
- Los datos de PostgreSQL y las claves de proteccion utilizaran volumenes independientes.
- La validacion en el ambiente definitivo incluira la conservacion de la IP de origen Syslog.

## Consecuencias

Windows no necesita los runtimes ni servidores utilizados por la aplicacion. Docker Desktop y WSL 2 se convierten en dependencias de plataforma y deben iniciar despues de cada reinicio. El despliegue no se considera aceptado hasta completar pruebas HTTPS, SSH, NETCONF, Syslog, persistencia, respaldo y restauracion en la PC seleccionada.
