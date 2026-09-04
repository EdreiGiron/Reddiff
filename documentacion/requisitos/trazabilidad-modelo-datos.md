# Trazabilidad del modelo de datos

Esta matriz relaciona la primera implementación del dominio con los requerimientos funcionales y no funcionales del documento del proyecto.

| Requisito | Entidades principales | Cobertura del modelo |
| --- | --- | --- |
| RF-01 | `usuario`, `rol`, `auditoria` | Cuenta activa, hash PBKDF2, sesión cifrada, validación de vigencia y auditoría de accesos. |
| RF-02 | `usuario`, `rol`, `auditoria` | API administrativa para crear, consultar, modificar, activar y desactivar cuentas y asignar roles. |
| RF-03 | `dispositivo` | Nombre, host, tipo, modelo, protocolo, puerto, fuente y autorización. |
| RF-04 | `captura`, `version_config` | Captura bajo demanda mediante SSH, NETCONF o archivo. |
| RF-05 | `evento_cambio`, `captura` | Evento deduplicable que puede originar una sola captura. |
| RF-06 | `version_config`, `captura`, `dispositivo` | Número, origen, fecha, contenido y huella SHA-256. |
| RF-07 | `version_config` | Índices por dispositivo, fecha, origen y estado. |
| RF-08 | `comparacion`, `detalle_diferencia` | Versiones relacionadas y detalle por línea. |
| RF-09 | `baseline`, `regla_baseline` | Referencia por dispositivo o tipo y sus reglas. |
| RF-10 | `verificacion`, `resultado_regla` | Estado general y resultado individual por regla. |
| RF-11 | `version_config`, `usuario` | Marca estable con usuario y fecha de validación. |
| RF-12 | `auditoria` | Usuario, acción, entidad, fecha, estado y detalle. |
| RF-13 | Entidades de resultados | Resultados independientes del contenido original. |

| Requisito no funcional | Decisión aplicada |
| --- | --- |
| RNF-01 | Cookie cifrada, autorización por roles, sesión limitada y revocación al desactivar usuario o rol. |
| RNF-02 | Hash PBKDF2 con sal aleatoria; cookies `HttpOnly`; protección CSRF, CORS exacto y límite de intentos. |
| RNF-03 | Huellas SHA-256, claves foráneas, índices únicos y metadatos UTC. |
| RNF-04 | Entidad de auditoría y referencias persistentes entre operaciones y resultados. |
| RNF-05 | Índices en fechas, estados y claves utilizadas por historial y resultados. |
| RNF-08 | Dominio sin dependencia de EF Core y configuraciones aisladas en Infraestructura. |
| RNF-10 | Tipo, fuente, huella, estado y dispositivo asociado para cada evento recibido. |

Las pruebas de dominio, aplicación e integración verifican la normalización de cuentas, la protección de contraseñas, el rechazo CSRF, la sesión, los roles, la auditoría y las restricciones administrativas.
