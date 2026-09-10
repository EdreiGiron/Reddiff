# ADR 0009: Orquestación controlada de capturas remotas

- Estado: aceptada.
- Fecha: 2026-09-10.

## Contexto

El inventario ya conserva el protocolo, punto de conexión, secreto protegido y huella aprobada de cada dispositivo. Conectar inmediatamente una biblioteca externa mezclaría decisiones de seguridad, persistencia y particularidades de transporte sin haber demostrado primero el comportamiento esperado ante fallos.

## Decisión

- Definir el conector remoto como una abstracción de la capa de aplicación.
- Seleccionar exclusivamente el conector correspondiente al protocolo registrado en el dispositivo.
- Exigir al adaptador que valide algoritmo y huella de la clave del host antes de autenticar.
- Limitar cada operación a 15 segundos y propagar su cancelación.
- Descifrar el secreto solo después de aprobar todas las precondiciones y no incorporarlo a resultados o auditorías.
- Rechazar respuestas vacías, mayores de 5 MB, con caracteres no permitidos o datos sensibles reconocibles.
- Crear una versión únicamente cuando transporte y contenido sean válidos; conservar como fallida la captura iniciada si alguno falla.
- Habilitar conectores simulados solo dentro de las pruebas de integración.
- No aceptar comandos, filtros o cargas operativas proporcionados por el usuario.

## Consecuencias

La API y el caso de uso pueden probarse de extremo a extremo sin tocar la red. El servidor ejecutable responde de forma controlada cuando aún no existe un adaptador real, lo cual impide interpretar una simulación como funcionalidad productiva. Los conectores de SSH y NETCONF podrán implementarse y sustituirse sin cambiar las reglas de persistencia ni el contrato HTTP.

La reducción del riesgo no elimina la necesidad de una prueba posterior en GNS3 o con un dispositivo de laboratorio autorizado. Esa prueba deberá comprobar la identidad del host, privilegios de solo lectura, tiempo de espera, tamaño de respuesta y ausencia de modificaciones sobre el equipo.
