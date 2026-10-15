Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:RutaRaiz = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$script:RutaCompose = Join-Path $script:RutaRaiz 'infraestructura\contenedores\produccion\compose.yml'
$script:RutaEntorno = Join-Path $script:RutaRaiz 'configuracion\entornos\produccion.env'
$script:RutaPlantillaEntorno = Join-Path $script:RutaRaiz 'configuracion\entornos\produccion.env.example'
$script:RutaSecretos = Join-Path $script:RutaRaiz 'configuracion\secretos\produccion'
$script:CodificacionUtf8SinBom = New-Object System.Text.UTF8Encoding($false)

function Test-EsAdministrador {
    $Identidad = [Security.Principal.WindowsIdentity]::GetCurrent()
    $Principal = New-Object Security.Principal.WindowsPrincipal($Identidad)
    return $Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-Archivo {
    param([Parameter(Mandatory)][string]$Ruta)

    if (-not (Test-Path -LiteralPath $Ruta -PathType Leaf)) {
        throw "No existe el archivo requerido: $Ruta"
    }
}

function Invoke-Docker {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string[]]$Argumentos,
        [switch]$Capturar
    )

    if ($Capturar) {
        $Salida = @(& docker @Argumentos 2>&1)
        $Codigo = $LASTEXITCODE
        if ($Codigo -ne 0) {
            throw "Docker finalizo con codigo $Codigo.`n$($Salida -join [Environment]::NewLine)"
        }

        return $Salida
    }

    & docker @Argumentos
    if ($LASTEXITCODE -ne 0) {
        throw "Docker finalizo con codigo $LASTEXITCODE."
    }
}

function Invoke-RedDiffCompose {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string[]]$Argumentos,
        [switch]$Capturar
    )

    $ArgumentosDocker = @(
        'compose',
        '--env-file', $script:RutaEntorno,
        '--file', $script:RutaCompose
    ) + $Argumentos

    if ($Capturar) {
        return Invoke-Docker -Argumentos $ArgumentosDocker -Capturar
    }

    Invoke-Docker -Argumentos $ArgumentosDocker
}

function Assert-DockerLinuxDisponible {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker no esta disponible en PATH. Instale e inicie Docker Desktop.'
    }

    $TipoMotor = @(Invoke-Docker -Argumentos @('info', '--format', '{{.OSType}}') -Capturar |
        Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) } |
        Select-Object -First 1)

    if ($TipoMotor.Count -eq 0 -or ([string] $TipoMotor[0]).Trim() -ne 'linux') {
        throw 'Docker Desktop debe estar iniciado en modo Linux containers.'
    }

    Invoke-Docker -Argumentos @('compose', 'version') | Out-Null
}

function Get-VariableEntorno {
    param([Parameter(Mandatory)][string]$Nombre)

    Assert-Archivo -Ruta $script:RutaEntorno
    $Patron = '^\s*' + [Regex]::Escape($Nombre) + '=(.*)$'
    $Valor = $null

    foreach ($Linea in [IO.File]::ReadAllLines($script:RutaEntorno)) {
        if ($Linea -match $Patron) {
            $Valor = $Matches[1].Trim()
        }
    }

    if ([string]::IsNullOrWhiteSpace($Valor)) {
        throw "La variable $Nombre no esta definida en $script:RutaEntorno."
    }

    return $Valor
}

function Set-VariableEntorno {
    param(
        [Parameter(Mandatory)][string]$Nombre,
        [Parameter(Mandatory)][string]$Valor
    )

    Assert-Archivo -Ruta $script:RutaEntorno
    $Lineas = [System.Collections.Generic.List[string]]::new()
    $Patron = '^\s*' + [Regex]::Escape($Nombre) + '='
    $Actualizada = $false

    foreach ($Linea in [IO.File]::ReadAllLines($script:RutaEntorno)) {
        if ($Linea -match $Patron) {
            $Lineas.Add("$Nombre=$Valor")
            $Actualizada = $true
        }
        else {
            $Lineas.Add($Linea)
        }
    }

    if (-not $Actualizada) {
        $Lineas.Add("$Nombre=$Valor")
    }

    [IO.File]::WriteAllLines($script:RutaEntorno, $Lineas, $script:CodificacionUtf8SinBom)
}

function New-SecretoAleatorio {
    param([int]$CantidadBytes = 48)

    $Bytes = New-Object byte[] $CantidadBytes
    $Generador = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $Generador.GetBytes($Bytes)
        return [Convert]::ToBase64String($Bytes)
    }
    finally {
        $Generador.Dispose()
        [Array]::Clear($Bytes, 0, $Bytes.Length)
    }
}

function Write-SecretoSiFalta {
    param([Parameter(Mandatory)][string]$Ruta)

    if (Test-Path -LiteralPath $Ruta -PathType Leaf) {
        return
    }

    $Secreto = New-SecretoAleatorio
    try {
        [IO.File]::WriteAllText($Ruta, $Secreto, $script:CodificacionUtf8SinBom)
    }
    finally {
        $Secreto = $null
    }
}

function Assert-ArchivosProduccion {
    Assert-Archivo -Ruta $script:RutaCompose
    Assert-Archivo -Ruta $script:RutaEntorno

    $Archivos = @(
        'postgresql_contrasena.txt',
        'postgresql_aplicacion_contrasena.txt',
        'autoridad_certificadora.crt',
        'postgresql_servidor.crt',
        'postgresql_servidor.key',
        'reddiff_https.crt',
        'reddiff_https.key',
        'administrador_inicial_contrasena.txt'
    )

    foreach ($Archivo in $Archivos) {
        Assert-Archivo -Ruta (Join-Path $script:RutaSecretos $Archivo)
    }
}

function Wait-ServicioSaludable {
    param(
        [Parameter(Mandatory)][string]$Servicio,
        [int]$Intentos = 60
    )

    $Contenedor = @(Invoke-RedDiffCompose -Argumentos @('ps', '--quiet', $Servicio) -Capturar |
        Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) } |
        Select-Object -First 1)

    if ($Contenedor.Count -eq 0) {
        throw "No se encontro el contenedor del servicio $Servicio."
    }

    $Id = ([string] $Contenedor[0]).Trim()
    for ($Intento = 1; $Intento -le $Intentos; $Intento++) {
        $Estado = @(Invoke-Docker -Argumentos @(
                'inspect',
                '--format', '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}',
                $Id
            ) -Capturar | Select-Object -First 1)

        $EstadoActual = if ($Estado.Count -gt 0) { ([string] $Estado[0]).Trim() } else { '' }
        switch ($EstadoActual) {
            'healthy' { return }
            'unhealthy' {
                Invoke-RedDiffCompose -Argumentos @('logs', '--tail', '100', $Servicio)
                throw "El servicio $Servicio alcanzo el estado unhealthy."
            }
            'exited' {
                Invoke-RedDiffCompose -Argumentos @('logs', '--tail', '100', $Servicio)
                throw "El servicio $Servicio se detuvo antes de quedar saludable."
            }
            'dead' {
                Invoke-RedDiffCompose -Argumentos @('logs', '--tail', '100', $Servicio)
                throw "El servicio $Servicio alcanzo el estado dead."
            }
        }

        Start-Sleep -Seconds 2
    }

    Invoke-RedDiffCompose -Argumentos @('logs', '--tail', '100', $Servicio)
    throw "El servicio $Servicio no alcanzo el estado saludable a tiempo."
}

function ConvertTo-TextoPlanoTemporal {
    param([Parameter(Mandatory)][Security.SecureString]$Valor)

    $Puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Valor)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($Puntero)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($Puntero)
    }
}
