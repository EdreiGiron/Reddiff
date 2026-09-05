# Seguridad del acceso remoto a dispositivos

## Alcance de esta etapa

El modelo y su migración incremental preparan las condiciones de seguridad necesarias para una captura remota posterior mediante SSH o NETCONF. Todavía no abren conexiones, solicitan capturas ni exponen credenciales por API. La migración se aplica únicamente mediante el procedimiento confirmado y verificable de operación local.

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

## Confianza del equipo

La aplicación no aceptará automáticamente una clave desconocida presentada por el equipo. Antes de guardar el acceso, un administrador deberá obtener la huella SHA-256 por un canal confiable y aprobarla explícitamente. El futuro conector comparará la clave recibida durante la negociación con ese valor antes de autenticar.

Cambiar el host, puerto o protocolo elimina el bloque completo. Revocar la autorización o desactivar el dispositivo también borra el acceso. De esa manera una credencial y una decisión de confianza no se reutilizan accidentalmente en otro punto de conexión.

## Gestión de claves de protección

Data Protection necesita conservar su anillo de claves fuera de la base de datos. El perfil local del proceso es suficiente para las pruebas de desarrollo. Antes de ejecutar la aplicación en contenedores o en más de una instancia se deberá configurar un repositorio persistente, respaldado y protegido por el sistema operativo o por un almacén de claves.

Perder ese anillo vuelve irrecuperables los secretos existentes. Copiarlo sin protección permitiría intentar descifrarlos. Su respaldo, rotación, permisos y recuperación deberán formar parte del procedimiento de despliegue antes de habilitar capturas remotas.

## Límites pendientes

Esta etapa no incluye:

- pantalla o endpoint para configurar el acceso;
- bibliotecas o conectores SSH y NETCONF;
- ejecución de capturas contra equipos reales;
- recepción de avisos SNMP o Syslog.

Cada elemento se incorporará en un bloque posterior con pruebas separadas. Ningún secreto real ni huella de producción debe utilizarse durante las pruebas iniciales.
