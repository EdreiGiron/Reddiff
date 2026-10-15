[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

Assert-DockerLinuxDisponible
Assert-ArchivosProduccion

Wait-ServicioSaludable -Servicio 'base-datos' -Intentos 5
Wait-ServicioSaludable -Servicio 'api' -Intentos 5
Wait-ServicioSaludable -Servicio 'web' -Intentos 5

$NombreHost = Get-VariableEntorno -Nombre 'REDDIFF_NOMBRE_HOST'
$PuertoHttps = [int] (Get-VariableEntorno -Nombre 'REDDIFF_HTTPS_PUERTO')
$PuertoSyslog = [int] (Get-VariableEntorno -Nombre 'REDDIFF_SYSLOG_PUERTO_PUBLICO')
$Uri = "https://${NombreHost}:$PuertoHttps/salud/listo"

try {
    $Respuesta = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 15
}
catch {
    throw "No fue posible validar $Uri. Compruebe la resolucion del nombre y la confianza del certificado. $($_.Exception.Message)"
}

if ($Respuesta.StatusCode -ne 200) {
    throw "El endpoint de salud devolvio HTTP $($Respuesta.StatusCode)."
}

$ContenedorBaseDatos = @(Invoke-RedDiffCompose -Argumentos @('ps', '--quiet', 'base-datos') -Capturar |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) } |
    Select-Object -First 1)
$ContenedorApi = @(Invoke-RedDiffCompose -Argumentos @('ps', '--quiet', 'api') -Capturar |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) } |
    Select-Object -First 1)

if ($ContenedorBaseDatos.Count -eq 0 -or $ContenedorApi.Count -eq 0) {
    throw 'No se encontraron los contenedores necesarios para validar los puertos publicados.'
}

$PublicacionBaseDatos = @(Invoke-Docker -Argumentos @(
        'inspect',
        '--format', '{{json (index .NetworkSettings.Ports "5432/tcp")}}',
        ([string] $ContenedorBaseDatos[0]).Trim()
    ) -Capturar | Where-Object {
        -not [string]::IsNullOrWhiteSpace([string] $_) -and ([string] $_).Trim() -ne 'null'
    })

if ($PublicacionBaseDatos.Count -gt 0) {
    throw "PostgreSQL no debe estar publicado en Windows: $($PublicacionBaseDatos -join ', ')"
}

$PuertoWeb = Test-NetConnection -ComputerName '127.0.0.1' -Port $PuertoHttps -InformationLevel Quiet
if (-not $PuertoWeb) {
    throw "Windows no puede alcanzar el puerto HTTPS $PuertoHttps."
}

$SalidaPublicacionSyslog = @(Invoke-Docker -Argumentos @(
        'inspect',
        '--format', '{{json (index .NetworkSettings.Ports "5514/udp")}}',
        ([string] $ContenedorApi[0]).Trim()
    ) -Capturar | Select-Object -First 1)

if ($SalidaPublicacionSyslog.Count -eq 0 -or
    [string]::IsNullOrWhiteSpace([string] $SalidaPublicacionSyslog[0]) -or
    ([string] $SalidaPublicacionSyslog[0]).Trim() -eq 'null') {
    throw "No se encontro la publicacion UDP de Syslog esperada en el puerto $PuertoSyslog."
}

$PublicacionesSyslog = @(([string] $SalidaPublicacionSyslog[0]) | ConvertFrom-Json)
$PuertoSyslogCorrecto = @($PublicacionesSyslog | Where-Object {
        [int] ($_.HostPort) -eq $PuertoSyslog
    })

if ($PuertoSyslogCorrecto.Count -eq 0) {
    $PuertosEncontrados = @($PublicacionesSyslog | ForEach-Object { $_.HostPort }) -join ', '
    throw "Syslog no esta publicado en UDP $PuertoSyslog. Puertos encontrados: $PuertosEncontrados."
}

Invoke-RedDiffCompose -Argumentos @('ps')
Write-Host "Salud HTTPS confirmada en $Uri." -ForegroundColor Green
Write-Host "Syslog publicado en UDP $PuertoSyslog."
Write-Host 'PostgreSQL permanece sin publicacion en el host.'
Write-Host 'La verificacion tecnica de RedDiff finalizo correctamente.' -ForegroundColor Green
