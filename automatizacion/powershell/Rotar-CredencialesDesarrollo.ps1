[CmdletBinding()]
param(
    [switch] $Confirmar
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaCompose = Join-Path $RutaRaiz 'infraestructura\contenedores\compose.desarrollo.yml'
$RutaSecretoAdmin = Join-Path $RutaRaiz 'configuracion\secretos\postgresql_contrasena.txt'
$RutaSecretoAplicacion = Join-Path $RutaRaiz 'configuracion\secretos\postgresql_aplicacion_contrasena.txt'
$RutaNuevaAdmin = "$RutaSecretoAdmin.nuevo"
$RutaNuevaAplicacion = "$RutaSecretoAplicacion.nuevo"
$RutaAnteriorAdmin = "$RutaSecretoAdmin.anterior"
$RutaAnteriorAplicacion = "$RutaSecretoAplicacion.anterior"
$RutaVerificarInfraestructura = Join-Path $PSScriptRoot 'Verificar-Infraestructura.ps1'
$RutaConfigurarUsuario = Join-Path $PSScriptRoot 'Configurar-UsuarioAplicacion.ps1'
$CodificacionSinBom = [System.Text.UTF8Encoding]::new($false)
$BaseDatosActualizada = $false
$RotacionVerificada = $false
$NuevaAdmin = $null
$NuevaAplicacion = $null

function Crear-Secreto {
    $BytesAleatorios = New-Object byte[] 48
    $Generador = [System.Security.Cryptography.RandomNumberGenerator]::Create()

    try {
        $Generador.GetBytes($BytesAleatorios)
        return [Convert]::ToBase64String($BytesAleatorios)
    }
    finally {
        $Generador.Dispose()
    }
}

if (-not $Confirmar) {
    throw 'La operación cambia credenciales activas. Ejecútela nuevamente con -Confirmar.'
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker no está disponible en PATH.'
}

$RutasRequeridas = @(
    $RutaEntorno,
    $RutaCompose,
    $RutaSecretoAdmin,
    $RutaSecretoAplicacion,
    $RutaVerificarInfraestructura,
    $RutaConfigurarUsuario
)

foreach ($Ruta in $RutasRequeridas) {
    if (-not (Test-Path -LiteralPath $Ruta -PathType Leaf)) {
        throw "No existe el archivo requerido: $Ruta"
    }
}

& $RutaVerificarInfraestructura

$ArgumentosCompose = @(
    'compose',
    '--env-file', $RutaEntorno,
    '--file', $RutaCompose
)

try {
    $NuevaAdmin = Crear-Secreto
    $NuevaAplicacion = Crear-Secreto
    [System.IO.File]::WriteAllText($RutaNuevaAdmin, $NuevaAdmin, $CodificacionSinBom)
    [System.IO.File]::WriteAllText($RutaNuevaAplicacion, $NuevaAplicacion, $CodificacionSinBom)

    Copy-Item -LiteralPath $RutaSecretoAdmin -Destination $RutaAnteriorAdmin -Force
    Copy-Item -LiteralPath $RutaSecretoAplicacion -Destination $RutaAnteriorAplicacion -Force

    Write-Host 'Rotando ambas credenciales dentro de una transacción...' -ForegroundColor Cyan
    $Entrada = "$NuevaAdmin`n$NuevaAplicacion`n"
    $Entrada | & docker @ArgumentosCompose exec -T base-datos `
        sh /opt/reddiff/herramientas/rotar-credenciales.sh

    if ($LASTEXITCODE -ne 0) {
        throw 'PostgreSQL rechazó la rotación; los secretos activos no fueron reemplazados.'
    }

    $BaseDatosActualizada = $true
    Copy-Item -LiteralPath $RutaNuevaAdmin -Destination $RutaSecretoAdmin -Force
    Copy-Item -LiteralPath $RutaNuevaAplicacion -Destination $RutaSecretoAplicacion -Force

    Write-Host 'Recreando el contenedor con los secretos nuevos...' -ForegroundColor Cyan
    & docker @ArgumentosCompose up --detach --force-recreate --wait base-datos
    if ($LASTEXITCODE -ne 0) {
        throw 'Las credenciales cambiaron, pero el contenedor no pudo recrearse.'
    }

    & $RutaVerificarInfraestructura
    & $RutaConfigurarUsuario

    $RotacionVerificada = $true
    Write-Host 'Las credenciales de desarrollo fueron rotadas y verificadas.' -ForegroundColor Green
}
finally {
    $Entrada = $null
    $NuevaAdmin = $null
    $NuevaAplicacion = $null

    if ($RotacionVerificada) {
        Remove-Item -LiteralPath $RutaNuevaAdmin, $RutaNuevaAplicacion -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $RutaAnteriorAdmin, $RutaAnteriorAplicacion -Force -ErrorAction SilentlyContinue
    }
    elseif ($BaseDatosActualizada) {
        Write-Warning 'PostgreSQL ya cambió las credenciales. Se conservaron los archivos .nuevo y .anterior para recuperación.'
    }
    else {
        Remove-Item -LiteralPath $RutaNuevaAdmin, $RutaNuevaAplicacion -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $RutaAnteriorAdmin, $RutaAnteriorAplicacion -Force -ErrorAction SilentlyContinue
    }
}
