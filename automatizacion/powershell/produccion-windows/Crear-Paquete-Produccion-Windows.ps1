[CmdletBinding()]
param(
    [string] $RutaSalida,

    [switch] $OmitirPruebas
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$RutaCliente = Join-Path $RutaRaiz 'cliente'
$RutaProyectoApi = Join-Path $RutaRaiz 'servidor\fuente\RedDiff.Api\RedDiff.Api.csproj'
$RutaInstaladores = Join-Path $RutaRaiz 'infraestructura\windows\instalacion'
$CodificacionSinBom = [System.Text.UTF8Encoding]::new($false)

if ([string]::IsNullOrWhiteSpace($RutaSalida)) {
    $RutaSalida = Join-Path $RutaRaiz 'artifacts\windows\RedDiff-Windows-x64.zip'
}

$RutaSalida = [System.IO.Path]::GetFullPath($RutaSalida)
$DirectorioSalida = Split-Path -Parent $RutaSalida
$RutaTemporal = Join-Path ([System.IO.Path]::GetTempPath()) ("reddiff-windows-" + [Guid]::NewGuid().ToString('N'))
$RutaPaquete = Join-Path $RutaTemporal 'RedDiff-Windows-x64'
$RutaAplicacion = Join-Path $RutaPaquete 'aplicacion'

function Invocar-Comando {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Comando,

        [Parameter(Mandatory = $true)]
        [string[]] $Argumentos,

        [Parameter(Mandatory = $true)]
        [string] $Descripcion
    )

    Write-Host $Descripcion -ForegroundColor Cyan
    & $Comando @Argumentos
    if ($LASTEXITCODE -ne 0) {
        throw "$Descripcion falló con el código $LASTEXITCODE."
    }
}

foreach ($Comando in @('dotnet', 'node', 'npm')) {
    if (-not (Get-Command $Comando -ErrorAction SilentlyContinue)) {
        throw "No se encontró $Comando en PATH."
    }
}

foreach ($Ruta in @($RutaCliente, $RutaProyectoApi, $RutaInstaladores)) {
    if (-not (Test-Path -LiteralPath $Ruta)) {
        throw "No existe la ruta requerida: $Ruta"
    }
}

try {
    New-Item -ItemType Directory -Path $RutaAplicacion -Force | Out-Null

    if (-not $OmitirPruebas) {
        Invocar-Comando -Comando 'dotnet' -Argumentos @(
            'test', (Join-Path $RutaRaiz 'RedDiff.slnx'),
            '--configuration', 'Release',
            '--logger', 'console;verbosity=minimal'
        ) -Descripcion 'Ejecutando pruebas del servidor'

        Push-Location $RutaCliente
        try {
            Invocar-Comando -Comando 'npm' -Argumentos @('ci') `
                -Descripcion 'Instalando dependencias reproducibles del cliente'
            Invocar-Comando -Comando 'npm' -Argumentos @('test', '--', '--watch=false') `
                -Descripcion 'Ejecutando pruebas del cliente'
        }
        finally {
            Pop-Location
        }
    }
    else {
        Push-Location $RutaCliente
        try {
            Invocar-Comando -Comando 'npm' -Argumentos @('ci') `
                -Descripcion 'Instalando dependencias reproducibles del cliente'
        }
        finally {
            Pop-Location
        }
    }

    Push-Location $RutaCliente
    try {
        Invocar-Comando -Comando 'npm' -Argumentos @(
            'run', 'build', '--', '--configuration', 'production'
        ) -Descripcion 'Compilando Angular para producción'
    }
    finally {
        Pop-Location
    }

    Invocar-Comando -Comando 'dotnet' -Argumentos @(
        'publish', $RutaProyectoApi,
        '--configuration', 'Release',
        '--runtime', 'win-x64',
        '--self-contained', 'false',
        '--output', $RutaAplicacion,
        '/p:UseAppHost=true'
    ) -Descripcion 'Publicando la API para Windows x64'

    $RutaClienteCompilado = Join-Path $RutaCliente 'dist\reddiff-cliente\browser'
    if (-not (Test-Path -LiteralPath (Join-Path $RutaClienteCompilado 'index.html') -PathType Leaf)) {
        throw 'Angular no produjo dist\reddiff-cliente\browser\index.html.'
    }

    $RutaWwwroot = Join-Path $RutaAplicacion 'wwwroot'
    New-Item -ItemType Directory -Path $RutaWwwroot -Force | Out-Null
    Copy-Item -Path (Join-Path $RutaClienteCompilado '*') -Destination $RutaWwwroot -Recurse -Force
    Copy-Item -LiteralPath $RutaInstaladores -Destination (Join-Path $RutaPaquete 'instalacion') -Recurse -Force

    $Informacion = @"
RedDiff para Windows x64
Generado: $([DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))
Framework: .NET 10
Cliente: Angular 22

Lea instalacion\README.md antes de copiar el paquete al servidor.
"@
    [System.IO.File]::WriteAllText(
        (Join-Path $RutaPaquete 'LEAME.txt'),
        $Informacion,
        $CodificacionSinBom)

    New-Item -ItemType Directory -Path $DirectorioSalida -Force | Out-Null
    if (Test-Path -LiteralPath $RutaSalida) {
        Remove-Item -LiteralPath $RutaSalida -Force
    }

    Compress-Archive -Path $RutaPaquete -DestinationPath $RutaSalida -CompressionLevel Optimal
    Write-Host "Paquete creado: $RutaSalida" -ForegroundColor Green
}
finally {
    Remove-Item -LiteralPath $RutaTemporal -Recurse -Force -ErrorAction SilentlyContinue
}
