# Captura NETCONF en laboratorio

## Alcance

Este procedimiento comprueba una captura de solo lectura mediante NETCONF sobre SSH. RedDiff negocia capacidades y envía únicamente `<get-config>` contra `running`; no admite RPC proporcionados por el usuario ni operaciones de escritura.

El router Cisco 7200 con IOS 12.4 empleado en las pruebas SSH debe conservarse para ese protocolo. Para NETCONF utilice una imagen autorizada de Cisco IOS XE 16.3.1 o posterior que incluya NETCONF/YANG. Cisco documenta la compatibilidad desde IOS XE 16.3.1 y el puerto SSH predeterminado 830 en su [guía de configuración NETCONF/YANG](https://www.cisco.com/c/en/us/support/docs/storage-networking/management/200933-YANG-NETCONF-Configuration-Validation.html).

## Requisitos previos

1. PostgreSQL, la API y Angular deben estar activos.
2. La API debe alcanzar la dirección del equipo por TCP 830.
3. El equipo debe anunciar una capacidad NETCONF base 1.0 o 1.1.
4. Debe existir una cuenta exclusiva del laboratorio autorizada para consultar la configuración.
5. El administrador debe obtener y comprobar la huella SHA-256 de la clave del host por un canal independiente.

## Preparar un equipo IOS XE autorizado

Los comandos exactos dependen de la imagen. En una imagen IOS XE compatible, habilite AAA, cree una cuenta exclusiva y active NETCONF desde la consola administrativa:

```text
RouterXE# configure terminal
RouterXE(config)# aaa new-model
RouterXE(config)# aaa authentication login default local
RouterXE(config)# aaa authorization exec default local
RouterXE(config)# username reddiff_netconf privilege 15 secret <SECRETO_EXCLUSIVO>
RouterXE(config)# netconf-yang
RouterXE(config)# end
RouterXE# copy running-config startup-config
```

Algunas plataformas exigen privilegio 15 para exponer los modelos. Esa cuenta solo debe existir en el laboratorio. RedDiff reduce el alcance al mantener el RPC de lectura fijo; en un despliegue real también se deben aplicar las restricciones AAA o NACM admitidas por el equipo.

Compruebe desde IOS XE que el servicio y el proceso YANG estén activos mediante los comandos disponibles en su versión, por ejemplo:

```text
RouterXE# show platform software yang-management process
RouterXE# show netconf-yang sessions
```

## Comprobar transporte y capacidades

Desde PowerShell confirme primero el puerto:

```powershell
$IpEquipo = '192.168.200.3'
Test-NetConnection -ComputerName $IpEquipo -Port 830
```

El resultado esperado es `TcpTestSucceeded : True`. Después solicite manualmente el subsistema NETCONF:

```powershell
ssh -p 830 -s "reddiff_netconf@$IpEquipo" netconf
```

Tras autenticar, el equipo debe devolver un mensaje `<hello>` con `<capabilities>`. Finalice la comprobación con `Ctrl+C`; no escriba RPC manuales con credenciales o información real.

## Obtener y comprobar la huella

El mismo script utilizado por SSH admite el puerto NETCONF:

```powershell
.\automatizacion\powershell\Obtener-HuellaHostSsh.ps1 `
    -Destino $IpEquipo `
    -Puerto 830
```

El script devuelve una huella candidata. Compare algoritmo y SHA-256 hexadecimal con la consola, el administrador de red u otro canal confiable antes de aprobarlos en RedDiff.

## Preparar el inventario y capturar

1. Inicie sesión como `Administrador`.
2. Registre el equipo con protocolo `NETCONF` y puerto `830`.
3. Autorice el dispositivo y seleccione **Configurar acceso**.
4. Ingrese `reddiff_netconf`, el secreto exclusivo, el algoritmo y la huella ya comprobada.
5. Confirme la huella y guarde el acceso.
6. Abra **Capturas** y, en **Captura remota**, seleccione el equipo NETCONF.
7. Compruebe que la nota muestre `<get-config>` y `running`; presione **Capturar configuración** una vez.
8. Abra la nueva versión y verifique origen `NETCONF`, contenido XML bajo `<data>`, huella SHA-256 y datos sensibles sustituidos por `[PROTEGIDO]`.
9. Confirme que la captura figure como `Completada` y que la evidencia anterior permanezca disponible.

## Fallos esperados

- **Puerto 830 cerrado:** revise `netconf-yang`, conectividad, ACL y la interfaz enlazada al laboratorio.
- **Identidad distinta:** detenga la prueba y compruebe el equipo; no reemplace la huella sin investigar el cambio.
- **Autenticación rechazada:** valide la cuenta en el equipo y reemplace el acceso protegido desde RedDiff.
- **Respuesta inválida:** confirme el intercambio `<hello>`, una capacidad base compatible y que `<get-config>` devuelva `<data>` sin `<rpc-error>`.
- **Tiempo agotado:** revise recursos de la imagen, transporte SSH y procesos YANG.
- **Dato no saneable:** no utilice la respuesta como evidencia; revise el modelo XML y amplíe las pruebas del saneador antes de admitir esa sintaxis.

Nunca utilice secretos ni configuraciones de producción para esta validación.
