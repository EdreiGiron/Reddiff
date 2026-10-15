[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

Assert-DockerLinuxDisponible
Assert-Archivo -Ruta $script:RutaEntorno
Assert-Archivo -Ruta $script:RutaCompose

Invoke-RedDiffCompose -Argumentos @('down')
Write-Host 'Los contenedores se detuvieron; los volumenes y la informacion se conservaron.' -ForegroundColor Green
