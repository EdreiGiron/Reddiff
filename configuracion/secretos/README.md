# Secretos locales

El contenido real de esta carpeta está excluido de Git y del contexto de construcción de Docker.

`Preparar-EntornoDesarrollo.ps1` genera `postgresql_contrasena.txt` mediante un generador criptográfico. El script nunca muestra la contraseña en la consola ni reemplaza un secreto existente.

Los secretos productivos deberán administrarse con el mecanismo seguro de la plataforma; no se copiarán desde esta carpeta.
