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

Para la instalación productiva con Docker Compose, `Preparar-Produccion.sh` crea un subdirectorio `produccion` con contraseñas y material criptográfico exclusivo del ambiente. Todo el subdirectorio permanece fuera de Git y del contexto de construcción.

La instalación nativa de Windows no utiliza esta carpeta. Conserva sus secretos en `C:\ProgramData\RedDiff\secretos` y aplica ACL para que el grupo de aplicaciones IIS solo pueda leer la contraseña limitada de la aplicación; la credencial administrativa permanece disponible únicamente para `SYSTEM` y administradores locales.

En una instalación definitiva, sus permisos, respaldo cifrado y acceso deben quedar bajo control de los operadores designados por LABYMED. La clave de la autoridad certificadora no se monta en los servicios y debe guardarse separadamente después de emitir los certificados.
