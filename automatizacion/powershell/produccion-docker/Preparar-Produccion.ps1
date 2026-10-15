[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$NombreHost,
    [string[]]$OrigenWeb = @('LocalSubnet'),
    [string[]]$OrigenSyslog = @('LocalSubnet'),
    [switch]$ConfigurarHostWindows,
    [switch]$AgregarResolucionLocal
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

if ($NombreHost -notmatch '^[A-Za-z0-9.-]+$') {
    throw 'El nombre del host contiene caracteres no permitidos.'
}

Assert-DockerLinuxDisponible
Assert-Archivo -Ruta $script:RutaPlantillaEntorno
Assert-Archivo -Ruta $script:RutaCompose

if ($ConfigurarHostWindows -and -not (Test-EsAdministrador)) {
    throw 'Abra PowerShell como administrador cuando utilice -ConfigurarHostWindows.'
}

if (-not (Test-Path -LiteralPath $script:RutaEntorno -PathType Leaf)) {
    Copy-Item -LiteralPath $script:RutaPlantillaEntorno -Destination $script:RutaEntorno
}

Set-VariableEntorno -Nombre 'REDDIFF_NOMBRE_HOST' -Valor $NombreHost
Set-VariableEntorno -Nombre 'REDDIFF_ORIGEN_CLIENTE' -Valor "https://$NombreHost"

if (-not (Test-Path -LiteralPath $script:RutaSecretos -PathType Container)) {
    New-Item -ItemType Directory -Path $script:RutaSecretos | Out-Null
}

Write-SecretoSiFalta -Ruta (Join-Path $script:RutaSecretos 'postgresql_contrasena.txt')
Write-SecretoSiFalta -Ruta (Join-Path $script:RutaSecretos 'postgresql_aplicacion_contrasena.txt')

$RutaAdministrador = Join-Path $script:RutaSecretos 'administrador_inicial_contrasena.txt'
if (-not (Test-Path -LiteralPath $RutaAdministrador -PathType Leaf)) {
    [IO.File]::WriteAllText($RutaAdministrador, '', $script:CodificacionUtf8SinBom)
}

$ArchivosCertificados = @(
    'autoridad_certificadora.crt',
    'autoridad_certificadora.key',
    'postgresql_servidor.crt',
    'postgresql_servidor.key',
    'reddiff_https.crt',
    'reddiff_https.key'
)

$Existentes = @($ArchivosCertificados | Where-Object {
        Test-Path -LiteralPath (Join-Path $script:RutaSecretos $_) -PathType Leaf
    })

if ($Existentes.Count -gt 0 -and $Existentes.Count -lt $ArchivosCertificados.Count) {
    throw "El conjunto de certificados esta incompleto. Revise $script:RutaSecretos; no se reemplazara material existente."
}

if ($Existentes.Count -eq 0) {
    $RutaDockerfile = Join-Path $script:RutaRaiz 'infraestructura\contenedores\produccion\certificados\Dockerfile'
    $Etiqueta = 'reddiff-certificados:local'
    Assert-Archivo -Ruta $RutaDockerfile

    Write-Host 'Construyendo la herramienta aislada para generar certificados...' -ForegroundColor Cyan
    Invoke-Docker -Argumentos @(
        'build',
        '--file', $RutaDockerfile,
        '--tag', $Etiqueta,
        $script:RutaRaiz
    )

    $Montaje = "type=bind,source=$script:RutaSecretos,destination=/salida"
    Invoke-Docker -Argumentos @(
        'run', '--rm',
        '--mount', $Montaje,
        $Etiqueta,
        $NombreHost
    )
}

# Limita el acceso local a secretos al usuario actual, SYSTEM y administradores.
$SidUsuario = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
& icacls $script:RutaSecretos '/inheritance:r' `
    '/grant:r' "*${SidUsuario}:(OI)(CI)F" `
    '*S-1-5-18:(OI)(CI)F' `
    '*S-1-5-32-544:(OI)(CI)F' `
    '/T' '/C' | Out-Null

if ($LASTEXITCODE -ne 0) {
    throw 'No fue posible proteger el directorio local de secretos con ACL de Windows.'
}

if ($ConfigurarHostWindows) {
    $ArgumentosHost = @{
        NombreHost = $NombreHost
        OrigenWeb = $OrigenWeb
        OrigenSyslog = $OrigenSyslog
    }
    if ($AgregarResolucionLocal) {
        $ArgumentosHost.AgregarResolucionLocal = $true
    }

    & (Join-Path $PSScriptRoot 'Configurar-HostWindows.ps1') @ArgumentosHost
}

Write-Host "Preparacion completada para https://$NombreHost." -ForegroundColor Green
Write-Host "Revise $script:RutaEntorno y luego ejecute Desplegar-Produccion.ps1."
