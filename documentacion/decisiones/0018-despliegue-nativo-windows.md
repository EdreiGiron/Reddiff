# 0018. Despliegue nativo en Windows

## Estado

Aceptada.

## Contexto

LABYMED dispone de servidores de producción Windows. Docker Desktop no admite Windows Server y la composición existente utiliza imágenes Linux de PostgreSQL, Nginx y .NET, por lo que convertirla a contenedores Windows no ofrece una ruta mantenible.

Windows Server 2016 continúa siendo técnicamente compatible con .NET 10, pero PostgreSQL 18 no se certifica para esa versión. PostgreSQL 15 sí dispone de instalador probado para Windows Server 2016.

## Decisión

Se incorpora una variante nativa con las siguientes características:

- IIS administra HTTPS, disponibilidad y ciclo de vida de ASP.NET Core;
- Angular se publica dentro de `wwwroot` y se sirve desde la misma aplicación;
- PostgreSQL se ejecuta como servicio local de Windows;
- la versión mayor de PostgreSQL es configurable entre 15 y 18;
- la conexión local sin TLS requiere una excepción explícita y solo acepta direcciones loopback;
- PowerShell automatiza empaquetado, instalación, migración, inicialización, verificación, respaldo y actualización;
- los secretos se protegen mediante ACL y nunca se incorporan al paquete.

## Consecuencias

RedDiff puede operar de forma nativa en Windows Server 2016 y Windows 11 sin Docker. El procedimiento agrega dependencia de IIS, del Hosting Bundle de .NET 10 y de una instalación local de PostgreSQL. Windows Server 2016 debe considerarse una plataforma transitoria debido a su fecha próxima de fin de soporte extendido.
