[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [switch] $EliminarDatos
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaCompose = Join-Path $RutaRaiz 'infraestructura\contenedores\compose.desarrollo.yml'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker no está disponible en PATH.'
}

if (-not (Test-Path -LiteralPath $RutaEntorno -PathType Leaf)) {
    throw 'No existe configuracion\entornos\desarrollo.env.'
}

$ArgumentosCompose = @(
    'compose',
    '--env-file', $RutaEntorno,
    '--file', $RutaCompose
)

if ($EliminarDatos) {
    $Objetivo = 'contenedores, red y volumen local de PostgreSQL'
    $Accion = 'Detener y eliminar permanentemente los datos locales'
    $ArgumentosDown = @('down', '--remove-orphans', '--volumes')
}
else {
    $Objetivo = 'contenedores y red de desarrollo'
    $Accion = 'Detener conservando el volumen de PostgreSQL'
    $ArgumentosDown = @('down', '--remove-orphans')
}

if (-not $PSCmdlet.ShouldProcess($Objetivo, $Accion)) {
    return
}

$ArgumentosDocker = $ArgumentosCompose + $ArgumentosDown
& docker @ArgumentosDocker
if ($LASTEXITCODE -ne 0) {
    throw 'No fue posible detener la infraestructura local.'
}

if ($EliminarDatos) {
    Write-Host 'La infraestructura y el volumen local fueron eliminados.' -ForegroundColor Yellow
}
else {
    Write-Host 'La infraestructura fue detenida; los datos permanecen en el volumen.' -ForegroundColor Green
}
