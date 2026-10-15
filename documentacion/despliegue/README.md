# Documentación de despliegue

RedDiff admite las siguientes modalidades de despliegue:

- [Docker Compose en Windows 11](produccion-docker-compose-windows.md), ruta principal para LABYMED S.A.;
- [Docker Compose en Linux](produccion-docker-compose.md), equivalente para servidores Linux;
- [Producción nativa en Windows](produccion-windows-nativo.md), alternativa de contingencia basada en IIS y PostgreSQL local.

Los procedimientos con Compose conservan secretos externos, persistencia de claves, migraciones controladas, verificaciones de salud y respaldos operativos.

La composición conserva PostgreSQL y las claves criptográficas en volúmenes diferentes, no publica la base de datos y exige HTTPS para la web y validación TLS completa entre la API y PostgreSQL.
