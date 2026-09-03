[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaCompose = Join-Path $RutaRaiz 'infraestructura\contenedores\compose.desarrollo.yml'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker no está disponible en PATH.'
}

if (-not (Test-Path -LiteralPath $RutaEntorno -PathType Leaf)) {
    throw 'El entorno no está preparado. Ejecute Preparar-EntornoDesarrollo.ps1.'
}

$ArgumentosCompose = @(
    'compose',
    '--env-file', $RutaEntorno,
    '--file', $RutaCompose
)

& docker @ArgumentosCompose ps
if ($LASTEXITCODE -ne 0) {
    throw 'No fue posible consultar el estado de los servicios.'
}

$SalidaPuerto = & docker @ArgumentosCompose port base-datos 5432
if ($LASTEXITCODE -ne 0) {
    throw 'No fue posible consultar el puerto publicado de PostgreSQL.'
}

$PuertoPublicado = [string] ($SalidaPuerto | Select-Object -First 1)
$PuertoPublicado = $PuertoPublicado.Trim()

if ($PuertoPublicado -notmatch '^127\.0\.0\.1:\d+$') {
    throw 'PostgreSQL no está publicado exclusivamente en 127.0.0.1.'
}

Write-Host "Puerto local verificado: $PuertoPublicado" -ForegroundColor Cyan

& docker @ArgumentosCompose exec -T base-datos pg_isready `
    --host=127.0.0.1 `
    --port=5432

if ($LASTEXITCODE -ne 0) {
    throw 'PostgreSQL no respondió correctamente a pg_isready.'
}

& docker @ArgumentosCompose exec -T base-datos `
    sh /opt/reddiff/herramientas/verificar-conexion.sh

if ($LASTEXITCODE -ne 0) {
    throw 'La consulta de comprobación de PostgreSQL falló.'
}

Write-Host 'PostgreSQL está disponible y acepta consultas.' -ForegroundColor Green
