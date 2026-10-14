[CmdletBinding()]
param(
    [string] $RutaDatos = (Join-Path $env:ProgramData 'RedDiff'),

    [string] $Destino
)

. (Join-Path $PSScriptRoot 'Comun.ps1')

Confirmar-Administrador
$Configuracion = Leer-ConfiguracionInstalacion -RutaDatos $RutaDatos
$RutaPgDump = Join-Path (Split-Path -Parent $Configuracion.RutaPsql) 'pg_dump.exe'
$RutaSecretoAdmin = Join-Path $Configuracion.RutaDatos 'secretos\postgresql_admin_contrasena.txt'
$RutaClaves = Join-Path $Configuracion.RutaDatos 'claves-proteccion'
$ContrasenaAdmin = $null
$ContrasenaAnterior = [Environment]::GetEnvironmentVariable('PGPASSWORD', 'Process')

if (-not (Test-Path -LiteralPath $RutaPgDump -PathType Leaf)) {
    throw "No existe pg_dump.exe en $RutaPgDump."
}

if (-not (Test-Path -LiteralPath $RutaSecretoAdmin -PathType Leaf)) {
    throw "No existe el secreto administrativo: $RutaSecretoAdmin"
}

if ([string]::IsNullOrWhiteSpace($Destino)) {
    $Destino = Join-Path $Configuracion.RutaDatos 'respaldos'
}

$Destino = [System.IO.Path]::GetFullPath($Destino)
$MarcaTiempo = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$RutaFinal = Join-Path $Destino "reddiff-$MarcaTiempo"
$RutaTemporal = "$RutaFinal.tmp"

try {
    New-Item -ItemType Directory -Path $RutaTemporal -Force | Out-Null
    $ContrasenaAdmin = [System.IO.File]::ReadAllText($RutaSecretoAdmin).TrimEnd("`r", "`n")
    if ([string]::IsNullOrWhiteSpace($ContrasenaAdmin)) {
        throw 'El secreto administrativo de PostgreSQL está vacío.'
    }

    [Environment]::SetEnvironmentVariable('PGPASSWORD', $ContrasenaAdmin, 'Process')
    Invocar-ComandoNativo -Comando $RutaPgDump -Argumentos @(
        '--host=127.0.0.1',
        "--port=$($Configuracion.PuertoPostgreSql)",
        "--username=$($Configuracion.UsuarioAdministradorBaseDatos)",
        "--dbname=$($Configuracion.NombreBaseDatos)",
        '--format=custom',
        '--no-password',
        "--file=$(Join-Path $RutaTemporal 'base-datos.dump')"
    ) -Descripcion 'El respaldo de PostgreSQL'

    if (-not (Test-Path -LiteralPath $RutaClaves -PathType Container)) {
        throw "No existe el directorio de claves: $RutaClaves"
    }

    Compress-Archive `
        -Path (Join-Path $RutaClaves '*') `
        -DestinationPath (Join-Path $RutaTemporal 'claves-proteccion.zip') `
        -CompressionLevel Optimal

    $Archivos = @('base-datos.dump', 'claves-proteccion.zip')
    $Lineas = foreach ($Archivo in $Archivos) {
        $Hash = Get-FileHash -LiteralPath (Join-Path $RutaTemporal $Archivo) -Algorithm SHA256
        "$($Hash.Hash.ToLowerInvariant())  $Archivo"
    }
    Escribir-TextoSinBom `
        -Ruta (Join-Path $RutaTemporal 'manifiesto.sha256') `
        -Contenido (($Lineas -join "`n") + "`n")

    Move-Item -LiteralPath $RutaTemporal -Destination $RutaFinal
    Invocar-ComandoNativo -Comando 'icacls.exe' -Argumentos @(
        $RutaFinal,
        '/inheritance:r',
        '/grant:r',
        'SYSTEM:(OI)(CI)F',
        'BUILTIN\Administrators:(OI)(CI)F',
        '/T',
        '/C'
    ) -Descripcion 'No fue posible proteger el respaldo'

    Write-Host "Respaldo creado: $RutaFinal" -ForegroundColor Green
}
finally {
    [Environment]::SetEnvironmentVariable('PGPASSWORD', $ContrasenaAnterior, 'Process')
    $ContrasenaAdmin = $null
    if (Test-Path -LiteralPath $RutaTemporal) {
        Remove-Item -LiteralPath $RutaTemporal -Recurse -Force -ErrorAction SilentlyContinue
    }
}
