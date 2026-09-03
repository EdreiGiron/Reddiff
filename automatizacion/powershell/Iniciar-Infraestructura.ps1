[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaCompose = Join-Path $RutaRaiz 'infraestructura\contenedores\compose.desarrollo.yml'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaPreparacion = Join-Path $PSScriptRoot 'Preparar-EntornoDesarrollo.ps1'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker no está disponible en PATH.'
}

& $RutaPreparacion

& docker info *> $null
if ($LASTEXITCODE -ne 0) {
    throw 'Docker Desktop no está iniciado o el motor Linux no está disponible.'
}

$ArgumentosCompose = @(
    'compose',
    '--env-file', $RutaEntorno,
    '--file', $RutaCompose
)

& docker @ArgumentosCompose config --quiet
if ($LASTEXITCODE -ne 0) {
    throw 'La configuración de Docker Compose no es válida.'
}

Write-Host 'Iniciando la infraestructura local...' -ForegroundColor Cyan
& docker @ArgumentosCompose up --detach --wait --wait-timeout 90
if ($LASTEXITCODE -ne 0) {
    throw 'No fue posible iniciar la infraestructura local.'
}

& docker @ArgumentosCompose ps
if ($LASTEXITCODE -ne 0) {
    throw 'No fue posible consultar el estado de los servicios.'
}

Write-Host 'La infraestructura está iniciada y saludable.' -ForegroundColor Green
