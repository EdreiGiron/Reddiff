# Decisión 0002: PostgreSQL en contenedor para desarrollo

- Estado: aceptada.
- Fecha: 2026-09-02.

## Contexto

El sistema necesita una base de datos relacional reproducible para desarrollo y pruebas. PostgreSQL 19 todavía se encuentra en fase beta, mientras que PostgreSQL 18.6 es la versión estable vigente y contiene correcciones recientes de seguridad y funcionamiento.

La imagen oficial modificó desde PostgreSQL 18 la ubicación recomendada del volumen, que ahora debe montarse en `/var/lib/postgresql`.

## Decisión

Se utilizará `postgres:18.6-alpine` en la composición de desarrollo. La configuración incluirá:

- versión explícita de PostgreSQL;
- volumen persistente en la ubicación vigente de la imagen oficial;
- contraseña aleatoria montada como secreto y no como valor versionado;
- publicación del puerto solamente en `127.0.0.1`;
- verificación de salud antes de considerar disponible el servicio;
- scripts PowerShell reproducibles y compatibles con el entorno de desarrollo.

La cuenta inicial será administrativa. Antes de conectar la API se creará un usuario de aplicación independiente y con privilegios mínimos.

## Consecuencias

### Positivas

- Todos los desarrolladores utilizan la misma versión.
- La instalación no depende de PostgreSQL instalado directamente en Windows.
- Los datos sobreviven al reemplazo del contenedor.
- Los secretos locales permanecen fuera del repositorio.

### Costos y precauciones

- Docker Desktop debe estar iniciado.
- El volumen requiere una estrategia explícita cuando se actualice la versión principal.
- La opción `-EliminarDatos` del script de detención borra definitivamente la base local.

## Referencias

- [PostgreSQL 18.6 y versiones compatibles](https://www.postgresql.org/about/news/postgresql-186-1711-1615-1519-1424-and-19-beta-3-released-3365/).
- [Cambio de `PGDATA` y volumen en la imagen oficial](https://hub.docker.com/_/postgres).
