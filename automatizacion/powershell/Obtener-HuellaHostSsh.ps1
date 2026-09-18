[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9.:-]{0,252}$')]
    [string] $Destino,

    [ValidateRange(1, 65535)]
    [int] $Puerto = 22,

    [ValidateRange(1, 60)]
    [int] $TiempoEsperaSegundos = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ComandoKeyscan = Get-Command 'ssh-keyscan.exe' -ErrorAction SilentlyContinue
if ($null -eq $ComandoKeyscan) {
    throw 'No se encontró ssh-keyscan.exe. Instale la característica Cliente OpenSSH de Windows.'
}

$ConfiguracionProceso = New-Object System.Diagnostics.ProcessStartInfo
$ConfiguracionProceso.FileName = $ComandoKeyscan.Source
$ConfiguracionProceso.Arguments = "-T $TiempoEsperaSegundos -p $Puerto $Destino"
$ConfiguracionProceso.UseShellExecute = $false
$ConfiguracionProceso.RedirectStandardOutput = $true
$ConfiguracionProceso.RedirectStandardError = $true
$ConfiguracionProceso.CreateNoWindow = $true

$Proceso = New-Object System.Diagnostics.Process
$Proceso.StartInfo = $ConfiguracionProceso

try {
    [void] $Proceso.Start()
    $TareaSalida = $Proceso.StandardOutput.ReadToEndAsync()
    $TareaError = $Proceso.StandardError.ReadToEndAsync()
    $Proceso.WaitForExit()

    $SalidaEstandar = $TareaSalida.GetAwaiter().GetResult()
    [void] $TareaError.GetAwaiter().GetResult()
    $CodigoSalida = $Proceso.ExitCode
}
finally {
    $Proceso.Dispose()
}

$Lineas = @(
    $SalidaEstandar -split "`r?`n" |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)

if ($CodigoSalida -ne 0 -or $Lineas.Count -eq 0) {
    throw "No fue posible obtener una clave pública SSH de ${Destino}:$Puerto."
}

$Resultados = @(
    foreach ($Linea in $Lineas) {
        if ([string]::IsNullOrWhiteSpace($Linea) -or $Linea.StartsWith('#')) {
            continue
        }

        $Partes = $Linea -split '\s+'
        if ($Partes.Count -lt 3) {
            continue
        }

        try {
            $ClavePublica = [Convert]::FromBase64String($Partes[2])
        }
        catch [FormatException] {
            continue
        }

        $Calculador = [Security.Cryptography.SHA256]::Create()
        try {
            $HuellaBytes = $Calculador.ComputeHash($ClavePublica)
            $HuellaHexadecimal = [BitConverter]::ToString($HuellaBytes).Replace('-', '').ToLowerInvariant()
        }
        finally {
            $Calculador.Dispose()
        }

        [PSCustomObject]@{
            Destino          = $Destino
            Puerto           = $Puerto
            Algoritmo        = $Partes[1]
            HuellaSha256Hex  = $HuellaHexadecimal
        }
    }
)

if ($Resultados.Count -eq 0) {
    throw 'La respuesta de ssh-keyscan no contenía una clave pública reconocible.'
}

Write-Warning 'La huella obtenida es candidata. Verifíquela por un canal confiable antes de aprobarla en RedDiff.'
$Resultados | Sort-Object Algoritmo
