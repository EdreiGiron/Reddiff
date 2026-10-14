[CmdletBinding()]
param(
    [string] $NombreUsuario = 'administrador.principal',

    [string] $RutaDatos = (Join-Path $env:ProgramData 'RedDiff')
)

. (Join-Path $PSScriptRoot 'Comun.ps1')

Confirmar-Administrador
$Configuracion = Leer-ConfiguracionInstalacion -RutaDatos $RutaDatos
$RutaEjecutable = Join-Path $Configuracion.RutaAplicacion 'RedDiff.Api.exe'
$RutaSecreto = Join-Path $Configuracion.RutaDatos 'secretos\administrador_inicial_contrasena.txt'
$Contrasena = $null
$Confirmacion = $null
$VariablesAnteriores = @{}

if (-not (Test-Path -LiteralPath $RutaEjecutable -PathType Leaf)) {
    throw "No existe la aplicación publicada: $RutaEjecutable"
}

function Establecer-VariableTemporal {
    param([string] $Nombre, [string] $Valor)
    $VariablesAnteriores[$Nombre] = [Environment]::GetEnvironmentVariable($Nombre, 'Process')
    [Environment]::SetEnvironmentVariable($Nombre, $Valor, 'Process')
}

try {
    $Contrasena = Convertir-SecureStringATexto -Valor (
        Read-Host 'Contraseña del primer administrador' -AsSecureString)
    $Confirmacion = Convertir-SecureStringATexto -Valor (
        Read-Host 'Repita la contraseña' -AsSecureString)

    if ($Contrasena -cne $Confirmacion) {
        throw 'Las contraseñas no coinciden.'
    }

    if ($Contrasena.Length -lt 12 -or $Contrasena.Length -gt 128) {
        throw 'La contraseña debe contener entre 12 y 128 caracteres.'
    }

    Escribir-TextoSinBom -Ruta $RutaSecreto -Contenido $Contrasena
    Establecer-VariableTemporal -Nombre 'ASPNETCORE_ENVIRONMENT' -Valor 'Production'
    Establecer-VariableTemporal `
        -Nombre 'Seguridad__AdministradorInicial__NombreUsuario' `
        -Valor $NombreUsuario
    Establecer-VariableTemporal `
        -Nombre 'Seguridad__AdministradorInicial__ArchivoContrasena' `
        -Valor $RutaSecreto
    Establecer-VariableTemporal -Nombre 'Eventos__Syslog__Habilitado' -Valor 'false'

    Push-Location $Configuracion.RutaAplicacion
    try {
        Invocar-ComandoNativo `
            -Comando $RutaEjecutable `
            -Argumentos @('--inicializar-administrador') `
            -Descripcion 'La inicialización del administrador'
    }
    finally {
        Pop-Location
    }

    Write-Host "El usuario $NombreUsuario quedó inicializado." -ForegroundColor Green
}
finally {
    if (Test-Path -LiteralPath $RutaSecreto -PathType Leaf) {
        Escribir-TextoSinBom -Ruta $RutaSecreto -Contenido ''
    }

    foreach ($Nombre in $VariablesAnteriores.Keys) {
        [Environment]::SetEnvironmentVariable($Nombre, $VariablesAnteriores[$Nombre], 'Process')
    }

    $Contrasena = $null
    $Confirmacion = $null
}
