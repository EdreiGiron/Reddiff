[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

Assert-DockerLinuxDisponible
Assert-ArchivosProduccion
Wait-ServicioSaludable -Servicio 'base-datos' -Intentos 5
Wait-ServicioSaludable -Servicio 'api' -Intentos 5

$RutaRespaldos = Join-Path $script:RutaRaiz 'respaldos\produccion'
$MarcaTiempo = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$RutaFinal = Join-Path $RutaRespaldos "reddiff-$MarcaTiempo"
$RutaTemporal = "$RutaFinal.tmp"

New-Item -ItemType Directory -Path $RutaRespaldos -Force | Out-Null
New-Item -ItemType Directory -Path $RutaTemporal | Out-Null

$ContenedorBaseDatos = @(Invoke-RedDiffCompose -Argumentos @('ps', '--quiet', 'base-datos') -Capturar |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) } |
    Select-Object -First 1)
$ContenedorApi = @(Invoke-RedDiffCompose -Argumentos @('ps', '--quiet', 'api') -Capturar |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) } |
    Select-Object -First 1)

if ($ContenedorBaseDatos.Count -eq 0 -or $ContenedorApi.Count -eq 0) {
    throw 'No se encontraron los contenedores requeridos para el respaldo.'
}

$IdBaseDatos = ([string] $ContenedorBaseDatos[0]).Trim()
$IdApi = ([string] $ContenedorApi[0]).Trim()
$RutaDumpContenedor = '/tmp/reddiff-respaldo.dump'

try {
    Invoke-Docker -Argumentos @(
        'exec', $IdBaseDatos,
        'sh', '-ec',
        'export PGPASSWORD="$(cat /run/secrets/postgresql_contrasena)"; exec pg_dump --host=127.0.0.1 --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --format=custom --no-password --file=/tmp/reddiff-respaldo.dump'
    )

    Invoke-Docker -Argumentos @(
        'cp',
        "${IdBaseDatos}:$RutaDumpContenedor",
        (Join-Path $RutaTemporal 'base-datos.dump')
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
    $MontajeDestino = "type=bind,source=$RutaTemporal,destination=/destino"
    Invoke-Docker -Argumentos @(
        'run', '--rm',
        '--volume', "${NombreVolumen}:/origen:ro",
        '--mount', $MontajeDestino,
        'alpine:3.22',
        'tar', '-czf', '/destino/claves-proteccion.tar.gz', '-C', '/origen', '.'
    )

    $Archivos = @('base-datos.dump', 'claves-proteccion.tar.gz')
    $Lineas = foreach ($Archivo in $Archivos) {
        $Hash = Get-FileHash -LiteralPath (Join-Path $RutaTemporal $Archivo) -Algorithm SHA256
        "$($Hash.Hash.ToLowerInvariant())  $Archivo"
    }

    [IO.File]::WriteAllLines(
        (Join-Path $RutaTemporal 'manifiesto.sha256'),
        $Lineas,
        $script:CodificacionUtf8SinBom
    )

    Move-Item -LiteralPath $RutaTemporal -Destination $RutaFinal
}
finally {
    try {
        Invoke-Docker -Argumentos @('exec', $IdBaseDatos, 'rm', '-f', $RutaDumpContenedor)
    }
    catch {
        Write-Warning 'No fue posible retirar el archivo temporal del contenedor de PostgreSQL.'
    }

    if (Test-Path -LiteralPath $RutaTemporal -PathType Container) {
        Remove-Item -LiteralPath $RutaTemporal -Recurse -Force
    }
}

Write-Host "Respaldo creado: $RutaFinal" -ForegroundColor Green
