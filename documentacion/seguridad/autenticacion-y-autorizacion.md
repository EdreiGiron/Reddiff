# Autenticación, autorización y administración de cuentas

## Alcance

Este bloque cubre RF-01, RF-02, RNF-01, RNF-02 y RNF-04. La API autentica cuentas locales, aplica los roles funcionales `Administrador` y `Tecnico`, permite administrar usuarios y registra los accesos y cambios administrativos en auditoría.

No existe una contraseña predeterminada ni un endpoint HTTP para crear la primera cuenta. La inicialización es una operación local, explícita y de un solo uso.

## Controles aplicados

- las contraseñas se transforman con PBKDF2-HMAC-SHA-256, sal aleatoria y 600 000 iteraciones;
- la sesión se almacena en una cookie cifrada, `HttpOnly`, `SameSite=Strict` y no persistente;
- la cookie siempre es segura fuera de desarrollo y vence sin renovación deslizante;
- cada solicitud protegida comprueba en PostgreSQL que la cuenta, el rol y las declaraciones de la sesión continúen vigentes;
- los cambios de estado, rol o contraseña invalidan el acceso anterior en la siguiente solicitud;
- las operaciones HTTP que modifican datos requieren el encabezado antifalsificación `X-CSRF-TOKEN`;
- el inicio de sesión admite diez intentos por minuto y dirección remota;
- CORS solo acepta el origen configurado y permite credenciales únicamente para ese origen;
- los errores de inicio de sesión no distinguen cuentas inexistentes, inactivas o contraseñas incorrectas;
- las respuestas de usuarios nunca incluyen el hash ni la contraseña;
- un administrador no puede desactivar su propia cuenta ni retirarse su propio rol.

## Inicializar la identidad

Con PostgreSQL iniciado y la migración aplicada:

```powershell
.\automatizacion\powershell\Inicializar-Administrador.ps1 -Confirmar
```

El script solicita dos veces una contraseña de 12 a 128 caracteres sin mostrarla. Puede indicarse otro nombre válido:

```powershell
.\automatizacion\powershell\Inicializar-Administrador.ps1 `
    -NombreUsuario administrador.principal `
    -Confirmar
```

La operación crea los dos roles funcionales y la primera cuenta solo cuando la tabla `usuario` está vacía. Las cuentas posteriores se crean mediante la API.

## Flujo HTTP

El cliente debe conservar las cookies y obtener un token antifalsificación antes de cada operación que cambie datos. Ejemplo de inicio de sesión local:

```powershell
$RespuestaCsrf = Invoke-WebRequest `
    http://localhost:5088/api/autenticacion/proteccion-csrf `
    -SessionVariable Sesion

$Csrf = ($RespuestaCsrf.Content | ConvertFrom-Json).token
$Credencial = Get-Credential -Message 'Credenciales del sistema'
$Puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR(
    $Credencial.Password)

try {
    $ContrasenaTemporal = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($Puntero)
    $Credenciales = @{
        nombreUsuario = $Credencial.UserName
        contrasena = $ContrasenaTemporal
    } | ConvertTo-Json

    Invoke-RestMethod `
        http://localhost:5088/api/autenticacion/iniciar-sesion `
        -Method Post `
        -WebSession $Sesion `
        -Headers @{ 'X-CSRF-TOKEN' = $Csrf } `
        -ContentType 'application/json' `
        -Body $Credenciales
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($Puntero)
    $ContrasenaTemporal = $null
    $Credenciales = $null
}
```

El ejemplo mantiene la contraseña únicamente en la sesión actual de PowerShell. No debe guardarse en scripts, historial, archivos de configuración ni documentación.

## Endpoints

| Método y ruta | Acceso | Finalidad |
| --- | --- | --- |
| `GET /api/autenticacion/proteccion-csrf` | Anónimo | Entregar el token antifalsificación y su cookie asociada. |
| `POST /api/autenticacion/iniciar-sesion` | Anónimo + CSRF | Validar credenciales y crear la sesión. |
| `GET /api/autenticacion/sesion` | Autenticado | Consultar la identidad y el rol vigentes. |
| `POST /api/autenticacion/cerrar-sesion` | Autenticado + CSRF | Auditar y finalizar la sesión. |
| `GET /api/roles` | Administrador | Listar el catálogo funcional de roles. |
| `GET /api/usuarios` | Administrador | Listar cuentas sin datos de contraseña. |
| `GET /api/usuarios/{id}` | Administrador | Consultar una cuenta. |
| `POST /api/usuarios` | Administrador + CSRF | Crear una cuenta activa. |
| `PUT /api/usuarios/{id}` | Administrador + CSRF | Cambiar nombre, rol y, opcionalmente, contraseña. |
| `PATCH /api/usuarios/{id}/estado` | Administrador + CSRF | Activar o desactivar una cuenta. |

La gestión de permisos arbitrarios queda fuera del alcance: los permisos se expresan mediante los dos roles validados en los casos de uso.
