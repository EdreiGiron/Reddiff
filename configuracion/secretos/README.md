# Secretos locales

El contenido real de esta carpeta está excluido de Git y del contexto de construcción de Docker.

`Preparar-EntornoDesarrollo.ps1` genera los siguientes archivos mediante un generador criptográfico:

- `postgresql_contrasena.txt`, utilizado exclusivamente para administrar PostgreSQL y ejecutar migraciones;
- `postgresql_aplicacion_contrasena.txt`, utilizado por la API con un usuario de privilegios limitados.

El script nunca muestra las contraseñas en la consola ni reemplaza secretos existentes.

Después de validar la migración inicial, las credenciales pueden renovarse de forma coordinada con:

```powershell
.\automatizacion\powershell\Rotar-CredencialesDesarrollo.ps1 -Confirmar
```

La operación cambia ambas identidades dentro de una transacción, recrea el contenedor y verifica las conexiones administrativa y de aplicación. Los archivos temporales `.nuevo` y `.anterior` permanecen ignorados y solo se conservan si hace falta recuperar una rotación incompleta.

Los secretos productivos deberán administrarse con el mecanismo seguro de la plataforma; no se copiarán desde esta carpeta.
