# Seguridad del acceso remoto a dispositivos

## Alcance actual

El modelo, la migración incremental y la administración web preparan las condiciones de seguridad necesarias para capturas mediante SSH o NETCONF. La orquestación de la captura bajo demanda ya se encuentra implementada y validada mediante conectores simulados. El servidor normal todavía no registra adaptadores reales, por lo que no abre conexiones de red en esta etapa.

La captura posterior será exclusivamente de lectura. No se incorporarán operaciones para entrar en modo de configuración, aplicar cambios ni ejecutar comandos arbitrarios.

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

La solicitud remota selecciona el conector por el protocolo configurado, descifra temporalmente el secreto, aplica un límite de 15 segundos y permite cancelar la operación. El contenido recibido atraviesa las mismas reglas de normalización, tamaño e identificación de datos sensibles utilizadas para archivos. Solo después de aprobarlas se crea una versión inmutable con su huella SHA-256.

Los fallos de conexión, autenticación, tiempo, identidad y contenido quedan representados mediante mensajes controlados. Las auditorías no almacenan excepciones de bibliotecas, secretos, configuraciones ni respuestas técnicas completas.

## Límites pendientes

Esta etapa no incluye:

- adaptadores reales de SSH y NETCONF;
- ejecución de capturas contra equipos reales;
- recepción de avisos SNMP o Syslog.

Los conectores simulados solo existen en el proyecto de pruebas y no se registran en la aplicación normal. Los adaptadores reales se incorporarán en un bloque posterior con pruebas separadas. Ningún secreto real ni huella de producción debe utilizarse durante las pruebas iniciales.
