# ADR 0010: Adaptador SSH de solo lectura

- Estado: aceptada.
- Fecha: 2026-09-10.

## Contexto

La orquestación remota ya separa transporte, validación del contenido y persistencia. Para habilitar SSH hace falta una biblioteca mantenida que admita cancelación, autenticación por contraseña y validación explícita de la clave del host sin incorporar una terminal genérica al contrato de la aplicación.

## Decisión

- Utilizar SSH.NET 2026.0.0, compatible de forma nativa con .NET 10.
- Crear una conexión nueva por captura y no compartir sesiones o credenciales entre solicitudes.
- Comparar el algoritmo y el SHA-256 hexadecimal de la clave pública antes de confiar en el host.
- Realizar la comparación de la huella en tiempo constante.
- Abrir un flujo de terminal controlado para compatibilidad con Cisco IOS 12.4, que rechaza el canal SSH de ejecución directa como un `autocommand` inválido.
- Enviar exclusivamente `terminal length 0` y `show running-config view full`. La primera orden solo desactiva la paginación de esa sesión y la segunda obtiene la configuración completa desde una CLI View.
- No aceptar comandos del cliente, exponer una consola, entrar en modo privilegiado ni modificar la configuración del dispositivo.
- Aplicar el límite temporal de la orquestación tanto a la conexión como al comando.
- Rechazar respuestas de error del CLI, incluido `invalid autocommand`, tanto en el adaptador como antes del saneamiento y la persistencia.
- Traducir las excepciones del transporte a fallos controlados sin devolver detalles de la biblioteca.
- Mantener los conectores simulados en las pruebas automatizadas para que estas nunca contacten la red.

## Consecuencias

Un dispositivo SSH autorizado y correctamente configurado puede producir una versión desde la aplicación web. Una clave distinta, autenticación rechazada, respuesta vacía o error del CLI impiden crear la versión y dejan evidencia de la captura fallida. Los datos sensibles reconocidos en una respuesta válida se eliminan antes de calcular la huella y almacenar el contenido, según la [ADR 0011](0011-enmascaramiento-automatico-contenido-remoto.md).

NETCONF se incorpora de forma separada en la [ADR 0014](0014-adaptador-netconf-solo-lectura.md). Los equipos Cisco IOS del laboratorio deben asignar al usuario técnico una CLI View que permita `terminal length 0` y `show running-config view full`, sin entrar en modo de configuración ni aceptar comandos de escritura. La configuración de la cuenta debe guardarse en `startup-config` para sobrevivir un reinicio del equipo virtual.
