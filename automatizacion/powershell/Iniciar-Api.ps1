[CmdletBinding()]
param(
    [switch] $SinRestaurar
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaProyecto = Join-Path $RutaRaiz 'servidor\fuente\RedDiff.Api\RedDiff.Api.csproj'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaSecreto = Join-Path $RutaRaiz 'configuracion\secretos\postgresql_aplicacion_contrasena.txt'
$ValoresAnteriores = @{}

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

    $Partes = $Linea -split '=', 2
    $Valor = $Partes[1].Trim()

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

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK no está disponible en PATH.'
}

if (-not (Test-Path -LiteralPath $RutaEntorno -PathType Leaf)) {
    throw 'No existe configuracion\entornos\desarrollo.env.'
}

if (-not (Test-Path -LiteralPath $RutaSecreto -PathType Leaf)) {
    throw 'No existe el secreto del usuario de aplicación. Ejecute Configurar-UsuarioAplicacion.ps1.'
}

$Contrasena = [System.IO.File]::ReadAllText($RutaSecreto).Trim()
if ([string]::IsNullOrWhiteSpace($Contrasena)) {
    throw 'El secreto del usuario de aplicación está vacío.'
}

try {
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
    Establecer-VariableTemporal -Nombre 'BaseDatos__Contrasena' -Valor $Contrasena
    Establecer-VariableTemporal -Nombre 'BaseDatos__ModoSsl' -Valor 'Disable'

    $ArgumentosDotnet = @(
        'run',
        '--project', $RutaProyecto,
        '--launch-profile', 'http'
    )

    if ($SinRestaurar) {
        $ArgumentosDotnet += '--no-restore'
    }

    Write-Host 'Iniciando la API en http://localhost:5088...' -ForegroundColor Cyan
    & dotnet @ArgumentosDotnet

    if ($LASTEXITCODE -ne 0) {
        throw 'La API finalizó con un error.'
    }
}
finally {
    foreach ($Nombre in $ValoresAnteriores.Keys) {
        [Environment]::SetEnvironmentVariable(
            $Nombre,
            $ValoresAnteriores[$Nombre],
            'Process')
    }

    $Contrasena = $null
}
