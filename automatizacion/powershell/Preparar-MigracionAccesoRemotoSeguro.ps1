[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$NombreMigracion = 'AgregarAccesoRemotoSeguro'
$IdentificadorMigracionAnterior = '20260904032054_Inicial'
$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaProyectoInfraestructura = Join-Path $RutaRaiz 'servidor\fuente\RedDiff.Infraestructura\RedDiff.Infraestructura.csproj'
$RutaProyectoApi = Join-Path $RutaRaiz 'servidor\fuente\RedDiff.Api\RedDiff.Api.csproj'
$RutaMigraciones = Join-Path $RutaRaiz 'servidor\fuente\RedDiff.Infraestructura\Persistencia\Migraciones'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaSecretoAdministrador = Join-Path $RutaRaiz 'configuracion\secretos\postgresql_contrasena.txt'
$RutaDirectorioSql = Join-Path $RutaRaiz 'infraestructura\base-datos\migraciones'
$RutaScriptSql = Join-Path $RutaDirectorioSql 'migracion-acceso-remoto-seguro.sql'
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

$RutasRequeridas = @(
    $RutaProyectoInfraestructura,
    $RutaProyectoApi,
    $RutaEntorno,
    $RutaSecretoAdministrador,
    (Join-Path $RutaMigraciones "${IdentificadorMigracionAnterior}.cs"),
    (Join-Path $RutaMigraciones "${IdentificadorMigracionAnterior}.Designer.cs"),
    (Join-Path $RutaMigraciones 'ContextoRedDiffModelSnapshot.cs')
)

foreach ($Ruta in $RutasRequeridas) {
    if (-not (Test-Path -LiteralPath $Ruta -PathType Leaf)) {
        throw "No existe el archivo requerido: $Ruta"
    }
}

$MigracionesExistentes = @(
    Get-ChildItem -LiteralPath $RutaMigraciones -Filter "*_${NombreMigracion}.cs" -File
)

if ($MigracionesExistentes.Count -ne 0) {
    throw "Ya existe una migración con el nombre $NombreMigracion."
}

if (Test-Path -LiteralPath $RutaScriptSql) {
    throw "Ya existe el script SQL: $RutaScriptSql"
}

$Contrasena = [System.IO.File]::ReadAllText($RutaSecretoAdministrador).Trim()
if ([string]::IsNullOrWhiteSpace($Contrasena)) {
    throw 'El secreto administrativo de PostgreSQL está vacío.'
}

New-Item -ItemType Directory -Path $RutaDirectorioSql -Force | Out-Null

try {
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
        -Valor (Obtener-ValorEntorno -Nombre 'REDDIFF_BD_USUARIO_ADMIN' -ValorPredeterminado 'reddiff_admin')
    Establecer-VariableTemporal -Nombre 'BaseDatos__Contrasena' -Valor $Contrasena
    Establecer-VariableTemporal -Nombre 'BaseDatos__ModoSsl' -Valor 'Disable'

    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw 'No fue posible restaurar las herramientas locales de .NET.'
    }

    Write-Host "Generando la migración $NombreMigracion..." -ForegroundColor Cyan

    $ArgumentosMigracion = @(
        'ef',
        'migrations',
        'add', $NombreMigracion,
        '--project', $RutaProyectoInfraestructura,
        '--startup-project', $RutaProyectoApi,
        '--context', 'ContextoRedDiff',
        '--output-dir', 'Persistencia\Migraciones'
    )

    & dotnet @ArgumentosMigracion
    if ($LASTEXITCODE -ne 0) {
        throw 'No fue posible generar la migración de acceso remoto seguro.'
    }

    $MigracionesGeneradas = @(
        Get-ChildItem -LiteralPath $RutaMigraciones -Filter "*_${NombreMigracion}.cs" -File
    )

    if ($MigracionesGeneradas.Count -ne 1) {
        throw 'No fue posible identificar de forma unívoca la migración generada.'
    }

    $RutaMigracion = $MigracionesGeneradas[0].FullName
    $IdentificadorMigracion = [System.IO.Path]::GetFileNameWithoutExtension($RutaMigracion)
    $RutaDesigner = Join-Path $RutaMigraciones "${IdentificadorMigracion}.Designer.cs"

    if (-not (Test-Path -LiteralPath $RutaDesigner -PathType Leaf)) {
        throw 'La migración fue creada sin su archivo de diseño.'
    }

    Write-Host 'Generando el SQL incremental e idempotente para revisión...' -ForegroundColor Cyan

    $ArgumentosScript = @(
        'ef',
        'migrations',
        'script', $IdentificadorMigracionAnterior, $IdentificadorMigracion,
        '--idempotent',
        '--output', $RutaScriptSql,
        '--project', $RutaProyectoInfraestructura,
        '--startup-project', $RutaProyectoApi,
        '--context', 'ContextoRedDiff'
    )

    & dotnet @ArgumentosScript
    if ($LASTEXITCODE -ne 0) {
        throw 'La migración fue creada, pero no pudo generarse el script SQL incremental.'
    }

    Write-Host 'Comprobando que el modelo y la migración coincidan...' -ForegroundColor Cyan

    $ArgumentosComprobacion = @(
        'ef',
        'migrations',
        'has-pending-model-changes',
        '--project', $RutaProyectoInfraestructura,
        '--startup-project', $RutaProyectoApi,
        '--context', 'ContextoRedDiff'
    )

    & dotnet @ArgumentosComprobacion
    if ($LASTEXITCODE -ne 0) {
        throw 'El modelo contiene cambios que no quedaron representados en la migración.'
    }

    Write-Host 'La migración de acceso remoto y su SQL incremental fueron generados.' -ForegroundColor Green
    Write-Host "Migración: $IdentificadorMigracion"
    Write-Host "Script SQL: $RutaScriptSql"
    Write-Host 'La base de datos todavía no fue modificada.' -ForegroundColor Yellow
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
