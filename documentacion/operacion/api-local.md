# API local y acceso a PostgreSQL

## Requisitos previos

- .NET SDK fijado por `global.json`;
- Docker Desktop iniciado con contenedores Linux;
- infraestructura local preparada según [base-datos-local.md](base-datos-local.md).

## Preparar la identidad de aplicación

Desde la raíz del repositorio:

```powershell
.\automatizacion\powershell\Configurar-UsuarioAplicacion.ps1
```

El procedimiento genera, si hace falta, una contraseña independiente, actualiza de forma idempotente el rol `reddiff_app` y comprueba una conexión real. No elimina el volumen ni muestra secretos.

La cuenta resultante puede conectarse y modificar datos de aplicación dentro del esquema `reddiff`, pero no puede crear roles, bases de datos ni esquemas ni acceder al historial de migraciones. Las migraciones utilizarán otra identidad.

## Restaurar herramientas y dependencias

```powershell
dotnet tool restore
dotnet restore .\RedDiff.slnx
dotnet build .\RedDiff.slnx --configuration Debug --no-restore
dotnet test .\RedDiff.slnx --configuration Debug --no-build
```

## Iniciar la API

La primera vez, cree el catálogo de roles y la cuenta administrativa:

```powershell
.\automatizacion\powershell\Inicializar-Administrador.ps1 -Confirmar
```

No se genera una contraseña predeterminada. El script la solicita de forma oculta y rechaza una segunda inicialización.

Después inicie el servicio:

```powershell
.\automatizacion\powershell\Iniciar-Api.ps1
```

El script conserva la contraseña fuera de `appsettings` y la retira del entorno del proceso de PowerShell cuando la API finaliza.

## Comprobar el servicio

En otra terminal:

```powershell
Invoke-RestMethod http://localhost:5088/salud/vivo
Invoke-RestMethod http://localhost:5088/salud/listo
```

- `/salud/vivo` comprueba que el proceso HTTP puede responder.
- `/salud/listo` comprueba que la API puede conectarse a PostgreSQL con la identidad de aplicación.

Ambas respuestas deben mostrar `estado` con valor `saludable`. La segunda responderá con código HTTP 503 cuando PostgreSQL no esté disponible o la configuración sea incorrecta.

Para detener la API, presione `Ctrl+C` en la terminal donde se está ejecutando.

El flujo de sesión y los endpoints administrativos se describen en [autenticación y autorización](../seguridad/autenticacion-y-autorizacion.md).
