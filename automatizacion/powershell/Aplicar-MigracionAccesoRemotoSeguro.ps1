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
$RutaScriptSql = Join-Path $RutaRaiz 'infraestructura\base-datos\migraciones\migracion-acceso-remoto-seguro.sql'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaSecretoAdministrador = Join-Path $RutaRaiz 'configuracion\secretos\postgresql_contrasena.txt'
$RutaCompose = Join-Path $RutaRaiz 'infraestructura\contenedores\compose.desarrollo.yml'
$RutaVerificarInfraestructura = Join-Path $PSScriptRoot 'Verificar-Infraestructura.ps1'
$RutaConfigurarUsuario = Join-Path $PSScriptRoot 'Configurar-UsuarioAplicacion.ps1'
$RutaVerificadorMigracion = Join-Path $RutaRaiz 'infraestructura\base-datos\herramientas\verificar-migracion-acceso-remoto-seguro.sh'
$IdentificadorMigracion = '20260905061958_AgregarAccesoRemotoSeguro'
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

function Obtener-HashTextoNormalizado {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Ruta
    )

    $Contenido = [System.IO.File]::ReadAllText($Ruta)
    $Contenido = $Contenido.Replace("`r`n", "`n").Replace("`r", "`n")
    $Codificacion = New-Object System.Text.UTF8Encoding($false)
    $Bytes = $Codificacion.GetBytes($Contenido)
    $Algoritmo = [System.Security.Cryptography.SHA256]::Create()

    try {
        return ([BitConverter]::ToString($Algoritmo.ComputeHash($Bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $Algoritmo.Dispose()
    }
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
    $RutaVerificadorMigracion,
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
        Hash = '26638a5478bdf34d42e3729c67e486d7e8fe388912d10fe73156b73c69923206'
    },
    [PSCustomObject]@{
        Ruta = (Join-Path $RutaMigraciones "${IdentificadorMigracion}.cs")
        Hash = '54fcafc81f60f074b8394b3653989159423afa106a90103e1822dcecfac48073'
    },
    [PSCustomObject]@{
        Ruta = (Join-Path $RutaMigraciones "${IdentificadorMigracion}.Designer.cs")
        Hash = '8aa91e1afe88abbb02456d3b47de181a8f693f66c240a72f70d3d9d90382a14b'
    },
    [PSCustomObject]@{
        Ruta = (Join-Path $RutaMigraciones 'ContextoRedDiffModelSnapshot.cs')
        Hash = '4190e975dbe4a32b6b1b8530357f8821b0eb4f09b8a70a3ea86b36723173d08e'
    }
)

Write-Host 'Comprobando que la migración sea exactamente la versión revisada...' -ForegroundColor Cyan

foreach ($Archivo in $HashesEsperados) {
    $HashActual = Obtener-HashTextoNormalizado -Ruta $Archivo.Ruta

    if ($HashActual -ne $Archivo.Hash) {
        throw "El archivo cambió después de la revisión: $($Archivo.Ruta)"
    }
}

& $RutaVerificarInfraestructura

$ArgumentosCompose = @(
    'compose',
    '--env-file', $RutaEntorno,
    '--file', $RutaCompose
)

Write-Host 'Comprobando el estado previo del esquema y del historial...' -ForegroundColor Cyan
& docker @ArgumentosCompose exec -T base-datos `
    sh /opt/reddiff/herramientas/verificar-migracion-acceso-remoto-seguro.sh previa

if ($LASTEXITCODE -ne 0) {
    throw 'El esquema no cumple las precondiciones para aplicar la migración incremental.'
}

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
        throw 'No fue posible aplicar la migración de acceso remoto seguro.'
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

Write-Host 'Comprobando columnas, restricción, historial y privilegios...' -ForegroundColor Cyan
& docker @ArgumentosCompose exec -T base-datos `
    sh /opt/reddiff/herramientas/verificar-migracion-acceso-remoto-seguro.sh final

if ($LASTEXITCODE -ne 0) {
    throw 'La migración fue aplicada, pero el esquema no superó la comprobación final.'
}

Write-Host 'La migración de acceso remoto seguro fue aplicada y verificada correctamente.' -ForegroundColor Green
Write-Host 'PostgreSQL conserva 13 tablas y añadió únicamente las cinco columnas protegidas y su restricción.' -ForegroundColor Green
