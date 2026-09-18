# ADR 0012: Comparación diferencial persistida

- Estado: aceptada.
- Fecha: 2026-09-18.

## Contexto

El historial permite consultar estados completos, pero revisar manualmente dos configuraciones extensas aumenta el tiempo necesario para localizar el cambio relacionado con un incidente. El modelo inicial ya dispone de `comparacion` y `detalle_diferencia`, y el documento académico delimita la función como evidencia de apoyo, no como calificación automática ni mecanismo de restauración.

## Decisión

- Comparar únicamente versiones existentes del mismo dispositivo.
- Tratar como significativos los espacios, la capitalización y el orden de las líneas.
- Clasificar el resultado en líneas agregadas, eliminadas o modificadas.
- Utilizar una biblioteca especializada de diferencias textuales en lugar de mantener un algoritmo propio sin validación equivalente.
- Persistir la ejecución y sus detalles para conservar trazabilidad.
- Exponer una vista lado a lado y un historial accesibles a `Administrador` y `Tecnico`.
- Auditar identificadores y cantidades sin copiar contenido de configuración.
- No incluir acciones de aplicación, restauración o envío de comandos.

## Consecuencias

El técnico puede concentrarse en las variaciones sin releer configuraciones completas y volver a consultar el mismo resultado. El almacenamiento crecerá de acuerdo con la cantidad de diferencias, no por una copia adicional de cada configuración. La utilidad del resultado depende de que las versiones de origen y destino se seleccionen en el orden correcto y de que el usuario interprete su contexto técnico.
