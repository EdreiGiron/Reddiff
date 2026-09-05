# Capturas y versiones de configuración

## Alcance implementado

Este contrato permite registrar un archivo de configuración asociado a un dispositivo autorizado y consultar la evidencia histórica resultante. Cada carga válida crea:

1. una `captura` bajo demanda con medio `Archivo`;
2. una `version_config` con número consecutivo por dispositivo;
3. una huella SHA-256 calculada sobre el contenido UTF-8 normalizado;
4. una auditoría sin copiar el contenido del archivo.

La carga nunca actualiza una versión existente. Las conexiones remotas de solo lectura mediante SSH o NETCONF se incorporarán cuando estén implementados el cifrado de credenciales de equipos y la validación explícita de su identidad criptográfica.

## Reglas del archivo

- El dispositivo debe existir y estar en estado `Autorizado`.
- Se admiten `.txt`, `.cfg`, `.conf` y `.config`.
- El tamaño máximo es de 5 000 000 bytes.
- El contenido debe ser UTF-8 de texto, sin caracteres de control binarios.
- Los finales de línea se normalizan a `LF` antes de almacenar y calcular la huella.
- El comentario es opcional y admite hasta 300 caracteres.
- Contraseñas, secretos, comunidades y claves reconocibles deben sustituirse por `[PROTEGIDO]`, `<PROTEGIDO>`, `***`, `REDACTED` o `ENMASCARADO` antes de cargar el archivo.

El filtro automático reduce exposiciones accidentales, pero no sustituye la revisión humana ni la anonimización del material de prueba.

## Endpoints

Todas las operaciones requieren una sesión activa. La carga requiere además el encabezado CSRF administrado por el cliente web.

| Método y ruta                                  | Resultado                                                                                                                             |
| ---------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `POST /api/capturas/archivo`                   | Recibe `multipart/form-data` con `DispositivoId`, `Archivo` y `Comentario` opcional. Devuelve `201` con la captura y versión creadas. |
| `GET /api/capturas`                            | Lista solicitudes de captura. Admite `dispositivoId` y `estado`.                                                                      |
| `GET /api/versiones-configuracion`             | Lista metadatos sin contenido. Admite `dispositivoId`, `origen`, `estado`, `desde` y `hasta`.                                         |
| `GET /api/versiones-configuracion/{versionId}` | Devuelve metadatos y contenido preservado de una versión.                                                                             |

Los filtros de fecha usan valores ISO 8601 con zona horaria. Los valores de `origen` son `Archivo`, `CapturaSsh` y `CapturaNetconf`; los dos últimos quedan reservados para el conector remoto posterior.

Las respuestas que contienen evidencia envían directivas para impedir su almacenamiento en caché. Los errores usan `ProblemDetails` y no devuelven el contenido recibido.

## Permisos

| Operación                                | Administrador | Técnico |
| ---------------------------------------- | ------------: | ------: |
| Consultar capturas y versiones           |            Sí |      Sí |
| Consultar contenido preservado           |            Sí |      Sí |
| Cargar archivo en dispositivo autorizado |            Sí |      Sí |
| Cargar en dispositivo no autorizado      |            No |      No |

No existe ninguna operación para enviar comandos arbitrarios ni aplicar configuraciones a los dispositivos.
