[CmdletBinding()]
param(
    [switch] $Confirmar
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaSolucion = Join-Path $RutaRaiz 'RedDiff.slnx'
$RutaProyectoInfraestructura = Join-Path $RutaRaiz 'servidor\fuente\RedDiff.Infraestructura\RedDiff.Infraestructura.csproj'
$RutaProyectoApi = Join-Path $RutaRaiz 'servidor\fuente\RedDiff.Api\RedDiff.Api.csproj'
$RutaMigraciones = Join-Path $RutaRaiz 'servidor\fuente\RedDiff.Infraestructura\Persistencia\Migraciones'
$RutaScriptSql = Join-Path $RutaRaiz 'infraestructura\base-datos\migraciones\migracion-inicial.sql'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaSecretoAdministrador = Join-Path $RutaRaiz 'configuracion\secretos\postgresql_contrasena.txt'
$RutaCompose = Join-Path $RutaRaiz 'infraestructura\contenedores\compose.desarrollo.yml'
$RutaVerificarInfraestructura = Join-Path $PSScriptRoot 'Verificar-Infraestructura.ps1'
$RutaConfigurarUsuario = Join-Path $PSScriptRoot 'Configurar-UsuarioAplicacion.ps1'
$IdentificadorMigracion = '20260904032054_Inicial'
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

if (-not $Confirmar) {
    throw 'La operación modifica PostgreSQL. Ejecútela nuevamente con el parámetro -Confirmar.'
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK no está disponible en PATH.'
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker no está disponible en PATH.'
}

$RutasRequeridas = @(
    $RutaSolucion,
    $RutaProyectoInfraestructura,
    $RutaProyectoApi,
    $RutaEntorno,
    $RutaSecretoAdministrador,
    $RutaCompose,
    $RutaVerificarInfraestructura,
    $RutaConfigurarUsuario,
    $RutaScriptSql,
    (Join-Path $RutaMigraciones "${IdentificadorMigracion}.cs"),
    (Join-Path $RutaMigraciones "${IdentificadorMigracion}.Designer.cs"),
    (Join-Path $RutaMigraciones 'ContextoRedDiffModelSnapshot.cs')
)

foreach ($Ruta in $RutasRequeridas) {
    if (-not (Test-Path -LiteralPath $Ruta -PathType Leaf)) {
        throw "No existe el archivo requerido: $Ruta"
    }
}

$HashesEsperados = @(
    [PSCustomObject]@{
        Ruta = $RutaScriptSql
        Hash = '978c8b1c8ebae40d6f666d6120eb4e66df21c3f9480da28b3657a48d0f00a7f7'
    },
    [PSCustomObject]@{
        Ruta = (Join-Path $RutaMigraciones "${IdentificadorMigracion}.cs")
        Hash = '5027af30f348dc0342e228510148ecfc2b4d52ef7a6908989f1bb803c82bf1ef'
    },
    [PSCustomObject]@{
        Ruta = (Join-Path $RutaMigraciones "${IdentificadorMigracion}.Designer.cs")
        Hash = 'f194b191d4639e551a723dd96d6ec5d0d4890e88392f7cf47297e97f84e37784'
    },
    [PSCustomObject]@{
        Ruta = (Join-Path $RutaMigraciones 'ContextoRedDiffModelSnapshot.cs')
        Hash = 'b11620bf9ccefc55f6043e7454c91a4a61ad25bca720b5a9746e9987320cecd9'
    }
)

Write-Host 'Comprobando que la migración sea exactamente la versión revisada...' -ForegroundColor Cyan

foreach ($Archivo in $HashesEsperados) {
    $HashActual = (Get-FileHash -LiteralPath $Archivo.Ruta -Algorithm SHA256).Hash.ToLowerInvariant()

    if ($HashActual -ne $Archivo.Hash) {
        throw "El archivo cambió después de la revisión: $($Archivo.Ruta)"
    }
}

& $RutaVerificarInfraestructura

Write-Host 'Compilando la solución antes de modificar PostgreSQL...' -ForegroundColor Cyan
& dotnet build $RutaSolucion --configuration Debug
if ($LASTEXITCODE -ne 0) {
    throw 'La compilación falló. PostgreSQL no fue modificado.'
}

& dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw 'No fue posible restaurar las herramientas locales de .NET.'
}

$Contrasena = [System.IO.File]::ReadAllText($RutaSecretoAdministrador).Trim()
if ([string]::IsNullOrWhiteSpace($Contrasena)) {
    throw 'El secreto administrativo de PostgreSQL está vacío.'
}

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

    Write-Host "Aplicando exclusivamente la migración $IdentificadorMigracion..." -ForegroundColor Cyan

    $ArgumentosActualizacion = @(
        'ef',
        'database',
        'update', $IdentificadorMigracion,
        '--project', $RutaProyectoInfraestructura,
        '--startup-project', $RutaProyectoApi,
        '--context', 'ContextoRedDiff',
        '--configuration', 'Debug',
        '--no-build'
    )

    & dotnet @ArgumentosActualizacion
    if ($LASTEXITCODE -ne 0) {
        throw 'No fue posible aplicar la migración inicial.'
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

Write-Host 'Restableciendo y comprobando los privilegios de reddiff_app...' -ForegroundColor Cyan
& $RutaConfigurarUsuario

$ArgumentosCompose = @(
    'compose',
    '--env-file', $RutaEntorno,
    '--file', $RutaCompose
)

Write-Host 'Comprobando tablas, relaciones, restricciones e índices...' -ForegroundColor Cyan
& docker @ArgumentosCompose exec -T base-datos `
    sh /opt/reddiff/herramientas/verificar-migracion-inicial.sh

if ($LASTEXITCODE -ne 0) {
    throw 'La migración fue aplicada, pero el esquema no superó la comprobación final.'
}

Write-Host 'La migración inicial fue aplicada y verificada correctamente.' -ForegroundColor Green
Write-Host 'PostgreSQL contiene las 13 tablas del modelo y reddiff_app conserva privilegios limitados.' -ForegroundColor Green
