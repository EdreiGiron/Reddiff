[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9.-]+$')]
    [string] $NombreHost,

    [ValidateRange(1, 65535)]
    [int] $PuertoHttps = 443,

    [ValidateRange(1, 65535)]
    [int] $PuertoSyslog = 514,

    [ValidatePattern('^[A-Za-z_][A-Za-z0-9_]{0,62}$')]
    [string] $NombreBaseDatos = 'reddiff',

    [ValidatePattern('^[A-Za-z_][A-Za-z0-9_]{0,62}$')]
    [string] $UsuarioAdministradorBaseDatos = 'reddiff_admin',

    [ValidatePattern('^[A-Za-z_][A-Za-z0-9_]{0,62}$')]
    [string] $UsuarioAplicacionBaseDatos = 'reddiff_app',

    [string] $UsuarioAdministradorPostgreSql = 'postgres',

    [ValidateRange(1, 65535)]
    [int] $PuertoPostgreSql = 5432,

    [string] $RutaPsql,

    [string] $RutaInstalacion = (Join-Path $env:ProgramFiles 'RedDiff'),

    [string] $RutaDatos = (Join-Path $env:ProgramData 'RedDiff'),

    [string] $CertificadoThumbprint,

    [string] $OrigenSyslog = 'LocalSubnet',

    [switch] $OmitirFirewall,

    [switch] $Confirmar
)

. (Join-Path $PSScriptRoot 'Comun.ps1')

Confirmar-Administrador

if (-not $Confirmar) {
    throw 'La instalación modifica IIS, PostgreSQL y el firewall. Ejecute nuevamente con -Confirmar.'
}

$RutaPaquete = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$RutaAplicacionPaquete = Join-Path $RutaPaquete 'aplicacion'
$RutaAplicacion = Join-Path ([System.IO.Path]::GetFullPath($RutaInstalacion)) 'aplicacion'
$RutaDatos = [System.IO.Path]::GetFullPath($RutaDatos)
$RutaSecretos = Join-Path $RutaDatos 'secretos'
$RutaClaves = Join-Path $RutaDatos 'claves-proteccion'
$RutaRespaldos = Join-Path $RutaDatos 'respaldos'
$RutaCertificados = Join-Path $RutaDatos 'certificados'
$RutaSqlPreparacion = Join-Path $PSScriptRoot 'sql\Preparar-BaseDatos.sql'
$RutaSqlPrivilegios = Join-Path $PSScriptRoot 'sql\Configurar-Usuario-Aplicacion.sql'
$RutaSecretoAdminBd = Join-Path $RutaSecretos 'postgresql_admin_contrasena.txt'
$RutaSecretoAplicacionBd = Join-Path $RutaSecretos 'postgresql_aplicacion_contrasena.txt'
$RutaSecretoAdministradorInicial = Join-Path $RutaSecretos 'administrador_inicial_contrasena.txt'
$RutaConfiguracionAplicacion = Join-Path $RutaAplicacion 'appsettings.Production.json'
$RutaConfiguracionInstalacion = Join-Path $RutaDatos 'instalacion.json'
$OrigenHttps = Obtener-OrigenHttps -NombreHost $NombreHost -PuertoHttps $PuertoHttps
$ContrasenaSuperusuario = $null
$ContrasenaAdminBd = $null
$ContrasenaAplicacionBd = $null
$VariablesAnteriores = @{}

function Establecer-VariableTemporal {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Nombre,

        [AllowEmptyString()]
        [Parameter(Mandatory = $true)]
        [string] $Valor
    )

    if (-not $VariablesAnteriores.ContainsKey($Nombre)) {
        $VariablesAnteriores[$Nombre] = [Environment]::GetEnvironmentVariable($Nombre, 'Process')
    }

    [Environment]::SetEnvironmentVariable($Nombre, $Valor, 'Process')
}

function Restaurar-VariablesTemporales {
    foreach ($Nombre in $VariablesAnteriores.Keys) {
        [Environment]::SetEnvironmentVariable($Nombre, $VariablesAnteriores[$Nombre], 'Process')
    }
}

function Habilitar-Iis {
    if (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue) {
        $Resultado = Install-WindowsFeature `
            Web-Server, Web-Mgmt-Tools, Web-AppInit `
            -IncludeManagementTools
        if (-not $Resultado.Success) {
            throw 'Windows no pudo habilitar los componentes requeridos de IIS.'
        }
    }
    else {
        foreach ($Caracteristica in @(
            'IIS-WebServerRole',
            'IIS-WebServer',
            'IIS-ManagementConsole',
            'IIS-ApplicationInit')) {
            Enable-WindowsOptionalFeature `
                -Online `
                -FeatureName $Caracteristica `
                -All `
                -NoRestart `
                -ErrorAction Stop | Out-Null
        }
    }

    Import-Module WebAdministration -ErrorAction Stop
}

function Confirmar-ModuloAspNetCoreIis {
    $Candidatos = @(
        (Join-Path $env:ProgramFiles 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll'),
        (Join-Path ${env:ProgramFiles(x86)} 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll')
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    if (-not ($Candidatos | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf })) {
        throw @'
No se encontró el módulo ASP.NET Core para IIS.
Instale el Hosting Bundle de .NET 10 después de habilitar IIS, reinicie IIS y ejecute nuevamente este script.
Descarga oficial: https://dotnet.microsoft.com/download/dotnet/10.0
'@
    }
}

function Configurar-ReglaFirewall {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Nombre,

        [Parameter(Mandatory = $true)]
        [ValidateSet('TCP', 'UDP')]
        [string] $Protocolo,

        [Parameter(Mandatory = $true)]
        [int] $Puerto,

        [Parameter(Mandatory = $true)]
        [string] $DireccionRemota
    )

    Get-NetFirewallRule -DisplayName $Nombre -ErrorAction SilentlyContinue |
        Remove-NetFirewallRule -ErrorAction Stop

    New-NetFirewallRule `
        -DisplayName $Nombre `
        -Direction Inbound `
        -Action Allow `
        -Enabled True `
        -Profile Domain,Private `
        -Protocol $Protocolo `
        -LocalPort $Puerto `
        -RemoteAddress $DireccionRemota | Out-Null
}

if (-not [Environment]::Is64BitOperatingSystem) {
    throw 'RedDiff requiere Windows de 64 bits.'
}

$DireccionIp = $null
if ([Net.IPAddress]::TryParse($NombreHost, [ref]$DireccionIp)) {
    throw 'NombreHost debe ser un nombre DNS, no una dirección IP, para emitir y validar HTTPS correctamente.'
}

foreach ($Ruta in @(
    (Join-Path $RutaAplicacionPaquete 'RedDiff.Api.dll'),
    (Join-Path $RutaAplicacionPaquete 'wwwroot\index.html'),
    $RutaSqlPreparacion,
    $RutaSqlPrivilegios)) {
    if (-not (Test-Path -LiteralPath $Ruta -PathType Leaf)) {
        throw "El paquete está incompleto; no existe $Ruta."
    }
}

$RutaPsqlResuelta = Resolver-Psql -RutaPsql $RutaPsql
$VersionPostgreSql = Obtener-VersionMayorPostgreSql -RutaPsql $RutaPsqlResuelta

if ($VersionPostgreSql -eq 18 -and [Environment]::OSVersion.Version.Major -eq 10 `
    -and [Environment]::OSVersion.Version.Build -lt 20348) {
    throw 'PostgreSQL 18 requiere Windows 11 o Windows Server 2022/2025 en este procedimiento. Para Windows Server 2016 instale PostgreSQL 15.'
}

Habilitar-Iis
Confirmar-ModuloAspNetCoreIis

if (-not [Diagnostics.EventLog]::SourceExists('RedDiff')) {
    New-EventLog -LogName Application -Source RedDiff
}

$EntradaContrasena = Read-Host `
    "Contraseña del superusuario PostgreSQL '$UsuarioAdministradorPostgreSql'" `
    -AsSecureString
$ContrasenaSuperusuario = Convertir-SecureStringATexto -Valor $EntradaContrasena
if ([string]::IsNullOrWhiteSpace($ContrasenaSuperusuario)) {
    throw 'La contraseña del superusuario PostgreSQL no puede estar vacía.'
}

try {
    foreach ($Ruta in @(
        $RutaAplicacion,
        $RutaSecretos,
        $RutaClaves,
        $RutaRespaldos,
        $RutaCertificados)) {
        New-Item -ItemType Directory -Path $Ruta -Force | Out-Null
    }

    if (Test-Path "IIS:\Sites\$NombreSitioRedDiff") {
        Stop-Website -Name $NombreSitioRedDiff -ErrorAction SilentlyContinue
    }

    Copy-Item -Path (Join-Path $RutaAplicacionPaquete '*') -Destination $RutaAplicacion -Recurse -Force

    $ContrasenaAdminBd = Crear-SecretoAleatorio
    $ContrasenaAplicacionBd = Crear-SecretoAleatorio
    Escribir-TextoSinBom -Ruta $RutaSecretoAdminBd -Contenido $ContrasenaAdminBd
    Escribir-TextoSinBom -Ruta $RutaSecretoAplicacionBd -Contenido $ContrasenaAplicacionBd
    Escribir-TextoSinBom -Ruta $RutaSecretoAdministradorInicial -Contenido ''

    $Configuracion = [ordered]@{
        BaseDatos = [ordered]@{
            Servidor = '127.0.0.1'
            Puerto = $PuertoPostgreSql
            VersionMayorServidor = $VersionPostgreSql
            Nombre = $NombreBaseDatos
            Usuario = $UsuarioAplicacionBaseDatos
            ArchivoContrasena = $RutaSecretoAplicacionBd
            ModoSsl = 'Disable'
            PermitirConexionLocalSinTls = $true
            TiempoEsperaSegundos = 5
            TiempoComandoSegundos = 30
        }
        Seguridad = [ordered]@{
            OrigenCliente = $OrigenHttps
            ConfiarProxyInverso = $false
            DirectorioClavesProteccion = $RutaClaves
            DuracionSesionMinutos = 30
            LimiteIntentosInicioSesionPorMinuto = 10
        }
        Eventos = [ordered]@{
            Syslog = [ordered]@{
                Habilitado = $true
                DireccionEscucha = '0.0.0.0'
                Puerto = $PuertoSyslog
            }
        }
        AllowedHosts = $NombreHost
    }
    Escribir-TextoSinBom `
        -Ruta $RutaConfiguracionAplicacion `
        -Contenido ($Configuracion | ConvertTo-Json -Depth 8)

    Establecer-VariableTemporal -Nombre 'PGPASSWORD' -Valor $ContrasenaSuperusuario
    Establecer-VariableTemporal -Nombre 'REDDIFF_BD_NOMBRE' -Valor $NombreBaseDatos
    Establecer-VariableTemporal -Nombre 'REDDIFF_BD_USUARIO_ADMIN' -Valor $UsuarioAdministradorBaseDatos
    Establecer-VariableTemporal -Nombre 'REDDIFF_BD_USUARIO_APLICACION' -Valor $UsuarioAplicacionBaseDatos
    Establecer-VariableTemporal -Nombre 'REDDIFF_BD_CONTRASENA_ADMIN' -Valor $ContrasenaAdminBd
    Establecer-VariableTemporal -Nombre 'REDDIFF_BD_CONTRASENA_APLICACION' -Valor $ContrasenaAplicacionBd

    $DireccionesPostgreSql = & $RutaPsqlResuelta `
        -X `
        --host=127.0.0.1 `
        "--port=$PuertoPostgreSql" `
        "--username=$UsuarioAdministradorPostgreSql" `
        --dbname=postgres `
        --tuples-only `
        --no-align `
        --command 'SHOW listen_addresses;'
    if ($LASTEXITCODE -ne 0) {
        throw 'No fue posible consultar listen_addresses de PostgreSQL.'
    }

    $DireccionesNoLocales = @((($DireccionesPostgreSql -join '').Trim()) -split ',') |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -notin @('localhost', '127.0.0.1', '::1') }
    if ($DireccionesNoLocales.Count -gt 0) {
        throw "PostgreSQL escucha direcciones no locales ($($DireccionesNoLocales -join ', ')). Restrinja listen_addresses antes de instalar RedDiff."
    }

    Write-Host 'Preparando identidades y base de datos PostgreSQL...' -ForegroundColor Cyan
    Invocar-ComandoNativo -Comando $RutaPsqlResuelta -Argumentos @(
        '-X',
        '--host=127.0.0.1',
        "--port=$PuertoPostgreSql",
        "--username=$UsuarioAdministradorPostgreSql",
        '--dbname=postgres',
        '--file', $RutaSqlPreparacion
    ) -Descripcion 'La preparación de PostgreSQL'

    Establecer-VariableTemporal -Nombre 'ASPNETCORE_ENVIRONMENT' -Valor 'Production'
    Establecer-VariableTemporal -Nombre 'BaseDatos__Servidor' -Valor '127.0.0.1'
    Establecer-VariableTemporal -Nombre 'BaseDatos__Puerto' -Valor $PuertoPostgreSql.ToString()
    Establecer-VariableTemporal -Nombre 'BaseDatos__VersionMayorServidor' -Valor $VersionPostgreSql.ToString()
    Establecer-VariableTemporal -Nombre 'BaseDatos__Nombre' -Valor $NombreBaseDatos
    Establecer-VariableTemporal -Nombre 'BaseDatos__Usuario' -Valor $UsuarioAdministradorBaseDatos
    Establecer-VariableTemporal -Nombre 'BaseDatos__ArchivoContrasena' -Valor $RutaSecretoAdminBd
    Establecer-VariableTemporal -Nombre 'BaseDatos__ModoSsl' -Valor 'Disable'
    Establecer-VariableTemporal -Nombre 'BaseDatos__PermitirConexionLocalSinTls' -Valor 'true'
    Establecer-VariableTemporal -Nombre 'Seguridad__OrigenCliente' -Valor $OrigenHttps
    Establecer-VariableTemporal -Nombre 'Seguridad__DirectorioClavesProteccion' -Valor $RutaClaves
    Establecer-VariableTemporal -Nombre 'Eventos__Syslog__Habilitado' -Valor 'false'

    Write-Host 'Aplicando migraciones de RedDiff...' -ForegroundColor Cyan
    Push-Location $RutaAplicacion
    try {
        Invocar-ComandoNativo -Comando (Join-Path $RutaAplicacion 'RedDiff.Api.exe') `
            -Argumentos @('--migrar-base-datos') `
            -Descripcion 'La migración de RedDiff'
    }
    finally {
        Pop-Location
    }

    Establecer-VariableTemporal -Nombre 'PGPASSWORD' -Valor $ContrasenaAdminBd
    Write-Host 'Aplicando privilegios mínimos a la cuenta de aplicación...' -ForegroundColor Cyan
    Invocar-ComandoNativo -Comando $RutaPsqlResuelta -Argumentos @(
        '-X',
        '--host=127.0.0.1',
        "--port=$PuertoPostgreSql",
        "--username=$UsuarioAdministradorBaseDatos",
        "--dbname=$NombreBaseDatos",
        '--file', $RutaSqlPrivilegios
    ) -Descripcion 'La configuración de privilegios PostgreSQL'

    if (-not (Test-Path "IIS:\AppPools\$NombrePoolRedDiff")) {
        New-WebAppPool -Name $NombrePoolRedDiff | Out-Null
    }

    Set-ItemProperty "IIS:\AppPools\$NombrePoolRedDiff" -Name managedRuntimeVersion -Value ''
    Set-ItemProperty "IIS:\AppPools\$NombrePoolRedDiff" -Name managedPipelineMode -Value Integrated
    Set-ItemProperty "IIS:\AppPools\$NombrePoolRedDiff" -Name startMode -Value AlwaysRunning
    Set-ItemProperty "IIS:\AppPools\$NombrePoolRedDiff" -Name processModel.identityType -Value ApplicationPoolIdentity
    Set-ItemProperty "IIS:\AppPools\$NombrePoolRedDiff" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
    Set-ItemProperty "IIS:\AppPools\$NombrePoolRedDiff" -Name processModel.maxProcesses -Value 1
    Set-ItemProperty "IIS:\AppPools\$NombrePoolRedDiff" -Name recycling.periodicRestart.time -Value ([TimeSpan]::Zero)
    Set-ItemProperty "IIS:\AppPools\$NombrePoolRedDiff" -Name recycling.disallowOverlappingRotation -Value $true

    Establecer-AclRedDiff -Ruta ([System.IO.Path]::GetFullPath($RutaInstalacion)) -Nivel Lectura
    Establecer-AclRedDiff -Ruta $RutaDatos -Nivel Lectura
    Establecer-AclRedDiff -Ruta $RutaClaves -Nivel Modificacion
    Invocar-ComandoNativo -Comando 'icacls.exe' -Argumentos @(
        $RutaSecretoAdminBd,
        '/inheritance:r',
        '/grant:r',
        'SYSTEM:F',
        'BUILTIN\Administrators:F'
    ) -Descripcion 'No fue posible proteger la contraseña administrativa de PostgreSQL'
    Invocar-ComandoNativo -Comando 'icacls.exe' -Argumentos @(
        $RutaSecretoAdministradorInicial,
        '/inheritance:r',
        '/grant:r',
        'SYSTEM:F',
        'BUILTIN\Administrators:F'
    ) -Descripcion 'No fue posible proteger el secreto de inicialización'
    Invocar-ComandoNativo -Comando 'icacls.exe' -Argumentos @(
        $RutaSecretoAplicacionBd,
        '/inheritance:r',
        '/grant:r',
        'SYSTEM:F',
        'BUILTIN\Administrators:F',
        "IIS AppPool\${NombrePoolRedDiff}:R"
    ) -Descripcion 'No fue posible proteger la contraseña de aplicación de PostgreSQL'

    if ([string]::IsNullOrWhiteSpace($CertificadoThumbprint)) {
        $Certificado = New-SelfSignedCertificate `
            -DnsName $NombreHost `
            -CertStoreLocation 'Cert:\LocalMachine\My' `
            -FriendlyName 'RedDiff HTTPS' `
            -NotAfter (Get-Date).AddDays(825) `
            -KeyAlgorithm RSA `
            -KeyLength 3072 `
            -HashAlgorithm SHA256
        $CertificadoThumbprint = $Certificado.Thumbprint

        $RutaCertificadoPublico = Join-Path $RutaCertificados 'reddiff_https.cer'
        Export-Certificate -Cert $Certificado -FilePath $RutaCertificadoPublico -Force | Out-Null
        Import-Certificate -FilePath $RutaCertificadoPublico -CertStoreLocation 'Cert:\LocalMachine\Root' | Out-Null
    }
    else {
        $CertificadoThumbprint = $CertificadoThumbprint.Replace(' ', '').ToUpperInvariant()
        if (-not (Test-Path "Cert:\LocalMachine\My\$CertificadoThumbprint")) {
            throw "No existe el certificado $CertificadoThumbprint en LocalMachine\My."
        }
    }

    if (Test-Path "IIS:\Sites\$NombreSitioRedDiff") {
        Set-ItemProperty "IIS:\Sites\$NombreSitioRedDiff" -Name physicalPath -Value $RutaAplicacion
        Set-ItemProperty "IIS:\Sites\$NombreSitioRedDiff" -Name applicationPool -Value $NombrePoolRedDiff
        Get-WebBinding -Name $NombreSitioRedDiff | Remove-WebBinding
    }
    else {
        New-Website `
            -Name $NombreSitioRedDiff `
            -PhysicalPath $RutaAplicacion `
            -ApplicationPool $NombrePoolRedDiff `
            -Port $PuertoHttps `
            -HostHeader $NombreHost `
            -Ssl | Out-Null
        Get-WebBinding -Name $NombreSitioRedDiff | Remove-WebBinding
    }

    New-WebBinding `
        -Name $NombreSitioRedDiff `
        -Protocol https `
        -Port $PuertoHttps `
        -HostHeader $NombreHost `
        -SslFlags 1
    (Get-WebBinding -Name $NombreSitioRedDiff -Protocol https).AddSslCertificate(
        $CertificadoThumbprint,
        'My')

    Set-WebConfigurationProperty `
        -PSPath 'MACHINE/WEBROOT/APPHOST' `
        -Filter "system.applicationHost/sites/site[@name='$NombreSitioRedDiff']/application[@path='/']" `
        -Name preloadEnabled `
        -Value $true

    if (-not $OmitirFirewall) {
        Configurar-ReglaFirewall `
            -Nombre 'RedDiff - HTTPS' `
            -Protocolo TCP `
            -Puerto $PuertoHttps `
            -DireccionRemota 'Any'
        Configurar-ReglaFirewall `
            -Nombre 'RedDiff - Syslog' `
            -Protocolo UDP `
            -Puerto $PuertoSyslog `
            -DireccionRemota $OrigenSyslog
    }

    $ConfiguracionInstalacion = [ordered]@{
        NombreHost = $NombreHost
        PuertoHttps = $PuertoHttps
        PuertoSyslog = $PuertoSyslog
        NombreBaseDatos = $NombreBaseDatos
        UsuarioAdministradorBaseDatos = $UsuarioAdministradorBaseDatos
        UsuarioAplicacionBaseDatos = $UsuarioAplicacionBaseDatos
        PuertoPostgreSql = $PuertoPostgreSql
        VersionPostgreSql = $VersionPostgreSql
        RutaPsql = $RutaPsqlResuelta
        RutaInstalacion = [System.IO.Path]::GetFullPath($RutaInstalacion)
        RutaAplicacion = $RutaAplicacion
        RutaDatos = $RutaDatos
        CertificadoThumbprint = $CertificadoThumbprint
        InstaladoUtc = [DateTime]::UtcNow.ToString('O')
    }
    Escribir-TextoSinBom `
        -Ruta $RutaConfiguracionInstalacion `
        -Contenido ($ConfiguracionInstalacion | ConvertTo-Json -Depth 5)

    Start-WebAppPool -Name $NombrePoolRedDiff -ErrorAction SilentlyContinue
    Start-Website -Name $NombreSitioRedDiff

    Write-Host 'RedDiff se instaló correctamente en Windows.' -ForegroundColor Green
    Write-Host "Dirección: $OrigenHttps"
    Write-Host 'Ejecute Inicializar-Administrador.ps1 y después Verificar-RedDiff.ps1.'
    if ([string]::IsNullOrWhiteSpace($CertificadoThumbprint) -eq $false) {
        Write-Host "Certificado HTTPS: $CertificadoThumbprint"
    }
}
finally {
    Restaurar-VariablesTemporales
    $EntradaContrasena = $null
    $ContrasenaSuperusuario = $null
    $ContrasenaAdminBd = $null
    $ContrasenaAplicacionBd = $null
}
