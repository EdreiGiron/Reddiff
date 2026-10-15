[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

$Sistema = Get-CimInstance Win32_OperatingSystem
if ($Sistema.Caption -notmatch 'Windows 11') {
    throw "Este procedimiento esta validado para Windows 11. Sistema detectado: $($Sistema.Caption)."
}

if (-not [Environment]::Is64BitOperatingSystem) {
    throw 'RedDiff requiere Windows de 64 bits.'
}

if (-not (Get-Command wsl.exe -ErrorAction SilentlyContinue)) {
    throw 'WSL 2 no esta instalado.'
}

& wsl.exe --status
if ($LASTEXITCODE -ne 0) {
    throw 'WSL no respondio correctamente. Ejecute wsl --update y reinicie Windows.'
}

Assert-DockerLinuxDisponible

$VersionDocker = @(Invoke-Docker -Argumentos @('version', '--format', '{{.Server.Version}}') -Capturar |
    Select-Object -First 1)
$VersionCompose = @(Invoke-Docker -Argumentos @('compose', 'version', '--short') -Capturar |
    Select-Object -First 1)

$PerfilPublico = @(Get-NetConnectionProfile | Where-Object NetworkCategory -eq 'Public')
if ($PerfilPublico.Count -gt 0) {
    Write-Warning 'Existe una interfaz con perfil Publico. Las reglas de RedDiff solo se habilitan para Domain y Private.'
}

Write-Host "Windows detectado: $($Sistema.Caption) $($Sistema.Version)" -ForegroundColor Green
Write-Host "Docker Engine: $(([string] $VersionDocker[0]).Trim())"
Write-Host "Docker Compose: $(([string] $VersionCompose[0]).Trim())"
Write-Host 'Motor de contenedores Linux disponible.'
Write-Host 'Los requisitos de Windows finalizaron correctamente.' -ForegroundColor Green
