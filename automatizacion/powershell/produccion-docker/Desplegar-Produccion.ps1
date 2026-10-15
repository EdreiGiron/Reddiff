[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

Assert-DockerLinuxDisponible
Assert-ArchivosProduccion

Write-Host 'Validando Docker Compose...' -ForegroundColor Cyan
Invoke-RedDiffCompose -Argumentos @('config', '--quiet')

Write-Host 'Construyendo las imagenes de RedDiff...' -ForegroundColor Cyan
Invoke-RedDiffCompose -Argumentos @(
    '--profile', 'herramientas',
    'build',
    'base-datos', 'migrador', 'api', 'web'
)

Write-Host 'Iniciando PostgreSQL...' -ForegroundColor Cyan
Invoke-RedDiffCompose -Argumentos @('up', '--detach', 'base-datos')
Wait-ServicioSaludable -Servicio 'base-datos' -Intentos 60

Write-Host 'Aplicando migraciones con la cuenta administrativa...' -ForegroundColor Cyan
Invoke-RedDiffCompose -Argumentos @(
    '--profile', 'herramientas',
    'run', '--rm', 'migrador'
)

Write-Host 'Restableciendo los privilegios de la cuenta de aplicacion...' -ForegroundColor Cyan
Invoke-RedDiffCompose -Argumentos @(
    'exec', '--no-TTY', 'base-datos',
    'sh', '/opt/reddiff/herramientas/configurar-usuario-aplicacion.sh'
)

Write-Host 'Iniciando API y cliente web...' -ForegroundColor Cyan
Invoke-RedDiffCompose -Argumentos @('up', '--detach', 'api', 'web')
Wait-ServicioSaludable -Servicio 'api' -Intentos 60
Wait-ServicioSaludable -Servicio 'web' -Intentos 60

Invoke-RedDiffCompose -Argumentos @('ps')

Write-Host 'RedDiff se desplego correctamente con Docker Compose.' -ForegroundColor Green
Write-Host 'Si es la primera instalacion, ejecute Inicializar-Administrador.ps1.'
