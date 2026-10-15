# Produccion con Docker Compose en Windows 11

## Alcance

Este procedimiento despliega RedDiff en una PC fisica con Windows 11 mediante Docker Desktop y contenedores Linux. Todos los comandos operativos se ejecutan desde PowerShell. Windows no necesita IIS, PostgreSQL, .NET, Node.js, Angular CLI, Nginx ni OpenSSL instalados directamente.

La composicion incluye:

- Nginx con la compilacion productiva de Angular y HTTPS;
- la API ASP.NET Core sobre .NET 10;
- PostgreSQL 18 con TLS y volumen persistente;
- migraciones controladas y una cuenta de base de datos con privilegios limitados;
- inicializacion separada de la primera cuenta administrativa;
- recepcion Syslog en UDP 514;
- volumen persistente para las claves de proteccion de credenciales.

## Requisitos

- Windows 11 Pro o Enterprise x64 con soporte vigente;
- virtualizacion habilitada en BIOS o UEFI;
- WSL 2 actualizado;
- Docker Desktop configurado con el motor WSL 2 y **Linux containers**;
- Docker Compose incluido en Docker Desktop;
- Git para obtener y actualizar el repositorio;
- PowerShell 5.1 o PowerShell 7;
- al menos 4 nucleos, 8 GB de RAM y 80 GB disponibles como punto de partida;
- nombre DNS estable para RedDiff y hora sincronizada.

El perfil de red de Windows debe ser `Domain` o `Private`. Las reglas suministradas no habilitan RedDiff en perfiles publicos.

## Puertos

| Direccion | Puerto | Origen o destino | Uso |
| --- | ---: | --- | --- |
| Entrada | TCP 443 | Equipos administradores autorizados | Interfaz HTTPS |
| Entrada | UDP 514 | Dispositivos Cisco autorizados | Eventos Syslog |
| Salida | TCP 22 | Dispositivos registrados | Captura SSH |
| Salida | TCP 830 | Dispositivos registrados | Captura NETCONF |

PostgreSQL y el puerto HTTP de la API no se publican en Windows.

## 1. Preparar Windows

Instale WSL 2 y Docker Desktop, reinicie cuando Windows lo solicite y confirme en Docker Desktop:

1. **Use the WSL 2 based engine** habilitado.
2. Motor configurado en **Linux containers**.
3. Inicio automatico de Docker Desktop habilitado.

Abra PowerShell y ejecute desde la raiz del repositorio:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\automatizacion\powershell\produccion-docker\Verificar-RequisitosWindows.ps1
```

## 2. Preparar configuracion, secretos y certificados

Abra PowerShell **como administrador**. Sustituya las redes por los rangos reales autorizados:

```powershell
Set-Location C:\Proyectos\RedDiff
Set-ExecutionPolicy -Scope Process Bypass

.\automatizacion\powershell\produccion-docker\Preparar-Produccion.ps1 `
    -NombreHost 'reddiff.labymed.local' `
    -OrigenWeb '192.168.200.0/24' `
    -OrigenSyslog '192.168.200.0/24' `
    -ConfigurarHostWindows `
    -AgregarResolucionLocal
```

El script:

- crea `configuracion\entornos\produccion.env` sin incorporarlo a Git;
- genera contrasenas independientes para PostgreSQL;
- construye un contenedor auxiliar con OpenSSL para emitir la autoridad y los certificados;
- no reemplaza secretos o certificados existentes;
- limita mediante ACL el directorio de secretos;
- instala la autoridad interna en `LocalMachine\Root`;
- crea reglas de firewall restringidas a `Domain` y `Private`;
- agrega resolucion local en `hosts` cuando se solicita.

Si la generacion de certificados se interrumpe, el script conservara los archivos creados y rechazara un conjunto incompleto para evitar reemplazos accidentales. Retire ese conjunto solamente despues de confirmar que corresponde a una instalacion nueva y repita la preparacion.

Para acceso desde otros equipos, publique el nombre en el DNS interno e instale `autoridad_certificadora.crt` como autoridad confiable en cada cliente autorizado. En produccion puede reemplazarse el certificado web por uno emitido por la PKI corporativa.

## 3. Desplegar

```powershell
.\automatizacion\powershell\produccion-docker\Desplegar-Produccion.ps1
```

La operacion valida Compose, construye las imagenes, inicia PostgreSQL, aplica las migraciones, restablece privilegios limitados e inicia la API y Nginx. Se detiene si algun servicio no alcanza el estado saludable.

## 4. Crear la primera cuenta

Ejecute una sola vez:

```powershell
.\automatizacion\powershell\produccion-docker\Inicializar-Administrador.ps1
```

La contrasena se solicita sin mostrarla, se monta solo durante la operacion y el archivo temporal se vacia al finalizar.

## 5. Verificar

```powershell
.\automatizacion\powershell\produccion-docker\Verificar-Produccion.ps1
```

La comprobacion exige:

- PostgreSQL, API y web saludables;
- respuesta HTTP 200 de `/salud/listo` con certificado confiable;
- TCP 443 accesible localmente;
- UDP 514 publicado por Docker;
- PostgreSQL sin puerto publicado en Windows.

Abra `https://reddiff.labymed.local` y pruebe el inicio de sesion.

## 6. Validacion Syslog obligatoria

Configure un dispositivo autorizado para enviar Syslog a la direccion IPv4 de la PC Windows y al puerto UDP 514. Genere un cambio controlado y confirme en RedDiff:

1. el evento conserva la IP real del dispositivo;
2. un cambio de contenido crea una nueva version;
3. un segundo evento sin cambios queda como `SinCambios` y no duplica la version.

Esta prueba es obligatoria porque Docker Desktop incorpora una capa de red. Si la aplicacion recibe una direccion distinta a la del dispositivo, no reduzca la validacion de seguridad: documente el resultado y ajuste la red antes de autorizar produccion.

## 7. Respaldar

```powershell
.\automatizacion\powershell\produccion-docker\Respaldar-Produccion.ps1
```

El respaldo se crea en `respaldos\produccion` e incluye:

- `base-datos.dump`;
- `claves-proteccion.tar.gz`;
- `manifiesto.sha256`.

Copie cada respaldo a un medio cifrado externo. Los certificados y secretos del directorio `configuracion\secretos\produccion` requieren una copia protegida independiente. Perder la base, las claves de proteccion o las contrasenas impide una recuperacion completa.

## 8. Restaurar

La restauracion reemplaza los datos actuales. Realicela solamente durante una ventana autorizada y con un respaldo verificado:

```powershell
.\automatizacion\powershell\produccion-docker\Restaurar-Produccion.ps1 `
    -RutaRespaldo 'C:\Respaldos\reddiff-20261014T030000Z' `
    -Confirmar
```

El script valida el manifiesto, detiene web y API, restaura PostgreSQL y las claves, restablece privilegios, inicia los servicios y ejecuta la verificacion tecnica.

## 9. Actualizar

Con el repositorio limpio:

```powershell
.\automatizacion\powershell\produccion-docker\Actualizar-Produccion.ps1 -Confirmar
```

La operacion crea un respaldo, exige un arbol de trabajo limpio, ejecuta `git pull --ff-only`, reconstruye la composicion y verifica la nueva version. Use `-OmitirGitPull` cuando los archivos se entreguen por un medio distinto de Git.

## 10. Detener sin eliminar datos

```powershell
.\automatizacion\powershell\produccion-docker\Detener-Produccion.ps1
```

No ejecute `docker compose down --volumes`: eliminaria la base de datos y las claves de proteccion.

## Reinicios de Windows

Los servicios usan `restart: unless-stopped`. Cuando Docker Desktop vuelve a iniciar, Docker recupera los contenedores. Docker Desktop debe estar configurado para iniciar con la sesion de la cuenta operativa. Despues de cada reinicio ejecute:

```powershell
.\automatizacion\powershell\produccion-docker\Verificar-Produccion.ps1
```

## Criterios de aceptacion

- Windows 11 y Docker Desktop reinician sin errores.
- Todos los servicios aparecen `healthy`.
- HTTPS utiliza un certificado confiable y el nombre correcto.
- PostgreSQL no se publica en el host.
- SSH y NETCONF salen desde los contenedores hacia los dispositivos autorizados.
- Syslog conserva la direccion del dispositivo y activa captura deduplicada.
- El respaldo y una restauracion controlada finalizan correctamente.
- Secretos, archivos `.env`, configuraciones capturadas y respaldos no se incorporan a Git.
