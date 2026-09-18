# ADR 0013: Líneas base con criterios inmutables

- Estado: aceptada.
- Fecha: 2026-09-18.

## Contexto

El historial y la comparación muestran qué configuración existía y qué cambió, pero no expresan las condiciones que una organización espera encontrar. El modelo persistente ya separa `baseline`, `regla_baseline`, `verificacion` y `resultado_regla`, y el documento académico solicita verificar configuraciones sin convertir el sistema en un mecanismo automático de certificación o remediación.

## Decisión

- Definir la línea base como un conjunto de criterios aplicable a un dispositivo o a un tipo de dispositivo.
- Mantener distinta la referencia histórica estable: es opcional y aporta contexto, pero no sustituye las reglas.
- Admitir criterios de presencia, ausencia y expresión regular con límites de longitud, cantidad y tiempo de evaluación.
- Hacer inmutables las reglas después de crear la línea base; los cambios de política se representan con una línea base nueva.
- Permitir que `Administrador` y `Tecnico` ejecuten y consulten verificaciones, pero reservar al administrador la creación y el cambio de vigencia.
- Determinar el resultado general con las reglas obligatorias y conservar también los resultados opcionales como evidencia.
- Evaluar solo versiones almacenadas y saneadas, sin conectarse al equipo.
- Auditar identificadores, cantidades y resultado general sin copiar la configuración completa ni valores sensibles.
- No incluir comandos, correcciones, restauración ni remediación automática.

## Consecuencias

La evidencia histórica conserva el criterio exacto aplicado en cada verificación y evita que una edición posterior reinterprete resultados anteriores. Cambiar una política requiere crear otra línea base y desactivar la anterior. El porcentaje facilita el resumen, pero no reemplaza la lectura del resultado general ni la revisión técnica de cada regla.
