# ADR 0015: Recepción Syslog y captura deduplicada

## Estado

Aceptada.

## Contexto

El dominio ya conserva eventos de cambio, una huella por dispositivo y la relación uno a uno entre un evento y una captura. Faltaba recibir avisos reales sin exponer un endpoint público que permitiera solicitar capturas arbitrarias ni aceptar comandos enviados por el dispositivo.

## Decisión

- Incorporar un receptor Syslog UDP configurable y deshabilitado por defecto.
- Utilizar un puerto de laboratorio no privilegiado, `5514`, para evitar ejecutar la API con permisos elevados.
- Identificar el dispositivo únicamente mediante la dirección IP de origen del datagrama.
- Admitir el evento solo cuando el dispositivo esté autorizado, tenga `Syslog` como fuente y conserve un acceso remoto verificado.
- Normalizar, limitar a 8 KB y sanear el datagrama antes de calcular su huella SHA-256.
- Deduplicar por dispositivo y huella; un datagrama idéntico no genera otra captura.
- Reutilizar el adaptador SSH o NETCONF configurado para obtener una versión de solo lectura.
- Comparar la huella de la configuración saneada con la última versión del dispositivo antes de asignar un nuevo número. Una coincidencia conserva el evento y la captura como evidencia, pero no genera otra versión.
- No interpretar el texto Syslog como una orden ni permitir que determine el RPC o comando ejecutado.
- Permitir a administradores y técnicos consultar el historial sin exponer el contenido completo del datagrama.

## Consecuencias

Un evento anunciado mediante Syslog puede generar como máximo una captura y solo genera una versión cuando el contenido saneado cambia. Los eventos sin variación quedan identificados como `SinCambios`, evitando números de versión repetidos sin perder trazabilidad. Los hosts desconocidos, equipos no autorizados, eventos demasiado grandes o texto inválido se rechazan y se auditan sin crear evidencia operativa. UDP no garantiza entrega, por lo que este mecanismo complementa las capturas bajo demanda y no debe considerarse una cola confiable.
