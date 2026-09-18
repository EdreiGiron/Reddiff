# Trazabilidad del modelo de datos

Esta matriz relaciona la primera implementación del dominio con los requerimientos funcionales y no funcionales del documento del proyecto.

| Requisito | Entidades principales                      | Cobertura del modelo                                                                                                                                           |
| --------- | ------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| RF-01     | `usuario`, `rol`, `auditoria`              | Cuenta activa, hash PBKDF2, sesión cifrada, validación de vigencia y auditoría de accesos.                                                                     |
| RF-02     | `usuario`, `rol`, `auditoria`              | API administrativa para crear, consultar, modificar, activar y desactivar cuentas y asignar roles.                                                             |
| RF-03     | `dispositivo`, `auditoria`                 | API y cliente para consultar, registrar y editar nombre, host, tipo, modelo, protocolo, puerto y fuente; autorización y estados bajo control administrativo.   |
| RF-04     | `dispositivo`, `captura`, `version_config` | Carga de archivos y captura SSH real de solo lectura implementadas; NETCONF conserva el contrato seguro y permanece pendiente de su adaptador específico.       |
| RF-05     | `evento_cambio`, `captura`                 | Evento deduplicable que puede originar una sola captura.                                                                                                       |
| RF-06     | `version_config`, `captura`, `dispositivo` | Contenido local o remoto normalizado, número consecutivo, origen, fecha, comentario, contenido preservado y huella SHA-256 implementados.                       |
| RF-07     | `version_config`                           | API y cliente de historial con filtro por dispositivo; contrato adicional por fecha, origen y estado, respaldado por índices.                                  |
| RF-08     | `comparacion`, `detalle_diferencia`        | Versiones relacionadas y detalle por línea.                                                                                                                    |
| RF-09     | `baseline`, `regla_baseline`               | Referencia por dispositivo o tipo y sus reglas.                                                                                                                |
| RF-10     | `verificacion`, `resultado_regla`          | Estado general y resultado individual por regla.                                                                                                               |
| RF-11     | `version_config`, `usuario`                | Marca estable con usuario y fecha de validación.                                                                                                               |
| RF-12     | `auditoria`                                | Usuario, acción, entidad, fecha, estado y detalle.                                                                                                             |
| RF-13     | Entidades de resultados                    | Resultados independientes del contenido original.                                                                                                              |

| Requisito no funcional | Decisión aplicada                                                                                                                                          |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| RNF-01                 | Cookie cifrada, autorización por roles, sesión limitada y revocación al desactivar usuario o rol.                                                          |
| RNF-02                 | Hash PBKDF2 para cuentas; Data Protection y contratos sin exposición para secretos de equipos; cookies `HttpOnly`, CSRF, CORS exacto y límite de intentos. |
| RNF-03                 | Huellas SHA-256 sobre contenido canónico, versiones consecutivas, claves foráneas, índices únicos y metadatos UTC.                                         |
| RNF-04                 | Entidad de auditoría y referencias persistentes entre operaciones y resultados.                                                                            |
| RNF-05                 | Índices en fechas, estados y claves utilizadas por historial y resultados.                                                                                 |
| RNF-08                 | Dominio sin dependencia de EF Core y configuraciones aisladas en Infraestructura.                                                                          |
| RNF-10                 | Tipo, fuente, huella, estado y dispositivo asociado para cada evento recibido.                                                                             |

Las pruebas de dominio, aplicación e integración verifican la normalización de cuentas y hosts, la protección de contraseñas y secretos, la invalidación de confianza al cambiar un punto de conexión, el rechazo CSRF, la sesión, los roles, los canales admitidos, la auditoría sin secretos, las restricciones administrativas, el enmascaramiento exigido en archivos, el saneamiento automático de respuestas remotas y la preservación de versiones sucesivas. Los conectores simulados comprueban la orquestación de SSH o NETCONF sin contactar la red; pruebas puras adicionales verifican el algoritmo y la huella que el adaptador SSH exige antes de autenticar. Las pruebas del cliente verifican la captura SSH, que el técnico pueda consultar el inventario y operar el historial sin recibir controles administrativos, y que la administración del acceso no muestre ni conserve el secreto enviado.
