# Secretos locales

El contenido real de esta carpeta está excluido de Git y del contexto de construcción de Docker.

`Preparar-EntornoDesarrollo.ps1` genera los siguientes archivos mediante un generador criptográfico:

- `postgresql_contrasena.txt`, utilizado exclusivamente para administrar PostgreSQL y ejecutar migraciones;
- `postgresql_aplicacion_contrasena.txt`, utilizado por la API con un usuario de privilegios limitados.

El script nunca muestra las contraseñas en la consola ni reemplaza secretos existentes.

Los secretos productivos deberán administrarse con el mecanismo seguro de la plataforma; no se copiarán desde esta carpeta.
