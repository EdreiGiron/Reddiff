# Herramientas de PostgreSQL

Scripts auxiliares que se montan en el contenedor para realizar comprobaciones sin trasladar contraseñas por la línea de comandos del equipo anfitrión.

Estos archivos no forman parte de la inicialización automática de PostgreSQL.

`configurar-usuario-aplicacion.sql` crea o actualiza de forma idempotente el usuario de ejecución de la API. Este usuario recibe acceso al esquema `reddiff` y permisos de lectura y escritura, pero no puede crear bases de datos, roles ni esquemas ni consultar o alterar el historial de migraciones.

`verificar-usuario-aplicacion.sql` comprueba la conexión y confirma que la cuenta no conserve privilegios administrativos, capacidad de creación ni acceso al historial de migraciones.

`verificar-migracion-acceso-remoto-seguro.sh` admite las fases `previa` y `final`. Antes de la actualización comprueba que solo exista la migración inicial aprobada o que la actualización ya esté completa. Después valida las cinco columnas, la restricción de integridad, las dos entradas del historial y los privilegios limitados de `reddiff_app`.
