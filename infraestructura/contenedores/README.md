# Contenedores

## Composición de desarrollo

`compose.desarrollo.yml` incorpora inicialmente PostgreSQL. La API, el procesador y el cliente se agregarán cuando sus puntos de inicio, configuración y verificaciones de salud estén implementados.

Controles incluidos:

- PostgreSQL 18.6 fijado explícitamente;
- contraseña suministrada mediante un secreto montado como archivo;
- puerto publicado solamente en `127.0.0.1`;
- red de proyecto independiente y puerto accesible únicamente desde el equipo local;
- volumen persistente en `/var/lib/postgresql`, requerido por la imagen oficial desde PostgreSQL 18;
- verificación de salud con `pg_isready`;
- rotación local de logs para evitar crecimiento ilimitado.

Los comandos operativos están documentados en [../../documentacion/operacion/base-datos-local.md](../../documentacion/operacion/base-datos-local.md).

Las imágenes futuras deberán utilizar versiones fijadas, usuarios sin privilegios cuando sea posible, verificaciones de salud, contextos de construcción pequeños y secretos proporcionados fuera de Git.
