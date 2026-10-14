# Producción nativa en Windows

## Alcance

RedDiff puede instalarse sin contenedores en Windows Server o Windows 11. La variante utiliza IIS, ASP.NET Core 10 y PostgreSQL para Windows. Angular se compila previamente y queda incorporado en `wwwroot`, por lo que IIS publica una sola aplicación HTTPS y no se requiere Node.js en el servidor.

El receptor Syslog continúa dentro del proceso ASP.NET Core. El grupo de aplicaciones se configura con inicio permanente, precarga y sin tiempo de inactividad para evitar que IIS suspenda la recepción UDP.

## Matriz de compatibilidad

| Plataforma | PostgreSQL | Uso recomendado |
| --- | ---: | --- |
| Windows Server 2016 x64 | 15 | Compatibilidad temporal; planificar actualización del sistema operativo. |
| Windows 11 Pro/Enterprise x64 | 18 | Laboratorio o instalación interna controlada. |
| Windows Server 2022/2025 x64 | 18 | Producción recomendada. |

El servidor de base de datos queda restringido a loopback. RedDiff solamente acepta `ModoSsl=Disable` fuera de desarrollo cuando también se establece `PermitirConexionLocalSinTls=true` y el servidor es `localhost`, `127.0.0.1` o `::1`. Para una base remota sigue siendo obligatorio `VerifyFull` y una autoridad certificadora válida.

## Construcción del paquete

En un equipo de compilación con .NET SDK 10 y Node.js 24.19:

```powershell
.\automatizacion\powershell\produccion-windows\Crear-Paquete-Produccion-Windows.ps1
```

El script ejecuta las pruebas, compila Angular, publica ASP.NET Core para `win-x64`, incorpora los archivos web y crea:

```text
artifacts\windows\RedDiff-Windows-x64.zip
```

La compilación no contiene contraseñas, certificados ni configuración del ambiente final.

Después de cada validación correcta en GitHub, el trabajo **Paquete Windows** conserva el mismo ZIP como artefacto descargable durante 14 días.

## Instalación y operación

El procedimiento operativo completo se incorpora al paquete en `instalacion\README.md`. Los comandos principales son:

```powershell
.\Instalar-RedDiff.ps1 -NombreHost 'reddiff.labymed.local' -OrigenSyslog '192.168.200.0/24' -Confirmar
.\Inicializar-Administrador.ps1
.\Verificar-RedDiff.ps1
.\Respaldar-RedDiff.ps1
```

Los archivos persistentes se separan de los binarios:

| Ruta | Contenido |
| --- | --- |
| `C:\Program Files\RedDiff` | Aplicación publicada. |
| `C:\ProgramData\RedDiff\secretos` | Contraseñas con ACL restringidas. |
| `C:\ProgramData\RedDiff\claves-proteccion` | Anillo criptográfico de Data Protection. |
| `C:\ProgramData\RedDiff\respaldos` | Copias operativas. |

No debe eliminarse `claves-proteccion`: sin esas claves no pueden recuperarse las credenciales cifradas de los dispositivos.

## Seguridad

- IIS termina TLS y solo publica HTTPS.
- PostgreSQL escucha localmente; no se abre 5432 en el firewall.
- La identidad `IIS AppPool\RedDiff` solo recibe lectura del secreto de aplicación y modificación del directorio de claves.
- La contraseña administrativa de PostgreSQL permanece inaccesible para el grupo de aplicaciones.
- UDP 514 se restringe a la subred indicada durante la instalación.
- Los encabezados HSTS, CSP, protección de marcos, MIME y referencia se aplican desde ASP.NET Core.

Debe utilizarse un certificado emitido por la PKI corporativa cuando esté disponible. El certificado autofirmado incluido en el procedimiento solo facilita laboratorios y pruebas de aceptación.
