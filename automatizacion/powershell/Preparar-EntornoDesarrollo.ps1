[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$RutaPlantillaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env.example'
$RutaEntorno = Join-Path $RutaRaiz 'configuracion\entornos\desarrollo.env'
$RutaDirectorioSecretos = Join-Path $RutaRaiz 'configuracion\secretos'
$RutaSecretoPostgresql = Join-Path $RutaDirectorioSecretos 'postgresql_contrasena.txt'
$RutaSecretoAplicacion = Join-Path $RutaDirectorioSecretos 'postgresql_aplicacion_contrasena.txt'

function Confirmar-SecretoAleatorio {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Ruta,

        [Parameter(Mandatory = $true)]
        [string] $Descripcion
    )

    if (-not (Test-Path -LiteralPath $Ruta -PathType Leaf)) {
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
        [System.IO.File]::WriteAllText($Ruta, $Contrasena, $CodificacionSinBom)

        Write-Host "Se generó $Descripcion." -ForegroundColor Green
    }
    elseif ((Get-Item -LiteralPath $Ruta).Length -eq 0) {
        throw "El secreto $([System.IO.Path]::GetFileName($Ruta)) existe, pero está vacío."
    }
    else {
        Write-Host "Se conservó $Descripcion existente." -ForegroundColor DarkGray
    }
}

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

Confirmar-SecretoAleatorio `
    -Ruta $RutaSecretoPostgresql `
    -Descripcion 'el secreto local del administrador de PostgreSQL'

Confirmar-SecretoAleatorio `
    -Ruta $RutaSecretoAplicacion `
    -Descripcion 'el secreto local del usuario de aplicación'

Write-Host 'El entorno local está preparado.' -ForegroundColor Cyan
