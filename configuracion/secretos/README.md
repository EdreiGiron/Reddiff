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

Para Docker Compose en Windows 11, `automatizacion/powershell/produccion-docker/Preparar-Produccion.ps1` crea el subdirectorio `produccion`, genera contrasenas y utiliza un contenedor auxiliar para emitir el material criptografico. La variante Linux ofrece el flujo equivalente mediante `Preparar-Produccion.sh`. Todo el subdirectorio permanece fuera de Git y del contexto de construccion.

La instalacion nativa alternativa de Windows no utiliza esta carpeta. Conserva sus secretos en `C:\ProgramData\RedDiff\secretos`.

En una instalación definitiva, sus permisos, respaldo cifrado y acceso deben quedar bajo control de los operadores designados por LABYMED. La clave de la autoridad certificadora no se monta en los servicios y debe guardarse separadamente después de emitir los certificados.
