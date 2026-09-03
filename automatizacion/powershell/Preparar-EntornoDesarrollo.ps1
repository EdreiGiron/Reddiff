[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaPlantillaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env.example'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaDirectorioSecretos = Join-Path $RutaRaiz 'configuracion\secretos'
$RutaSecretoPostgresql = Join-Path $RutaDirectorioSecretos 'postgresql_contrasena.txt'

if (-not (Test-Path -LiteralPath $RutaPlantillaEntorno -PathType Leaf)) {
    throw "No se encontró la plantilla de entorno: $RutaPlantillaEntorno"
}

if (-not (Test-Path -LiteralPath $RutaEntorno -PathType Leaf)) {
    Copy-Item -LiteralPath $RutaPlantillaEntorno -Destination $RutaEntorno
    Write-Host 'Se creó configuracion\entornos\desarrollo.env.' -ForegroundColor Green
}
else {
    Write-Host 'Se conservó el archivo desarrollo.env existente.' -ForegroundColor DarkGray
}

if (-not (Test-Path -LiteralPath $RutaDirectorioSecretos -PathType Container)) {
    New-Item -ItemType Directory -Path $RutaDirectorioSecretos -Force | Out-Null
}

if (-not (Test-Path -LiteralPath $RutaSecretoPostgresql -PathType Leaf)) {
    $BytesAleatorios = New-Object byte[] 48
    $Generador = [System.Security.Cryptography.RandomNumberGenerator]::Create()

    try {
        $Generador.GetBytes($BytesAleatorios)
    }
    finally {
        $Generador.Dispose()
    }

    $Contrasena = [Convert]::ToBase64String($BytesAleatorios)
    $CodificacionSinBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($RutaSecretoPostgresql, $Contrasena, $CodificacionSinBom)

    Write-Host 'Se generó el secreto local de PostgreSQL.' -ForegroundColor Green
}
elseif ((Get-Item -LiteralPath $RutaSecretoPostgresql).Length -eq 0) {
    throw 'El secreto postgresql_contrasena.txt existe, pero está vacío.'
}
else {
    Write-Host 'Se conservó el secreto local de PostgreSQL existente.' -ForegroundColor DarkGray
}

Write-Host 'El entorno local está preparado.' -ForegroundColor Cyan
