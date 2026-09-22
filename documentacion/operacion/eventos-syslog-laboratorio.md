# Eventos Syslog en laboratorio

Este procedimiento valida el flujo `Syslog → evento → captura SSH → versión` con el Cisco 7200 utilizado en las pruebas anteriores. No requiere IOS XE ni KVM.

## 1. Preparar RedDiff

En `configuracion\entornos\desarrollo.env` agregue o actualice:

```dotenv
REDDIFF_SYSLOG_HABILITADO=true
REDDIFF_SYSLOG_DIRECCION=0.0.0.0
REDDIFF_SYSLOG_PUERTO=5514
```

El puerto `5514` evita iniciar la API como administrador. Inicie infraestructura, API y Angular mediante los scripts habituales. Confirme el receptor desde otra terminal:

```powershell
Get-NetUDPEndpoint -LocalPort 5514 |
    Format-Table LocalAddress, LocalPort, OwningProcess
```

Si Windows Defender Firewall bloquea el tráfico, abra PowerShell como administrador y cree una regla limitada al perfil privado:

```powershell
New-NetFirewallRule `
    -DisplayName 'RedDiff Syslog UDP 5514' `
    -Direction Inbound `
    -Protocol UDP `
    -LocalPort 5514 `
    -Action Allow `
    -Profile Private
```

Al finalizar el laboratorio puede retirarla:

```powershell
Remove-NetFirewallRule -DisplayName 'RedDiff Syslog UDP 5514'
```

## 2. Preparar el inventario

Edite el dispositivo Cisco utilizado para SSH y confirme:

- host: `192.168.200.2`, o la dirección real de la interfaz que origina Syslog;
- estado: `Autorizado`;
- fuente de eventos: `Syslog`;
- acceso remoto: configurado y verificado;
- protocolo de captura: `SSH`.

El host del inventario debe coincidir exactamente con la IP de origen observada por RedDiff. No registre un nombre DNS para esta prueba.

## 3. Configurar Cisco IOS

En la consola administrativa del router, ajuste la IP de destino si la PC que ejecuta RedDiff utiliza otra dirección:

```text
RouterReal# configure terminal
RouterReal(config)# service timestamps log datetime msec
RouterReal(config)# logging host 192.168.200.1 transport udp port 5514
RouterReal(config)# logging trap notifications
RouterReal(config)# logging source-interface FastEthernet0/0
RouterReal(config)# archive
RouterReal(config-archive)# log config
RouterReal(config-archive-log-cfg)# logging enable
RouterReal(config-archive-log-cfg)# notify syslog contenttype plaintext
RouterReal(config-archive-log-cfg)# hidekeys
RouterReal(config-archive-log-cfg)# end
RouterReal# write memory
```

Si la imagen IOS no reconoce la variante `logging host`, utilice la ayuda contextual `logging ?` y conserve como requisitos la IP de RedDiff, UDP y el puerto `5514`.

## 4. Generar un cambio controlado

Utilice únicamente una modificación inocua del laboratorio:

```text
RouterReal# configure terminal
RouterReal(config)# banner motd ^CVALIDACION SYSLOG REDDIFF^C
RouterReal(config)# end
```

Espere la captura automática. En **Capturas y versiones → Eventos** compruebe:

1. tipo `Syslog`;
2. estado `Procesado`;
3. origen `192.168.200.2`;
4. captura asociada;
5. nueva versión con origen `SSH`;
6. comentario que indique que fue originada por un evento Syslog.

Genere después otro aviso que no cambie la configuración capturada. El nuevo evento y su consulta remota deben conservarse, pero el evento debe quedar como `SinCambios` y no debe aumentar el número de versiones. Si se envía exactamente el mismo datagrama, la huella del evento debe impedir incluso una segunda captura.

## Fallos esperados

- **No aparece el puerto UDP:** confirme que `REDDIFF_SYSLOG_HABILITADO=true` esté en el archivo local antes de iniciar la API.
- **Host no registrado:** verifique la dirección configurada con `logging source-interface` y el host del inventario.
- **Evento rechazado:** confirme autorización, fuente `Syslog` y acceso remoto configurado.
- **Evento fallido:** revise que la captura SSH manual continúe funcionando.
- **Evento `SinCambios`:** es un resultado válido; la configuración coincide con la última versión y no se crea otra.
- **No llega el datagrama:** compruebe ruta, interfaz de origen y firewall de Windows.

Los mensajes Syslog son avisos no confiables transportados por UDP. La evidencia principal sigue siendo la configuración capturada y su huella SHA-256.

En un entorno distinto del laboratorio, limite la dirección de escucha a la interfaz de gestión y restrinja el firewall a las IP autorizadas. UDP permite suplantar el origen y no aporta confidencialidad ni autenticación; por ello, RedDiff valida el inventario, sanea el contenido y jamás interpreta el evento como un comando, pero estas comprobaciones no sustituyen la segmentación de red.
