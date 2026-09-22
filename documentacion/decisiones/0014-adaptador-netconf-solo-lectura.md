# ADR 0014: Adaptador NETCONF de solo lectura

- Estado: aceptada.
- Fecha: 2026-09-22.

## Contexto

El inventario, la administración de credenciales y la orquestación remota ya distinguen SSH de NETCONF. Faltaba un adaptador real que utilizara el contrato existente sin permitir que el navegador proporcionara operaciones NETCONF ni convertir RedDiff en una herramienta de aprovisionamiento.

## Decisión

- Reutilizar `NetConfClient` de SSH.NET 2026.0.0; no incorporar una segunda biblioteca de transporte.
- Crear una conexión nueva por captura y no compartir sesiones, credenciales ni respuestas.
- Comparar el algoritmo y la huella SHA-256 de la clave del host antes de confiar en el servidor NETCONF.
- Exigir que el servidor anuncie NETCONF base 1.0 o 1.1 durante el intercambio de capacidades.
- Mantener un único RPC fijo en el adaptador: `<get-config>` sobre `<running/>`.
- No aceptar XML, filtros ni RPC desde la API o el cliente web.
- No implementar `<edit-config>`, `<copy-config>`, `<delete-config>`, `commit` ni ninguna operación que escriba en el equipo.
- Aplicar límites temporales a la conexión y al RPC, y traducir fallos de transporte a resultados controlados.
- Rechazar respuestas con `<rpc-error>`, sin `<data>` o sin contenido utilizable.
- Conservar como evidencia el elemento `<data>` normalizado, no el sobre `<rpc-reply>`.
- Enmascarar elementos, atributos y texto nativo que identifiquen secretos antes del procesamiento común, el cálculo de la huella y la persistencia.

## Consecuencias

Un dispositivo NETCONF autorizado puede generar versiones con origen `CapturaNetconf` mediante el mismo caso de uso empleado por SSH. La solución conserva el XML estructurado devuelto por el servidor, lo que permite comparaciones e inspección histórica sin ejecutar transformaciones dependientes de un fabricante.

La compatibilidad depende de que el sistema operativo del dispositivo implemente NETCONF sobre SSH. El Cisco 7200 con IOS 12.4 usado para validar SSH no es el objetivo de aceptación de esta etapa; el laboratorio NETCONF utiliza una imagen IOS XE compatible. La limitación de escritura se aplica en RedDiff mediante el RPC fijo y debe complementarse con AAA o controles del equipo cuando la plataforma los ofrezca.
