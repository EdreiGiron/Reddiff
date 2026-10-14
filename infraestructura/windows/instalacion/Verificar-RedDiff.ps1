[CmdletBinding()]
param(
    [string] $RutaDatos = (Join-Path $env:ProgramData 'RedDiff')
)

. (Join-Path $PSScriptRoot 'Comun.ps1')

$Configuracion = Leer-ConfiguracionInstalacion -RutaDatos $RutaDatos
$Origen = Obtener-OrigenHttps `
    -NombreHost $Configuracion.NombreHost `
    -PuertoHttps ([int]$Configuracion.PuertoHttps)

Import-Module WebAdministration -ErrorAction Stop

$EstadoPool = (Get-WebAppPoolState -Name $NombrePoolRedDiff).Value
if ($EstadoPool -ne 'Started') {
    throw "El grupo de aplicaciones $NombrePoolRedDiff está en estado $EstadoPool."
}

$EstadoSitio = (Get-WebsiteState -Name $NombreSitioRedDiff).Value
if ($EstadoSitio -ne 'Started') {
    throw "El sitio $NombreSitioRedDiff está en estado $EstadoSitio."
}

$ServicioPostgreSql = Get-Service -Name 'postgresql*' -ErrorAction SilentlyContinue |
    Where-Object { $_.Status -eq 'Running' } |
    Select-Object -First 1
if ($null -eq $ServicioPostgreSql) {
    throw 'No se encontró un servicio PostgreSQL en ejecución.'
}

$Respuesta = Invoke-WebRequest `
    -Uri "$Origen/salud/listo" `
    -UseBasicParsing `
    -TimeoutSec 15

if ($Respuesta.StatusCode -ne 200) {
    throw "La comprobación de salud devolvió HTTP $($Respuesta.StatusCode)."
}

$PuertoSyslog = [int]$Configuracion.PuertoSyslog
$ProcesoIis = Get-Process -Name w3wp -ErrorAction SilentlyContinue
if ($null -eq $ProcesoIis) {
    throw 'IIS no tiene un proceso de trabajo activo para RedDiff.'
}

$ProcesosIis = @($ProcesoIis | Select-Object -ExpandProperty Id)
$ReceptorSyslog = Get-NetUDPEndpoint -LocalPort $PuertoSyslog -ErrorAction SilentlyContinue |
    Where-Object { $_.OwningProcess -in $ProcesosIis } |
    Select-Object -First 1
if ($null -eq $ReceptorSyslog) {
    throw "No se encontró el receptor Syslog de IIS escuchando en UDP $PuertoSyslog."
}

Write-Host 'RedDiff respondió correctamente y sus servicios están activos.' -ForegroundColor Green
Write-Host "Sitio: $Origen"
Write-Host "PostgreSQL: $($ServicioPostgreSql.Name)"
Write-Host "Syslog configurado: UDP $PuertoSyslog"
