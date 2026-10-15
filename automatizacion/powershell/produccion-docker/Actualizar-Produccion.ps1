[CmdletBinding()]
param(
    [switch]$Confirmar,
    [switch]$OmitirGitPull
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

if (-not $Confirmar) {
    throw 'La actualizacion crea un respaldo y reconstruye los servicios. Repita con -Confirmar.'
}

& (Join-Path $PSScriptRoot 'Respaldar-Produccion.ps1')

if (-not $OmitirGitPull) {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw 'Git no esta disponible. Use -OmitirGitPull si actualizara los archivos por otro medio.'
    }

    $Estado = @(& git -C $script:RutaRaiz status --porcelain)
    if ($LASTEXITCODE -ne 0) {
        throw 'No fue posible consultar el estado del repositorio.'
    }

    if ($Estado.Count -gt 0) {
        throw 'El repositorio contiene cambios locales. La actualizacion automatica se detuvo para no sobrescribirlos.'
    }

    & git -C $script:RutaRaiz pull --ff-only
    if ($LASTEXITCODE -ne 0) {
        throw 'Git no pudo actualizar el repositorio mediante avance rapido.'
    }
}

& (Join-Path $PSScriptRoot 'Desplegar-Produccion.ps1')
& (Join-Path $PSScriptRoot 'Verificar-Produccion.ps1')

Write-Host 'RedDiff se actualizo y verifico correctamente.' -ForegroundColor Green
