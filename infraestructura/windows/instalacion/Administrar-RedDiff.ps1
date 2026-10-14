[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Iniciar', 'Detener', 'Reiniciar', 'Estado')]
    [string] $Accion
)

. (Join-Path $PSScriptRoot 'Comun.ps1')

Confirmar-Administrador
Import-Module WebAdministration -ErrorAction Stop

switch ($Accion) {
    'Iniciar' {
        Start-WebAppPool -Name $NombrePoolRedDiff -ErrorAction SilentlyContinue
        Start-Website -Name $NombreSitioRedDiff
    }
    'Detener' {
        Stop-Website -Name $NombreSitioRedDiff -ErrorAction SilentlyContinue
        Stop-WebAppPool -Name $NombrePoolRedDiff -ErrorAction SilentlyContinue
    }
    'Reiniciar' {
        Restart-WebAppPool -Name $NombrePoolRedDiff
    }
    'Estado' {
        [pscustomobject]@{
            Sitio = (Get-WebsiteState -Name $NombreSitioRedDiff).Value
            GrupoAplicaciones = (Get-WebAppPoolState -Name $NombrePoolRedDiff).Value
        }
    }
}

if ($Accion -ne 'Estado') {
    Write-Host "Operación $Accion completada para RedDiff." -ForegroundColor Green
}
