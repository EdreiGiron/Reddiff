# ADR 0008: Administración del acceso remoto sin exposición

- Estado: aceptada.
- Fecha: 2026-09-09.

## Contexto

El bloque de acceso remoto protegido ya existe en el dominio y en PostgreSQL. Antes de implementar conectores SSH o NETCONF, el administrador necesita un mecanismo controlado para configurarlo, reemplazarlo y revocarlo. El contrato no puede convertir el almacenamiento cifrado en un medio para recuperar credenciales desde el navegador.

## Decisión

- Limitar la consulta y las mutaciones del acceso remoto al rol `Administrador`.
- Devolver únicamente estado, usuario técnico, algoritmo, huella de la clave del host y fecha de configuración.
- Exigir un secreto nuevo y una confirmación explícita de la huella tanto al configurar como al reemplazar.
- Proteger el secreto antes de asignarlo a la entidad y no incluir el texto legible ni el valor protegido en respuestas o auditorías.
- Revocar los cinco valores del bloque en una sola operación.
- Mantener esta administración separada de cualquier intento de conexión o captura.

## Consecuencias

El navegador puede conocer si existe acceso y mostrar sus metadatos de confianza, pero no puede recuperar el secreto almacenado. Un reemplazo completo evita mezclar una credencial nueva con una decisión de confianza anterior. Cada mutación deja evidencia de quién la realizó y sobre qué dispositivo, sin conservar material sensible.

La validez de la credencial y la comparación efectiva de la clave del host solo podrán comprobarse al incorporar los conectores de lectura. Esa etapa deberá consumir el secreto durante el menor tiempo posible, limitar los comandos permitidos y registrar resultados sin contenido sensible.

El adaptador SSH incorporado posteriormente cumple esta condición con validación previa de la clave del host, un comando fijo y saneamiento del resultado antes de persistirlo.
