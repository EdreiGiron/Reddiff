# Contenedores

## Composición de desarrollo

`compose.desarrollo.yml` mantiene PostgreSQL para el trabajo local. La composición completa de producción se encuentra en `produccion/compose.yml` e incorpora PostgreSQL, API y cliente web.

Controles incluidos:

- PostgreSQL 18.6 fijado explícitamente;
- contraseña suministrada mediante un secreto montado como archivo;
- puerto publicado solamente en `127.0.0.1`;
- red de proyecto independiente y puerto accesible únicamente desde el equipo local;
- volumen persistente en `/var/lib/postgresql`, requerido por la imagen oficial desde PostgreSQL 18;
- verificación de salud con `pg_isready`;
- rotación local de logs para evitar crecimiento ilimitado.

Los comandos operativos están documentados en [../../documentacion/operacion/base-datos-local.md](../../documentacion/operacion/base-datos-local.md).

## Composición de producción

La composición productiva incluye compilaciones multietapa, usuarios sin privilegios para la API y Nginx, comprobaciones de salud, redes separadas, TLS, volúmenes persistentes y secretos montados desde archivos excluidos de Git.

El procedimiento obligatorio se encuentra en [producción con Docker Compose](../../documentacion/despliegue/produccion-docker-compose.md).
