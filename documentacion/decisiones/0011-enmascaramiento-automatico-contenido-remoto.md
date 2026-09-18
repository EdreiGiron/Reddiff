# ADR 0011: Enmascaramiento automático del contenido remoto

- Estado: aceptada.
- Fecha: 2026-09-11.

## Contexto

Una configuración Cisco consultada mediante SSH suele incluir credenciales locales, secretos de modo privilegiado, comunidades SNMP o claves entre equipos. Rechazar toda la respuesta evita exponer esos valores, pero también impide registrar configuraciones legítimas obtenidas con una cuenta local de solo lectura.

La carga manual y la captura remota tienen límites de confianza distintos. El archivo proviene del operador y puede revisarse antes de enviarlo; el resultado remoto llega directamente del dispositivo y debe sanearse dentro del servidor antes de persistirse.

## Decisión

- Mantener el rechazo de archivos que contengan datos sensibles reconocibles sin enmascarar.
- Enmascarar automáticamente las líneas sensibles del resultado remoto antes de calcular su huella SHA-256.
- Sustituir la línea completa por una marca explícita y conservar su sangría, sin intentar guardar versiones cifradas o hashes del secreto.
- Eliminar completamente los bloques de claves privadas entre sus delimitadores de inicio y fin.
- Calcular la huella y crear la versión únicamente sobre el contenido ya saneado.
- Mantener el límite de tamaño y la validación de caracteres sobre la respuesta original.
- No copiar el valor eliminado a errores, respuestas HTTP, auditorías o registros.

## Consecuencias

La captura puede conservar la estructura no sensible de una configuración real sin almacenar secretos del dispositivo. Dos cambios que afecten exclusivamente un valor protegido no serán distinguibles en el historial, lo cual constituye una reducción deliberada de trazabilidad para evitar conservar material autenticador.

El reconocimiento de patrones es una defensa adicional y debe ampliarse cuando se incorporen nuevos fabricantes o sintaxis. Las pruebas reales continúan limitadas a GNS3 o equipos expresamente autorizados, y el operador debe revisar el contenido saneado antes de utilizarlo como evidencia.
