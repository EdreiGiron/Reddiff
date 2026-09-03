# Herramientas de PostgreSQL

Scripts auxiliares que se montan en el contenedor para realizar comprobaciones sin trasladar contraseñas por la línea de comandos del equipo anfitrión.

Estos archivos no forman parte de la inicialización automática de PostgreSQL.

`configurar-usuario-aplicacion.sql` crea o actualiza de forma idempotente el usuario de ejecución de la API. Este usuario recibe acceso al esquema `reddiff` y permisos de lectura y escritura, pero no puede crear bases de datos, roles ni esquemas.
