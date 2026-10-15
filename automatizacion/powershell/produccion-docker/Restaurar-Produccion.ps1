[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$RutaRespaldo,
    [switch]$Confirmar
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

if (-not $Confirmar) {
    throw 'La restauracion reemplaza la base de datos y las claves actuales. Repita con -Confirmar.'
}

Assert-DockerLinuxDisponible
Assert-ArchivosProduccion

$RutaRespaldo = [IO.Path]::GetFullPath($RutaRespaldo)
if (-not (Test-Path -LiteralPath $RutaRespaldo -PathType Container)) {
    throw "No existe el directorio de respaldo: $RutaRespaldo"
}

$RutaDump = Join-Path $RutaRespaldo 'base-datos.dump'
$RutaClaves = Join-Path $RutaRespaldo 'claves-proteccion.tar.gz'
$RutaManifiesto = Join-Path $RutaRespaldo 'manifiesto.sha256'
Assert-Archivo -Ruta $RutaDump
Assert-Archivo -Ruta $RutaClaves
Assert-Archivo -Ruta $RutaManifiesto

$Esperados = @{}
foreach ($Linea in [IO.File]::ReadAllLines($RutaManifiesto)) {
    if ($Linea -match '^([0-9a-fA-F]{64})\s+\*?(.+)$') {
        $Esperados[$Matches[2].Trim()] = $Matches[1].ToLowerInvariant()
    }
}

foreach ($Archivo in @('base-datos.dump', 'claves-proteccion.tar.gz')) {
    if (-not $Esperados.ContainsKey($Archivo)) {
        throw "El manifiesto no contiene $Archivo."
    }

    $Actual = (Get-FileHash -LiteralPath (Join-Path $RutaRespaldo $Archivo) -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($Actual -cne $Esperados[$Archivo]) {
        throw "La integridad de $Archivo no coincide con el manifiesto."
    }
}

Invoke-RedDiffCompose -Argumentos @('stop', 'web', 'api')
Invoke-RedDiffCompose -Argumentos @('up', '--detach', 'base-datos')
Wait-ServicioSaludable -Servicio 'base-datos' -Intentos 60

$ContenedorBaseDatos = @(Invoke-RedDiffCompose -Argumentos @('ps', '--quiet', 'base-datos') -Capturar |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) } |
    Select-Object -First 1)
$ContenedorApi = @(Invoke-RedDiffCompose -Argumentos @('ps', '--all', '--quiet', 'api') -Capturar |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) } |
    Select-Object -First 1)

if ($ContenedorBaseDatos.Count -eq 0 -or $ContenedorApi.Count -eq 0) {
    throw 'No se encontraron los contenedores requeridos para restaurar RedDiff.'
}

$IdBaseDatos = ([string] $ContenedorBaseDatos[0]).Trim()
$IdApi = ([string] $ContenedorApi[0]).Trim()
$RutaDumpContenedor = '/tmp/reddiff-restauracion.dump'

try {
    Invoke-Docker -Argumentos @('cp', $RutaDump, "${IdBaseDatos}:$RutaDumpContenedor")

    Invoke-Docker -Argumentos @(
        'exec', $IdBaseDatos,
        'sh', '-ec',
        'export PGPASSWORD="$(cat /run/secrets/postgresql_contrasena)"; dropdb --host=127.0.0.1 --username="$POSTGRES_USER" --if-exists --force "$POSTGRES_DB"; createdb --host=127.0.0.1 --username="$POSTGRES_USER" --owner="$POSTGRES_USER" "$POSTGRES_DB"; pg_restore --host=127.0.0.1 --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --no-owner --no-privileges /tmp/reddiff-restauracion.dump'
    )

    $VolumenClaves = @(Invoke-Docker -Argumentos @(
            'inspect',
            '--format', '{{range .Mounts}}{{if eq .Destination "/var/lib/reddiff/claves-proteccion"}}{{.Name}}{{end}}{{end}}',
            $IdApi
        ) -Capturar | Select-Object -First 1)

    if ($VolumenClaves.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string] $VolumenClaves[0])) {
        throw 'No se encontro el volumen de claves de proteccion.'
    }

    $NombreVolumen = ([string] $VolumenClaves[0]).Trim()
    $MontajeRespaldo = "type=bind,source=$RutaRespaldo,destination=/respaldo,readonly"
    Invoke-Docker -Argumentos @(
        'run', '--rm',
        '--volume', "${NombreVolumen}:/destino",
        '--mount', $MontajeRespaldo,
        'alpine:3.22',
        'sh', '-ec',
        'find /destino -mindepth 1 -maxdepth 1 -exec rm -rf -- {} +; tar -xzf /respaldo/claves-proteccion.tar.gz -C /destino; chown -R 1654:1654 /destino; chmod 0700 /destino'
    )

    Invoke-RedDiffCompose -Argumentos @(
        'exec', '--no-TTY', 'base-datos',
        'sh', '/opt/reddiff/herramientas/configurar-usuario-aplicacion.sh'
    )

    Invoke-RedDiffCompose -Argumentos @('up', '--detach', 'api', 'web')
    Wait-ServicioSaludable -Servicio 'api' -Intentos 60
    Wait-ServicioSaludable -Servicio 'web' -Intentos 60
}
finally {
    try {
        Invoke-Docker -Argumentos @('exec', $IdBaseDatos, 'rm', '-f', $RutaDumpContenedor)
    }
    catch {
        Write-Warning 'No fue posible retirar el archivo temporal de restauracion.'
    }
}

& (Join-Path $PSScriptRoot 'Verificar-Produccion.ps1')
Write-Host 'RedDiff se restauro y verifico correctamente.' -ForegroundColor Green
