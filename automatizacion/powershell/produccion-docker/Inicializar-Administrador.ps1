[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

Assert-DockerLinuxDisponible
Assert-ArchivosProduccion

$RutaSecreto = Join-Path $script:RutaSecretos 'administrador_inicial_contrasena.txt'
$Contrasena = $null
$Confirmacion = $null

try {
    $ContrasenaSegura = Read-Host 'Contrasena del primer administrador' -AsSecureString
    $ConfirmacionSegura = Read-Host 'Repita la contrasena' -AsSecureString
    $Contrasena = ConvertTo-TextoPlanoTemporal -Valor $ContrasenaSegura
    $Confirmacion = ConvertTo-TextoPlanoTemporal -Valor $ConfirmacionSegura

    if ($Contrasena -cne $Confirmacion) {
        throw 'Las contrasenas no coinciden.'
    }

    if ($Contrasena.Length -lt 12 -or $Contrasena.Length -gt 128) {
        throw 'La contrasena debe contener entre 12 y 128 caracteres.'
    }

    [IO.File]::WriteAllText($RutaSecreto, $Contrasena, $script:CodificacionUtf8SinBom)
    Invoke-RedDiffCompose -Argumentos @(
        '--profile', 'herramientas',
        'run', '--rm', 'inicializador'
    )
}
finally {
    [IO.File]::WriteAllText($RutaSecreto, '', $script:CodificacionUtf8SinBom)
    $Contrasena = $null
    $Confirmacion = $null
}

Write-Host 'La primera cuenta administrativa fue creada correctamente.' -ForegroundColor Green
