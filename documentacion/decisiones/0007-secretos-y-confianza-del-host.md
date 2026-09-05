# ADR 0007: Secretos protegidos y confianza explícita del host

- Estado: aceptada.
- Fecha: 2026-09-05.

## Contexto

RF-04 requiere capturas bajo demanda mediante SSH o NETCONF y RNF-02 prohíbe exponer credenciales. Una conexión cifrada no basta si el sistema acepta cualquier clave presentada por el equipo, porque podría entregar el secreto a un destino suplantado. El modelo debe resolver ambos riesgos antes de implementar conectores.

El diseño académico contiene trece entidades principales. Agregar una entidad técnica únicamente para credenciales rompería esa correspondencia sin aportar una relación de negocio nueva.

## Decisión

- Mantener las trece entidades y ampliar `dispositivo` con un bloque opcional e indivisible de acceso remoto.
- Proteger el secreto mediante ASP.NET Core Data Protection y un propósito exclusivo versionado.
- Conservar la huella SHA-256 y el algoritmo de la clave del host, aprobados fuera de la conexión inicial.
- Exigir que el dispositivo esté autorizado antes de configurar el acceso.
- Eliminar automáticamente todo el bloque cuando cambie el host, puerto o protocolo, o cuando se retire la autorización.
- Mantener los secretos fuera de contratos HTTP, auditorías y registros.
- Separar el modelo, la migración y los conectores en revisiones sucesivas.

## Consecuencias

La base de datos no contendrá contraseñas legibles y una alteración del texto protegido será rechazada. La confianza queda ligada al punto de conexión revisado, evitando aceptar silenciosamente una identidad distinta.

El anillo de claves de Data Protection pasa a ser un activo operativo: debe persistirse, protegerse y respaldarse en cada entorno. Una pérdida del anillo impide recuperar los secretos existentes y obliga a configurarlos nuevamente.

Esta decisión no habilita aún conexiones remotas. La siguiente migración deberá revisarse antes de modificar PostgreSQL y el conector posterior solo podrá realizar operaciones predefinidas de lectura.
