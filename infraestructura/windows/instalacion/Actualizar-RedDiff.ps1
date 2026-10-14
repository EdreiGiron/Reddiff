[CmdletBinding()]
param(
    [string] $RutaDatos = (Join-Path $env:ProgramData 'RedDiff'),

    [switch] $Confirmar
)

. (Join-Path $PSScriptRoot 'Comun.ps1')

Confirmar-Administrador
if (-not $Confirmar) {
    throw 'La actualización reemplaza los binarios instalados. Ejecute nuevamente con -Confirmar.'
}

$Configuracion = Leer-ConfiguracionInstalacion -RutaDatos $RutaDatos
$RutaPaquete = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$RutaAplicacionPaquete = Join-Path $RutaPaquete 'aplicacion'
$RutaAplicacion = $Configuracion.RutaAplicacion
$RutaConfiguracionProduccion = Join-Path $RutaAplicacion 'appsettings.Production.json'
$RutaCopiaConfiguracion = Join-Path ([System.IO.Path]::GetTempPath()) ("reddiff-config-" + [Guid]::NewGuid().ToString('N') + '.json')
$RutaSecretoAdmin = Join-Path $Configuracion.RutaDatos 'secretos\postgresql_admin_contrasena.txt'
$VariablesAnteriores = @{}

if (-not (Test-Path -LiteralPath (Join-Path $RutaAplicacionPaquete 'RedDiff.Api.dll') -PathType Leaf)) {
    throw 'El paquete de actualización no contiene la aplicación publicada.'
}

if (-not (Test-Path -LiteralPath $RutaConfiguracionProduccion -PathType Leaf)) {
    throw 'No existe appsettings.Production.json en la instalación actual.'
}

function Establecer-VariableTemporal {
    param([string] $Nombre, [string] $Valor)
    $VariablesAnteriores[$Nombre] = [Environment]::GetEnvironmentVariable($Nombre, 'Process')
    [Environment]::SetEnvironmentVariable($Nombre, $Valor, 'Process')
}

& (Join-Path $PSScriptRoot 'Respaldar-RedDiff.ps1') -RutaDatos $RutaDatos

Import-Module WebAdministration -ErrorAction Stop
Copy-Item -LiteralPath $RutaConfiguracionProduccion -Destination $RutaCopiaConfiguracion -Force

try {
    Stop-Website -Name $NombreSitioRedDiff -ErrorAction SilentlyContinue
    Stop-WebAppPool -Name $NombrePoolRedDiff -ErrorAction SilentlyContinue

    Copy-Item -Path (Join-Path $RutaAplicacionPaquete '*') -Destination $RutaAplicacion -Recurse -Force
    Copy-Item -LiteralPath $RutaCopiaConfiguracion -Destination $RutaConfiguracionProduccion -Force

    Establecer-VariableTemporal -Nombre 'ASPNETCORE_ENVIRONMENT' -Valor 'Production'
    Establecer-VariableTemporal -Nombre 'BaseDatos__Servidor' -Valor '127.0.0.1'
    Establecer-VariableTemporal -Nombre 'BaseDatos__Puerto' -Valor ([string]$Configuracion.PuertoPostgreSql)
    Establecer-VariableTemporal -Nombre 'BaseDatos__VersionMayorServidor' -Valor ([string]$Configuracion.VersionPostgreSql)
    Establecer-VariableTemporal -Nombre 'BaseDatos__Nombre' -Valor $Configuracion.NombreBaseDatos
    Establecer-VariableTemporal -Nombre 'BaseDatos__Usuario' -Valor $Configuracion.UsuarioAdministradorBaseDatos
    Establecer-VariableTemporal -Nombre 'BaseDatos__ArchivoContrasena' -Valor $RutaSecretoAdmin
    Establecer-VariableTemporal -Nombre 'BaseDatos__ModoSsl' -Valor 'Disable'
    Establecer-VariableTemporal -Nombre 'BaseDatos__PermitirConexionLocalSinTls' -Valor 'true'
    Establecer-VariableTemporal `
        -Nombre 'Seguridad__OrigenCliente' `
        -Valor (Obtener-OrigenHttps `
            -NombreHost $Configuracion.NombreHost `
            -PuertoHttps ([int]$Configuracion.PuertoHttps))
    Establecer-VariableTemporal `
        -Nombre 'Seguridad__DirectorioClavesProteccion' `
        -Valor (Join-Path $Configuracion.RutaDatos 'claves-proteccion')
    Establecer-VariableTemporal -Nombre 'Eventos__Syslog__Habilitado' -Valor 'false'

    Push-Location $RutaAplicacion
    try {
        Invocar-ComandoNativo `
            -Comando (Join-Path $RutaAplicacion 'RedDiff.Api.exe') `
            -Argumentos @('--migrar-base-datos') `
            -Descripcion 'La migración de la actualización'
    }
    finally {
        Pop-Location
    }

    Establecer-AclRedDiff -Ruta $Configuracion.RutaInstalacion -Nivel Lectura
    Start-WebAppPool -Name $NombrePoolRedDiff
    Start-Website -Name $NombreSitioRedDiff

    & (Join-Path $PSScriptRoot 'Verificar-RedDiff.ps1') -RutaDatos $RutaDatos
    Write-Host 'La actualización de RedDiff finalizó correctamente.' -ForegroundColor Green
}
finally {
    foreach ($Nombre in $VariablesAnteriores.Keys) {
        [Environment]::SetEnvironmentVariable($Nombre, $VariablesAnteriores[$Nombre], 'Process')
    }

    Remove-Item -LiteralPath $RutaCopiaConfiguracion -Force -ErrorAction SilentlyContinue
}
