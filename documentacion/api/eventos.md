# Eventos de cambio

## Consulta autenticada

| Método y ruta      | Descripción                                                              |
| ------------------ | ------------------------------------------------------------------------ |
| `GET /api/eventos` | Lista los eventos visibles en orden descendente, sin contenido completo. |

Parámetros opcionales:

- `dispositivoId`: filtra por dispositivo;
- `estado`: admite `Recibido`, `Validado`, `Encolado`, `Procesado`, `SinCambios`, `Rechazado`, `Duplicado` o `Fallido`;
- `limite`: de 1 a 500; el valor predeterminado es 100.

La respuesta incluye fuente, tipo, fecha, huella SHA-256, estado, resumen saneado y referencias a la captura y versión cuando existen. Administradores y técnicos autenticados pueden consultarla.

## Recepción Syslog

Los eventos no ingresan por HTTP. Un servicio interno escucha UDP únicamente cuando `Eventos:Syslog:Habilitado` es verdadero. El receptor no confía en nombres declarados dentro del mensaje: utiliza la dirección IP de origen observada en el datagrama para localizar un único dispositivo autorizado. Debido a que UDP no autentica ese origen, el receptor debe operar en una red de gestión segmentada y con reglas de firewall restrictivas.

Un evento aceptado nunca suministra comandos. RedDiff ejecuta exclusivamente la operación de lectura fija del adaptador SSH o NETCONF ya configurado para el equipo. Después del saneamiento compara la huella de la configuración obtenida con la última versión del dispositivo. Si ambas coinciden, conserva el evento y la captura con estado completado, marca el evento como `SinCambios` y no crea una versión redundante.
