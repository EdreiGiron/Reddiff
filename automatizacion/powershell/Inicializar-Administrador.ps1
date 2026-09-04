[CmdletBinding()]
param(
    [ValidatePattern('^[\p{L}\p{Nd}._-]{3,100}$')]
    [string] $NombreUsuario = 'administrador',

    [switch] $Confirmar
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaProyecto = Join-Path $RutaRaiz 'servidor\fuente\RedDiff.Api\RedDiff.Api.csproj'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaSecretoAplicacion = Join-Path $RutaRaiz 'configuracion\secretos\postgresql_aplicacion_contrasena.txt'
$RutaVerificarInfraestructura = Join-Path $PSScriptRoot 'Verificar-Infraestructura.ps1'
$ValoresAnteriores = @{}
$Contrasena = $null
$Confirmacion = $null
$PunteroContrasena = [IntPtr]::Zero
$PunteroConfirmacion = [IntPtr]::Zero

function Obtener-ValorEntorno {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Nombre,

        [Parameter(Mandatory = $true)]
        [string] $ValorPredeterminado
    )

    $NombreEscapado = [Regex]::Escape($Nombre)
    $Linea = Get-Content -LiteralPath $RutaEntorno |
        Where-Object { $_ -match "^\s*$NombreEscapado\s*=" } |
        Select-Object -Last 1

    if ($null -eq $Linea) {
        return $ValorPredeterminado
    }

    $Valor = ($Linea -split '=', 2)[1].Trim()
    if ([string]::IsNullOrWhiteSpace($Valor)) {
        throw "La variable $Nombre no puede estar vacía."
    }

    return $Valor
}

function Establecer-VariableTemporal {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Nombre,

        [Parameter(Mandatory = $true)]
        [string] $Valor
    )

    $ValoresAnteriores[$Nombre] = [Environment]::GetEnvironmentVariable($Nombre, 'Process')
    [Environment]::SetEnvironmentVariable($Nombre, $Valor, 'Process')
}

if (-not $Confirmar) {
    throw 'La operación crea la primera cuenta. Ejecútela nuevamente con -Confirmar.'
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK no está disponible en PATH.'
}

$RutasRequeridas = @(
    $RutaProyecto,
    $RutaEntorno,
    $RutaSecretoAplicacion,
    $RutaVerificarInfraestructura
)

foreach ($Ruta in $RutasRequeridas) {
    if (-not (Test-Path -LiteralPath $Ruta -PathType Leaf)) {
        throw "No existe el archivo requerido: $Ruta"
    }
}

& $RutaVerificarInfraestructura

$SecretoAplicacion = [System.IO.File]::ReadAllText($RutaSecretoAplicacion).Trim()
if ([string]::IsNullOrWhiteSpace($SecretoAplicacion)) {
    throw 'El secreto del usuario de aplicación está vacío.'
}

try {
    $Segura = Read-Host 'Contraseña del primer administrador' -AsSecureString
    $SeguraConfirmacion = Read-Host 'Repita la contraseña' -AsSecureString
    $PunteroContrasena = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Segura)
    $PunteroConfirmacion = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SeguraConfirmacion)
    $Contrasena = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($PunteroContrasena)
    $Confirmacion = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($PunteroConfirmacion)

    if ($Contrasena -cne $Confirmacion) {
        throw 'Las contraseñas no coinciden.'
    }

    if ($Contrasena.Length -lt 12 -or $Contrasena.Length -gt 128) {
        throw 'La contraseña debe contener entre 12 y 128 caracteres.'
    }

    Establecer-VariableTemporal -Nombre 'DOTNET_ENVIRONMENT' -Valor 'Development'
    Establecer-VariableTemporal -Nombre 'ASPNETCORE_ENVIRONMENT' -Valor 'Development'
    Establecer-VariableTemporal -Nombre 'BaseDatos__Servidor' -Valor '127.0.0.1'
    Establecer-VariableTemporal `
        -Nombre 'BaseDatos__Puerto' `
        -Valor (Obtener-ValorEntorno -Nombre 'REDDIFF_BD_PUERTO' -ValorPredeterminado '5432')
    Establecer-VariableTemporal `
        -Nombre 'BaseDatos__Nombre' `
        -Valor (Obtener-ValorEntorno -Nombre 'REDDIFF_BD_NOMBRE' -ValorPredeterminado 'reddiff')
    Establecer-VariableTemporal `
        -Nombre 'BaseDatos__Usuario' `
        -Valor (Obtener-ValorEntorno -Nombre 'REDDIFF_BD_USUARIO_APLICACION' -ValorPredeterminado 'reddiff_app')
    Establecer-VariableTemporal -Nombre 'BaseDatos__Contrasena' -Valor $SecretoAplicacion
    Establecer-VariableTemporal -Nombre 'BaseDatos__ModoSsl' -Valor 'Disable'
    Establecer-VariableTemporal `
        -Nombre 'Seguridad__AdministradorInicial__NombreUsuario' `
        -Valor $NombreUsuario
    Establecer-VariableTemporal `
        -Nombre 'Seguridad__AdministradorInicial__Contrasena' `
        -Valor $Contrasena

    Write-Host 'Inicializando el catálogo de roles y la primera cuenta...' -ForegroundColor Cyan
    & dotnet run `
        --project $RutaProyecto `
        --configuration Debug `
        --no-launch-profile `
        -- `
        --inicializar-administrador

    if ($LASTEXITCODE -ne 0) {
        throw 'No fue posible inicializar la identidad.'
    }

    Write-Host 'La identidad fue inicializada correctamente.' -ForegroundColor Green
}
finally {
    foreach ($Nombre in $ValoresAnteriores.Keys) {
        [Environment]::SetEnvironmentVariable(
            $Nombre,
            $ValoresAnteriores[$Nombre],
            'Process')
    }

    if ($PunteroContrasena -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($PunteroContrasena)
    }

    if ($PunteroConfirmacion -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($PunteroConfirmacion)
    }

    $Contrasena = $null
    $Confirmacion = $null
    $SecretoAplicacion = $null
}
