# Captura SSH en laboratorio

## Alcance

Este procedimiento comprueba la captura de una configuración Cisco mediante SSH desde RedDiff. Utilice únicamente GNS3 o un dispositivo expresamente autorizado. El adaptador utiliza una sesión controlada para ejecutar `terminal length 0` y `show running-config view full`; no ofrece consola, modo de configuración ni envío de comandos personalizados.

NETCONF se valida mediante un procedimiento separado y requiere una imagen compatible con ese protocolo.

## Requisitos previos

1. PostgreSQL, la API y Angular deben estar activos.
2. El equipo debe ser alcanzable desde la computadora donde se ejecuta la API.
3. Debe existir un usuario asociado a una CLI View de solo lectura, autorizado para ejecutar `terminal length 0` y `show running-config view full`, sin acceso al modo de configuración.
4. El administrador debe comprobar la clave pública del equipo por un canal confiable.
5. Utilice únicamente credenciales de laboratorio. RedDiff eliminará líneas sensibles reconocibles antes de almacenar el resultado, pero este control no autoriza el uso de secretos de producción.

## Configurar y conservar la cuenta de lectura en IOS

Realice esta preparación desde la consola administrativa del router. Sustituya los marcadores por secretos exclusivos del laboratorio y no los copie en documentación, capturas de pantalla ni Git:

```text
RouterReal# enable view
Password: <SECRETO_ENABLE>
RouterReal# configure terminal
RouterReal(config)# parser view REDDIFF
RouterReal(config-view)# secret <SECRETO_DE_LA_VISTA>
RouterReal(config-view)# commands exec include terminal length 0
RouterReal(config-view)# commands exec include show running-config view full
RouterReal(config-view)# commands exec include show privilege
RouterReal(config-view)# commands exec include show parser view
RouterReal(config-view)# exit
RouterReal(config)# username reddiff_lector view REDDIFF secret <SECRETO_DEL_USUARIO>
RouterReal(config)# line vty 0 4
RouterReal(config-line)# login local
RouterReal(config-line)# transport input ssh
RouterReal(config-line)# end
RouterReal# copy running-config startup-config
Destination filename [startup-config]?
Building configuration...
[OK]
```

El último comando es obligatorio en GNS3: sin él, al detener o reiniciar el router se puede recuperar una `startup-config` anterior y desaparecerán la vista o el usuario. Compruebe la persistencia desde la consola administrativa:

```text
RouterReal# show startup-config | section parser view REDDIFF
RouterReal# show startup-config | include username reddiff_lector
```

Antes de configurar RedDiff, abra una sesión manual como `reddiff_lector` y confirme que `show privilege`, `show parser view`, `terminal length 0` y `show running-config view full` funcionan, mientras `configure terminal` es rechazado. No utilice como sustituto permanente una cuenta general como `reddiff`; primero restaure la cuenta de lectura y luego reemplace el acceso protegido en la aplicación.

La palabra `end` que aparece al final de `show running-config view full` pertenece a la configuración mostrada: no debe volver a escribirse en el prompt. La salida correcta termina con `end` y, en la línea siguiente, regresa automáticamente a `RouterReal>`. RedDiff envía cada orden con un único retorno de carro, equivalente a pulsar Enter una vez, para impedir que quede un prompt vacío pendiente entre ambas órdenes.

## Obtener una huella candidata

Desde la raíz del repositorio ejecute:

```powershell
.\automatizacion\powershell\Obtener-HuellaHostSsh.ps1 `
    -Destino 192.0.2.10 `
    -Puerto 22
```

Reemplace `192.0.2.10` por la dirección exclusiva del laboratorio. El script utiliza `ssh-keyscan.exe` y devuelve uno o más algoritmos con su SHA-256 hexadecimal de 64 caracteres.

La consulta solo obtiene una candidata y no demuestra por sí misma la identidad del equipo. Compare el algoritmo y la huella con la consola del dispositivo, el administrador de red u otro canal independiente antes de marcar la confirmación en RedDiff.

## Preparar el inventario

1. Inicie sesión como `Administrador`.
2. Abra **Dispositivos** y registre el host, puerto y protocolo `SSH` del laboratorio.
3. Autorice el dispositivo después de revisar sus datos.
4. Seleccione **Configurar acceso**.
5. Ingrese el usuario de consulta, su secreto, el algoritmo y la huella verificada.
6. Confirme la huella y guarde. RedDiff no volverá a mostrar el secreto.

Si el acceso ya estaba configurado con otra cuenta, utilice **Reemplazar acceso** y compruebe en el resumen del diálogo que el usuario actual sea exactamente `reddiff_lector` antes de iniciar la captura.

## Ejecutar la captura

1. Abra **Capturas**.
2. En **Captura remota**, seleccione el dispositivo SSH preparado.
3. Presione **Capturar configuración** una sola vez.
4. Espere el mensaje que indique el número de versión creado.
5. Abra la nueva versión y compruebe dispositivo, origen `SSH`, fecha, usuario solicitante, contenido y huella SHA-256.
6. Confirme que las líneas sensibles hayan sido sustituidas por `[PROTEGIDO: ...]` y que ningún valor autenticador aparezca en el detalle.
7. Revise la pestaña **Capturas** y confirme que el estado sea `Completada`.

Una segunda captura válida debe crear el siguiente número de versión sin reemplazar la evidencia anterior.

## Fallos esperados y revisión

- **Identidad criptográfica distinta:** vuelva a comprobar el equipo; no sustituya la huella hasta descartar una suplantación o un cambio no autorizado.
- **Credenciales rechazadas:** revise la cuenta directamente en el dispositivo y reemplace el acceso protegido desde RedDiff.
- **Tiempo agotado o conexión no disponible:** compruebe dirección, puerto, enrutamiento, ACL y servicio SSH.
- **Respuesta inválida:** confirme que la cuenta entre en la CLI View esperada y pueda ejecutar `terminal length 0` y `show running-config view full` sin interacción adicional. Textos como `invalid autocommand` son errores y nunca deben aparecer como una versión válida.
- **Dato que no pudo enmascararse:** no repita la captura hasta revisar la sintaxis. El sistema no creará una versión si no puede garantizar el saneamiento.

Los mensajes de la interfaz son deliberadamente controlados. Consulte la terminal de la API para disponibilidad general, pero no agregue registros que impriman credenciales o configuraciones completas.
