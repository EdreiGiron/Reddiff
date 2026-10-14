# RedDiff nativo para Windows

Este paquete instala RedDiff sin Docker mediante:

- IIS como servidor HTTPS y supervisor del proceso ASP.NET Core;
- ASP.NET Core 10 para la API, el receptor Syslog y el cliente Angular incorporado;
- PostgreSQL instalado como servicio de Windows;
- archivos de secretos protegidos mediante ACL de Windows;
- almacenamiento persistente de claves criptográficas y respaldos.

## Plataformas admitidas

| Sistema operativo | PostgreSQL recomendado | Observación |
| --- | ---: | --- |
| Windows 11 Pro o Enterprise x64 | 18 | Apropiado para demostración, laboratorio o servidor interno pequeño. |
| Windows Server 2016 x64 | 15 | Compatible, pero el soporte extendido de Windows finaliza en enero de 2027. |
| Windows Server 2022/2025 x64 | 18 | Plataforma recomendada para producción nueva. |

PostgreSQL y RedDiff deben instalarse en el mismo servidor. La aplicación solo permite la excepción sin TLS cuando la base de datos está en `127.0.0.1`, `localhost` o `::1`; PostgreSQL no debe publicar el puerto 5432 hacia la red.

## Requisitos previos

1. Instalar PostgreSQL para Windows y conservar la contraseña del usuario `postgres`:
   - PostgreSQL 15 en Windows Server 2016;
   - PostgreSQL 18 en Windows 11 o Windows Server 2022/2025.
2. Abrir PowerShell como administrador y habilitar IIS. El instalador también intenta habilitarlo automáticamente.
3. Instalar el **Hosting Bundle de .NET 10 después de IIS**:
   <https://dotnet.microsoft.com/download/dotnet/10.0>
4. Crear en DNS un registro para el servidor, por ejemplo `reddiff.labymed.local`.
5. Confirmar que TCP 443 y UDP 514 no estén ocupados.
6. Utilizar una cuenta con permisos de administrador local durante la instalación.

El servidor debe permitir salida TCP 22 y 830 hacia los dispositivos administrados. PostgreSQL y la aplicación no requieren Node.js, Visual Studio ni el SDK de .NET en el servidor final.

## Instalación

Descomprima el paquete en una ruta temporal, abra PowerShell como administrador y ejecute:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
Set-Location C:\Temporal\RedDiff-Windows-x64\instalacion

.\Instalar-RedDiff.ps1 `
    -NombreHost 'reddiff.labymed.local' `
    -OrigenSyslog '192.168.200.0/24' `
    -Confirmar
```

El script:

- valida PostgreSQL, IIS y el módulo ASP.NET Core;
- genera credenciales independientes para migraciones y para la aplicación;
- crea la base de datos y aplica las migraciones;
- limita los privilegios del usuario de aplicación;
- copia RedDiff a `C:\Program Files\RedDiff`;
- conserva datos, claves y respaldos en `C:\ProgramData\RedDiff`;
- crea el sitio y el grupo de aplicaciones IIS con inicio automático;
- configura HTTPS y reglas de firewall;
- restringe las ACL de secretos y claves.

Si `psql.exe` no se encuentra automáticamente, agregue:

```powershell
-RutaPsql 'C:\Program Files\PostgreSQL\15\bin\psql.exe'
```

### Certificado HTTPS

Sin parámetros adicionales, el instalador crea un certificado autofirmado y exporta su parte pública en:

```text
C:\ProgramData\RedDiff\certificados\reddiff_https.cer
```

Este certificado debe agregarse a las autoridades de confianza de cada equipo cliente. Para producción se recomienda suministrar un certificado corporativo ya instalado en `Cert:\LocalMachine\My`:

```powershell
.\Instalar-RedDiff.ps1 `
    -NombreHost 'reddiff.labymed.local' `
    -CertificadoThumbprint 'HUELLA_SIN_ESPACIOS' `
    -OrigenSyslog '192.168.200.0/24' `
    -Confirmar
```

## Primera cuenta

Ejecute una sola vez:

```powershell
.\Inicializar-Administrador.ps1
```

La contraseña no se muestra, debe contener entre 12 y 128 caracteres y el archivo temporal se vacía al terminar.

## Verificación

El nombre configurado debe resolver hacia el servidor. Después ejecute:

```powershell
.\Verificar-RedDiff.ps1
```

También puede abrir:

```text
https://reddiff.labymed.local
```

Para comprobar Syslog desde un dispositivo Cisco, configure como destino la IP del servidor y UDP 514. El firewall limita el origen a la red indicada mediante `-OrigenSyslog`.

## Operación

```powershell
.\Administrar-RedDiff.ps1 -Accion Estado
.\Administrar-RedDiff.ps1 -Accion Reiniciar
.\Administrar-RedDiff.ps1 -Accion Detener
.\Administrar-RedDiff.ps1 -Accion Iniciar
```

## Respaldo

```powershell
.\Respaldar-RedDiff.ps1
```

Cada respaldo contiene:

- `base-datos.dump` en formato personalizado de PostgreSQL;
- `claves-proteccion.zip`, indispensable para descifrar los accesos SSH/NETCONF;
- `manifiesto.sha256` para verificar integridad.

Copie los respaldos a un medio cifrado fuera del servidor. Una restauración debe recuperar juntos la base de datos y el mismo conjunto de claves.

## Actualización

Genere un paquete nuevo desde la versión de código aprobada, cópielo al servidor y ejecute desde su carpeta `instalacion`:

```powershell
.\Actualizar-RedDiff.ps1 -Confirmar
```

El procedimiento crea un respaldo previo, detiene IIS, reemplaza la aplicación, conserva la configuración y los secretos, aplica migraciones y verifica el servicio.

## Puertos

| Dirección | Puerto | Uso |
| --- | ---: | --- |
| Entrada | TCP 443 | Interfaz HTTPS |
| Entrada desde dispositivos | UDP 514 | Eventos Syslog |
| Salida hacia dispositivos | TCP 22 | Capturas SSH |
| Salida hacia dispositivos | TCP 830 | Capturas NETCONF |

No cree una regla de entrada para PostgreSQL 5432.
