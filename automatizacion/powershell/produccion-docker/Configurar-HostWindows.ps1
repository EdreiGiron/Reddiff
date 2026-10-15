[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$NombreHost,
    [string[]]$OrigenWeb = @('LocalSubnet'),
    [string[]]$OrigenSyslog = @('LocalSubnet'),
    [switch]$AgregarResolucionLocal,
    [string]$DireccionResolucionLocal = '127.0.0.1'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

if (-not (Test-EsAdministrador)) {
    throw 'Abra PowerShell como administrador para configurar certificados, firewall y archivo hosts.'
}

Assert-Archivo -Ruta (Join-Path $script:RutaSecretos 'autoridad_certificadora.crt')

$RutaAutoridad = Join-Path $script:RutaSecretos 'autoridad_certificadora.crt'
$Certificado = Import-Certificate `
    -FilePath $RutaAutoridad `
    -CertStoreLocation 'Cert:\LocalMachine\Root'

if (-not $Certificado) {
    throw 'No fue posible instalar la autoridad certificadora de RedDiff.'
}

function Set-ReglaRedDiff {
    param(
        [Parameter(Mandatory)][string]$Nombre,
        [Parameter(Mandatory)][string]$Descripcion,
        [Parameter(Mandatory)][ValidateSet('TCP', 'UDP')][string]$Protocolo,
        [Parameter(Mandatory)][int]$Puerto,
        [Parameter(Mandatory)][string[]]$Origen
    )

    $Existente = Get-NetFirewallRule -Name $Nombre -ErrorAction SilentlyContinue
    if ($Existente) {
        Remove-NetFirewallRule -Name $Nombre
    }

    New-NetFirewallRule `
        -Name $Nombre `
        -DisplayName $Descripcion `
        -Direction Inbound `
        -Action Allow `
        -Enabled True `
        -Profile Domain, Private `
        -Protocol $Protocolo `
        -LocalPort $Puerto `
        -RemoteAddress $Origen | Out-Null
}

$PuertoHttps = [int] (Get-VariableEntorno -Nombre 'REDDIFF_HTTPS_PUERTO')
$PuertoSyslog = [int] (Get-VariableEntorno -Nombre 'REDDIFF_SYSLOG_PUERTO_PUBLICO')

Set-ReglaRedDiff `
    -Nombre 'RedDiff-Docker-HTTPS' `
    -Descripcion 'RedDiff HTTPS mediante Docker Compose' `
    -Protocolo TCP `
    -Puerto $PuertoHttps `
    -Origen $OrigenWeb

Set-ReglaRedDiff `
    -Nombre 'RedDiff-Docker-Syslog' `
    -Descripcion 'RedDiff Syslog mediante Docker Compose' `
    -Protocolo UDP `
    -Puerto $PuertoSyslog `
    -Origen $OrigenSyslog

if ($AgregarResolucionLocal -and $NombreHost -notmatch '^\d{1,3}(\.\d{1,3}){3}$') {
    $RutaHosts = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'
    $Patron = '(?im)^\s*[^#\r\n]+\s+' + [Regex]::Escape($NombreHost) + '(?:\s|$)'
    $Contenido = [IO.File]::ReadAllText($RutaHosts)

    if ($Contenido -notmatch $Patron) {
        $Linea = "`r`n$DireccionResolucionLocal`t$NombreHost`t# RedDiff Docker`r`n"
        [IO.File]::AppendAllText($RutaHosts, $Linea, [Text.Encoding]::ASCII)
    }
}

Write-Host 'Certificado, reglas de firewall y resolucion local configurados.' -ForegroundColor Green
