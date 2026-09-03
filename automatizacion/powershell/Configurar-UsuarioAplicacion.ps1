[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaCompose = Join-Path $RutaRaiz 'infraestructura\contenedores\compose.desarrollo.yml'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaSecretoAplicacion = Join-Path $RutaRaiz 'configuracion\secretos\postgresql_aplicacion_contrasena.txt'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker no está disponible en PATH.'
}

if (-not (Test-Path -LiteralPath $RutaEntorno -PathType Leaf)) {
    throw 'El entorno no está preparado. Ejecute Iniciar-Infraestructura.ps1.'
}

if (-not (Test-Path -LiteralPath $RutaSecretoAplicacion -PathType Leaf)) {
    throw 'No existe el secreto del usuario de aplicación. Ejecute Iniciar-Infraestructura.ps1.'
}

$ArgumentosCompose = @(
    'compose',
    '--env-file', $RutaEntorno,
    '--file', $RutaCompose
)

Write-Host 'Configurando el usuario de aplicación con privilegios limitados...' -ForegroundColor Cyan

& docker @ArgumentosCompose exec -T base-datos `
    sh /opt/reddiff/herramientas/configurar-usuario-aplicacion.sh

if ($LASTEXITCODE -ne 0) {
    throw 'No fue posible configurar el usuario de aplicación.'
}

& docker @ArgumentosCompose exec -T base-datos `
    sh /opt/reddiff/herramientas/verificar-usuario-aplicacion.sh

if ($LASTEXITCODE -ne 0) {
    throw 'El usuario de aplicación no superó la comprobación de acceso.'
}

Write-Host 'El usuario de aplicación está configurado y disponible.' -ForegroundColor Green
