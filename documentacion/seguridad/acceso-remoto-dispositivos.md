# Seguridad del acceso remoto a dispositivos

## Alcance actual

El modelo, la migración incremental y la administración web establecen las condiciones de seguridad para capturas mediante SSH o NETCONF. Ambos protocolos utilizan adaptadores reales limitados a consultas de solo lectura.

La captura es exclusivamente de lectura. No existen operaciones para entrar en modo de configuración, aplicar cambios ni ejecutar comandos o RPC arbitrarios.

## Datos protegidos

Cada dispositivo puede conservar un único bloque de acceso remoto compuesto por:

- nombre de usuario técnico de solo lectura;
- secreto cifrado mediante ASP.NET Core Data Protection;
- algoritmo de la clave del host;
- huella SHA-256 de esa clave;
- fecha UTC de configuración del acceso.

Los cinco valores deben existir juntos o permanecer vacíos. La restricción `ck_dispositivo_acceso_remoto` llevará esta invariancia también a PostgreSQL.

El secreto no se almacena como texto legible ni se incorpora a respuestas, auditorías, registros o documentación. La protección utiliza el propósito aislado `RedDiff.Dispositivos.Credenciales.v1`, por lo que otro componente con un propósito distinto no puede reutilizar el dato protegido.

La consulta y las mutaciones están limitadas al rol `Administrador`. Las mutaciones requieren protección CSRF, vuelven a comprobar que la cuenta y su rol sigan activos y dejan una auditoría sin material sensible. Un reemplazo exige proporcionar un secreto nuevo; el valor existente no puede recuperarse desde el formulario.

## Confianza del equipo

La aplicación no aceptará automáticamente una clave desconocida presentada por el equipo. Antes de guardar el acceso, un administrador deberá obtener la huella SHA-256 por un canal confiable y aprobarla explícitamente. El contrato del conector exige comparar la clave recibida durante la negociación con ese valor antes de autenticar; una diferencia se trata como un conflicto de identidad y no genera una versión.

Cambiar el host, puerto o protocolo elimina el bloque completo. Revocar la autorización o desactivar el dispositivo también borra el acceso. De esa manera una credencial y una decisión de confianza no se reutilizan accidentalmente en otro punto de conexión.

## Gestión de claves de protección

Data Protection necesita conservar su anillo de claves fuera de la base de datos. El perfil local del proceso es suficiente para las pruebas de desarrollo. Antes de ejecutar la aplicación en contenedores o en más de una instancia se deberá configurar un repositorio persistente, respaldado y protegido por el sistema operativo o por un almacén de claves.

Perder ese anillo vuelve irrecuperables los secretos existentes. Copiarlo sin protección permitiría intentar descifrarlos. Su respaldo, rotación, permisos y recuperación deberán formar parte del procedimiento de despliegue antes de habilitar capturas remotas.

## Administración disponible

Desde **Dispositivos**, un administrador puede:

1. configurar el acceso de un equipo autorizado;
2. consultar únicamente el usuario, el algoritmo, la huella y la fecha de configuración;
3. reemplazar el bloque completo tras verificar nuevamente la huella;
4. revocar el acceso, borrando conjuntamente los cinco valores persistidos.

El formulario requiere una confirmación explícita de que la huella se obtuvo por un canal confiable. El secreto se limpia del control tanto después de una respuesta correcta como después de un error.

## Orquestación segura

La solicitud remota selecciona el conector por el protocolo configurado, descifra temporalmente el secreto, aplica un límite de 15 segundos y permite cancelar la operación. El contenido recibido se normaliza, limita y valida antes de reemplazar líneas sensibles y bloques de claves privadas por marcas seguras. Solo entonces se calcula la huella SHA-256 y se crea una versión inmutable. Los archivos manuales conservan el requisito más estricto de llegar previamente enmascarados.

Los fallos de conexión, autenticación, tiempo, identidad y contenido quedan representados mediante mensajes controlados. Las auditorías no almacenan excepciones de bibliotecas, secretos, configuraciones ni respuestas técnicas completas.

## Adaptador SSH habilitado

El adaptador SSH abre una conexión nueva para cada captura, compara el algoritmo y la huella SHA-256 de la clave pública antes de confiar en el equipo y después autentica al usuario técnico. Para admitir IOS 12.4 utiliza un flujo de terminal controlado que solo envía `terminal length 0` y `show running-config view full`; no recibe comandos del navegador, no ofrece consola, no entra en modo privilegiado y no modifica la configuración. Los errores del CLI se rechazan antes del saneamiento y nunca se aceptan como evidencia. El servidor sanea una respuesta válida antes de incorporarla al historial y no conserva el valor eliminado.

Las pruebas automatizadas reemplazan el adaptador por dobles controlados y nunca contactan la red. Una prueba del adaptador real requiere un laboratorio autorizado y el procedimiento de [captura SSH en laboratorio](../operacion/captura-ssh-laboratorio.md).

## Adaptador NETCONF habilitado

El adaptador NETCONF abre una sesión nueva sobre SSH, valida la misma identidad criptográfica aprobada y comprueba que el servidor anuncie una capacidad base NETCONF 1.0 o 1.1. La solicitud está fija en el código y contiene únicamente `<get-config>` con `<running/>`; la API y el navegador no pueden proporcionar RPC alternativos. No se implementan `<edit-config>`, `<copy-config>`, `<delete-config>` ni `commit`.

La respuesta debe incluir `<data>` y no puede contener `<rpc-error>`. Antes de pasar al procesamiento común, los elementos, atributos y fragmentos de configuración nativa que identifiquen contraseñas, secretos, comunidades o claves se sustituyen por `[PROTEGIDO]`. La validación automatizada no abre conexiones reales. La prueba controlada requiere un equipo compatible y el procedimiento de [captura NETCONF en laboratorio](../operacion/captura-netconf-laboratorio.md).

## Límites pendientes

Esta etapa no incluye:

- recepción de avisos SNMP o Syslog.

Los conectores simulados solo existen en el proyecto de pruebas; los adaptadores SSH y NETCONF reales se registran en la aplicación normal. Ningún secreto ni huella de producción debe utilizarse en pruebas: se debe trabajar con GNS3 o con un equipo expresamente autorizado.
