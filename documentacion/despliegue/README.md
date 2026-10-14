# Documentación de despliegue

RedDiff admite dos modalidades de despliegue:

- [Producción con Docker Compose](produccion-docker-compose.md), recomendada para servidores Linux;
- [Producción nativa en Windows](produccion-windows-nativo.md), basada en IIS, ASP.NET Core y PostgreSQL para Windows.

Ambos procedimientos conservan secretos externos, persistencia de claves, migraciones controladas, verificaciones de salud y respaldos operativos.

La composición conserva PostgreSQL y las claves criptográficas en volúmenes diferentes, no publica la base de datos y exige HTTPS para la web y validación TLS completa entre la API y PostgreSQL.
