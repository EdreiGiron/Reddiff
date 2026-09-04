# Base de datos local con Docker

## Alcance

Este procedimiento inicia PostgreSQL 18.6 para desarrollo. El servicio solamente publica el puerto en la interfaz local y conserva sus datos en un volumen administrado por Docker.

La cuenta configurada aquí es administrativa y se utilizará únicamente para preparar la base y aplicar migraciones. La aplicación utiliza un usuario distinto con privilegios limitados.

## Primera preparación

Desde la raíz del repositorio:

```powershell
.\automatizacion\powershell\Preparar-EntornoDesarrollo.ps1
```

El script crea, sin sobrescribir archivos existentes:

- `configuracion/entornos/desarrollo.env`, con opciones no sensibles;
- `configuracion/secretos/postgresql_contrasena.txt`, con la contraseña administrativa;
- `configuracion/secretos/postgresql_aplicacion_contrasena.txt`, con una contraseña independiente para la API.

Ambos valores locales permanecen fuera de Git. El archivo de contraseña también queda fuera del contexto de construcción de Docker.

## Inicio y verificación

```powershell
.\automatizacion\powershell\Iniciar-Infraestructura.ps1
.\automatizacion\powershell\Configurar-UsuarioAplicacion.ps1
.\automatizacion\powershell\Verificar-Infraestructura.ps1
```

El resultado esperado es un servicio `base-datos` con estado saludable y una consulta que muestre:

- puerto local `127.0.0.1:5432`;
- base de datos `reddiff`;
- usuario `reddiff_admin`;
- versión `18.6` de PostgreSQL.

La conexión desde una herramienta instalada en Windows utiliza:

| Opción | Valor predeterminado |
| --- | --- |
| Servidor | `127.0.0.1` |
| Puerto | `5432` |
| Base de datos | `reddiff` |
| Usuario administrativo | `reddiff_admin` |
| Contraseña | Contenido local de `configuracion/secretos/postgresql_contrasena.txt` |
| Usuario de aplicación | `reddiff_app` |
| Contraseña de aplicación | Contenido local de `configuracion/secretos/postgresql_aplicacion_contrasena.txt` |

## Preparación de la migración inicial

Después de compilar y probar el modelo, la migración inicial y su script SQL de revisión se generan mediante:

```powershell
.\automatizacion\powershell\Preparar-MigracionInicial.ps1
```

El script utiliza temporalmente la identidad administrativa, genera los archivos de Entity Framework Core dentro de `RedDiff.Infraestructura/Persistencia/Migraciones` y escribe un SQL idempotente en `infraestructura/base-datos/migraciones/migracion-inicial.sql`.

Esta operación no ejecuta la migración. El SQL debe revisarse antes de autorizar cualquier cambio sobre PostgreSQL.

Después de la revisión, la migración aprobada se aplica y verifica con:

```powershell
.\automatizacion\powershell\Aplicar-MigracionInicial.ps1 -Confirmar
```

## Rotación de credenciales locales

Después de comprobar el esquema puede reemplazar las dos credenciales generadas durante la preparación:

```powershell
.\automatizacion\powershell\Rotar-CredencialesDesarrollo.ps1 -Confirmar
```

La operación actualiza PostgreSQL en una transacción, reemplaza los archivos locales, recrea el contenedor y comprueba las identidades administrativa y de aplicación. Si PostgreSQL ya cambió pero una comprobación posterior falla, los archivos `.nuevo` y `.anterior` se conservan para recuperación y no deben eliminarse hasta resolver el incidente.

## Detención sin pérdida de datos

```powershell
.\automatizacion\powershell\Detener-Infraestructura.ps1
```

El contenedor y la red se eliminan, pero el volumen se conserva. Al iniciar nuevamente, la información seguirá disponible.

## Reinicio completo

La siguiente operación elimina permanentemente la base de datos local. Debe utilizarse únicamente cuando se desea comenzar desde cero:

```powershell
.\automatizacion\powershell\Detener-Infraestructura.ps1 -EliminarDatos
```

Antes de ejecutarla, confirme que no necesita conservar información de prueba.

## Diagnóstico básico

Si el inicio falla:

```powershell
docker compose `
    --env-file .\configuracion\entornos\desarrollo.env `
    --file .\infraestructura\contenedores\compose.desarrollo.yml `
    ps

docker compose `
    --env-file .\configuracion\entornos\desarrollo.env `
    --file .\infraestructura\contenedores\compose.desarrollo.yml `
    logs base-datos

docker compose `
    --env-file .\configuracion\entornos\desarrollo.env `
    --file .\infraestructura\contenedores\compose.desarrollo.yml `
    port base-datos 5432
```

No comparta los logs sin revisar previamente que no incluyan información sensible.

## Referencias técnicas

- [Versiones compatibles de PostgreSQL](https://www.postgresql.org/support/versioning/).
- [Imagen oficial de PostgreSQL para Docker](https://hub.docker.com/_/postgres).
- [Orden de inicio y verificaciones de salud en Docker Compose](https://docs.docker.com/compose/how-tos/startup-order/).
