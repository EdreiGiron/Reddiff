Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:NombreSitioRedDiff = 'RedDiff'
$script:NombrePoolRedDiff = 'RedDiff'
$script:RutaDatosRedDiffPredeterminada = Join-Path $env:ProgramData 'RedDiff'
$script:RutaInstalacionRedDiffPredeterminada = Join-Path $env:ProgramFiles 'RedDiff'
$script:CodificacionUtf8SinBom = [System.Text.UTF8Encoding]::new($false)

function Confirmar-Administrador {
    $Identidad = [Security.Principal.WindowsIdentity]::GetCurrent()
    $Principal = New-Object Security.Principal.WindowsPrincipal($Identidad)
    if (-not $Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Abra PowerShell con la opción Ejecutar como administrador.'
    }
}

function Convertir-SecureStringATexto {
    param(
        [Parameter(Mandatory = $true)]
        [Security.SecureString] $Valor
    )

    $Puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Valor)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($Puntero)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($Puntero)
    }
}

function Crear-SecretoAleatorio {
    $Bytes = New-Object byte[] 48
    $Generador = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $Generador.GetBytes($Bytes)
        return [Convert]::ToBase64String($Bytes)
    }
    finally {
        $Generador.Dispose()
    }
}

function Escribir-TextoSinBom {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Ruta,

        [AllowEmptyString()]
        [Parameter(Mandatory = $true)]
        [string] $Contenido
    )

    [System.IO.File]::WriteAllText($Ruta, $Contenido, $script:CodificacionUtf8SinBom)
}

function Invocar-ComandoNativo {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Comando,

        [Parameter(Mandatory = $true)]
        [string[]] $Argumentos,

        [Parameter(Mandatory = $true)]
        [string] $Descripcion
    )

    & $Comando @Argumentos
    if ($LASTEXITCODE -ne 0) {
        throw "$Descripcion falló con el código $LASTEXITCODE."
    }
}

function Resolver-Psql {
    param([string] $RutaPsql)

    if (-not [string]::IsNullOrWhiteSpace($RutaPsql)) {
        $RutaCompleta = [System.IO.Path]::GetFullPath($RutaPsql)
        if (-not (Test-Path -LiteralPath $RutaCompleta -PathType Leaf)) {
            throw "No existe psql.exe en $RutaCompleta."
        }

        return $RutaCompleta
    }

    $Comando = Get-Command psql.exe -ErrorAction SilentlyContinue
    if ($null -ne $Comando) {
        return $Comando.Source
    }

    $Candidatos = Get-ChildItem -Path (Join-Path $env:ProgramFiles 'PostgreSQL') `
        -Filter psql.exe -Recurse -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending

    if ($null -eq $Candidatos -or $Candidatos.Count -eq 0) {
        throw 'No se encontró psql.exe. Instale PostgreSQL o indique -RutaPsql.'
    }

    return $Candidatos[0].FullName
}

function Obtener-VersionMayorPostgreSql {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RutaPsql
    )

    $Salida = & $RutaPsql --version
    if ($LASTEXITCODE -ne 0 -or $Salida -notmatch '(\d+)(?:\.\d+)?') {
        throw 'No fue posible determinar la versión de PostgreSQL.'
    }

    $Version = [int]$Matches[1]
    if ($Version -lt 15 -or $Version -gt 18) {
        throw "PostgreSQL $Version no está admitido. Utilice una versión entre 15 y 18."
    }

    return $Version
}

function Leer-ConfiguracionInstalacion {
    param(
        [string] $RutaDatos = $script:RutaDatosRedDiffPredeterminada
    )

    $Ruta = Join-Path $RutaDatos 'instalacion.json'
    if (-not (Test-Path -LiteralPath $Ruta -PathType Leaf)) {
        throw "No existe la configuración de instalación: $Ruta"
    }

    return Get-Content -LiteralPath $Ruta -Raw | ConvertFrom-Json
}

function Establecer-AclRedDiff {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Ruta,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Lectura', 'Modificacion')]
        [string] $Nivel
    )

    $PermisoPool = if ($Nivel -eq 'Modificacion') { '(OI)(CI)M' } else { '(OI)(CI)RX' }
    Invocar-ComandoNativo -Comando 'icacls.exe' -Argumentos @(
        $Ruta,
        '/inheritance:r',
        '/grant:r',
        'SYSTEM:(OI)(CI)F',
        'BUILTIN\Administrators:(OI)(CI)F',
        "IIS AppPool\$($script:NombrePoolRedDiff):$PermisoPool",
        '/T',
        '/C'
    ) -Descripcion "No fue posible restringir los permisos de $Ruta"
}

function Obtener-OrigenHttps {
    param(
        [Parameter(Mandatory = $true)]
        [string] $NombreHost,

        [Parameter(Mandatory = $true)]
        [int] $PuertoHttps
    )

    if ($PuertoHttps -eq 443) {
        return "https://$NombreHost"
    }

    return "https://$NombreHost`:$PuertoHttps"
}
