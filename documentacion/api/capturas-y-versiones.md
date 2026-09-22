# Capturas y versiones de configuración

## Alcance implementado

Este contrato permite registrar un archivo de configuración asociado a un dispositivo autorizado, solicitar una captura remota y consultar la evidencia histórica resultante. Cada operación válida crea:

1. una `captura` bajo demanda con medio `Archivo`;
2. una `version_config` con número consecutivo por dispositivo;
3. una huella SHA-256 calculada sobre el contenido UTF-8 normalizado;
4. una auditoría sin copiar el contenido del archivo.

La operación nunca actualiza una versión existente. SSH utiliza una sesión controlada para desactivar la paginación y ejecutar `show running-config view full`. NETCONF negocia capacidades y envía exclusivamente `<get-config>` contra el almacén `running`. Ambos adaptadores están habilitados únicamente para dispositivos autorizados y configurados con el protocolo correspondiente.

## Reglas de la captura remota

- El usuario solicitante y su rol deben permanecer activos.
- El dispositivo debe estar autorizado y tener un bloque de acceso remoto completo.
- El conector debe coincidir exactamente con el protocolo configurado en el inventario.
- La solicitud entrega al conector la huella SHA-256 aprobada y exige que la identidad se compruebe antes de autenticar.
- La operación completa tiene un límite de 15 segundos y admite cancelación.
- El secreto se descifra únicamente dentro del alcance de la captura y nunca se incluye en resultados o auditorías.
- Una identidad de host distinta cancela la operación y no crea una versión.
- Una respuesta vacía, demasiado grande o binaria se registra como captura fallida y no se almacena como versión.
- Una respuesta con errores del CLI, incluido `invalid autocommand`, se registra como captura fallida y no se almacena como versión.
- Una respuesta NETCONF sin una capacidad base compatible, sin elemento `<data>` o con `<rpc-error>` se registra como captura fallida y no se almacena como versión.
- Una captura originada por evento cuya huella coincida con la última versión se conserva como consulta completada, pero no crea un número de versión redundante.
- Las líneas con credenciales, comunidades o claves reconocibles se sustituyen por una marca `[PROTEGIDO: ...]` antes de calcular la huella y persistir el contenido.
- Los elementos y atributos XML cuyo nombre identifica contraseñas, secretos, comunidades o claves se sustituyen por `[PROTEGIDO]` antes de calcular la huella.
- Un bloque de clave privada se elimina completo. Si el contenido no puede sanearse de forma segura, la captura falla y no crea una versión.
- El conector solo puede devolver contenido de configuración; el contrato no admite comandos proporcionados por el usuario.

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
| `POST /api/capturas/remota`                    | Recibe JSON con `dispositivoId`. Devuelve `201` para una captura SSH o NETCONF válida, o un error controlado.                         |
| `GET /api/capturas`                            | Lista solicitudes de captura. Admite `dispositivoId` y `estado`.                                                                      |
| `GET /api/versiones-configuracion`             | Lista metadatos sin contenido. Admite `dispositivoId`, `origen`, `estado`, `desde` y `hasta`.                                         |
| `GET /api/versiones-configuracion/{versionId}` | Devuelve metadatos y contenido preservado de una versión.                                                                             |

Los filtros de fecha usan valores ISO 8601 con zona horaria. Los valores de `origen` son `Archivo`, `CapturaSsh` y `CapturaNetconf`.

Las respuestas que contienen evidencia envían directivas para impedir su almacenamiento en caché. Los errores usan `ProblemDetails` y no devuelven el contenido recibido.

El enmascaramiento automático solo se utiliza con resultados obtenidos directamente por un conector remoto. Los archivos continúan requiriendo revisión y enmascaramiento previo por parte del operador.

## Permisos

| Operación                                | Administrador | Técnico |
| ---------------------------------------- | ------------: | ------: |
| Consultar capturas y versiones           |            Sí |      Sí |
| Consultar contenido preservado           |            Sí |      Sí |
| Cargar archivo en dispositivo autorizado |            Sí |      Sí |
| Solicitar captura remota autorizada      |            Sí |      Sí |
| Cargar en dispositivo no autorizado      |            No |      No |

No existe ninguna operación para enviar comandos arbitrarios ni aplicar configuraciones a los dispositivos.
