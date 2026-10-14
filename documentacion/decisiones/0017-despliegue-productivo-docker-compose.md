# ADR 0017: Despliegue productivo con Docker Compose

- Estado: aceptada
- Fecha: 2026-10-13

## Contexto

RedDiff debe instalarse de forma repetible en un ambiente nuevo de LABYMED S.A. La solución necesita conservar versiones, claves de protección y datos de auditoría, recibir Syslog y conectarse a dispositivos mediante SSH o NETCONF sin exponer PostgreSQL.

## Decisión

- Se utilizará Docker Compose sobre un único servidor Linux.
- Nginx servirá Angular, terminará HTTPS y reenviará únicamente `/api` y `/salud`.
- La API y PostgreSQL permanecerán en redes privadas de Compose.
- PostgreSQL exigirá TLS con validación completa desde la API.
- La base de datos y el anillo de Data Protection utilizarán volúmenes independientes.
- Las migraciones se ejecutarán antes de iniciar la nueva versión de la API y con una identidad administrativa separada.
- La API conservará una cuenta de PostgreSQL con privilegios limitados.
- Los secretos se montarán como archivos y no formarán parte de las imágenes, variables versionadas o registros.
- Los respaldos incluirán la base de datos y las claves de protección correspondientes.

## Consecuencias

La instalación y las actualizaciones quedan concentradas en scripts repetibles, con verificaciones de salud y posibilidad de reconstruir una versión anterior. El servidor debe conservar copias externas de datos y claves, administrar certificados y disponer de conectividad controlada hacia los dispositivos.

La composición está orientada a una sola instancia. Alta disponibilidad, orquestación multinodo y actualización sin interrupción quedan fuera del alcance actual.
