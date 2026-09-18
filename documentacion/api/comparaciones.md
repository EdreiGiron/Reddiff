# Comparación diferencial de configuraciones

## Alcance implementado

El módulo compara dos versiones del mismo dispositivo y conserva el resultado como evidencia. La operación clasifica cada cambio como línea `Agregada`, `Eliminada` o `Modificada`, pero no determina si el cambio es correcto, no ejecuta comandos y no altera el equipo.

Cada comparación válida crea:

1. una fila `comparacion` vinculada con las dos versiones y el usuario solicitante;
2. una colección ordenada de `detalle_diferencia` sin duplicar el contenido completo;
3. una auditoría con identificadores, cantidades y resultado, sin copiar líneas de configuración.

## Reglas

- Ambas versiones deben existir y ser diferentes.
- Las dos versiones deben pertenecer al mismo dispositivo.
- El usuario y su rol deben permanecer activos.
- Los espacios, la capitalización y el orden de las líneas forman parte del contenido y se comparan de manera exacta.
- Una comparación sin cambios es válida y queda registrada con cero diferencias.
- El resultado histórico es de solo lectura.
- Las líneas mostradas ya provienen del contenido saneado que RedDiff preservó en cada versión.

## Endpoints

Todas las operaciones requieren una sesión activa. `POST` requiere el encabezado CSRF administrado por el cliente web.

| Método y ruta                            | Resultado                                                                                    |
| ---------------------------------------- | -------------------------------------------------------------------------------------------- |
| `POST /api/comparaciones`                | Recibe `versionOrigenId` y `versionDestinoId`; devuelve `201` con el resultado completo.     |
| `GET /api/comparaciones`                 | Lista comparaciones recientes. Admite el filtro opcional `dispositivoId`.                    |
| `GET /api/comparaciones/{comparacionId}` | Devuelve metadatos, cantidades y detalle ordenado de una comparación previamente registrada. |

Los errores se devuelven mediante `ProblemDetails`. Una selección con la misma versión o con versiones de distintos dispositivos produce `400`; una versión o comparación inexistente produce `404`.

## Permisos

| Operación                             | Administrador | Técnico |
| ------------------------------------- | ------------: | ------: |
| Crear una comparación                 |            Sí |      Sí |
| Consultar historial de comparaciones  |            Sí |      Sí |
| Consultar el detalle de diferencias   |            Sí |      Sí |
| Modificar una versión desde el módulo |            No |      No |
| Aplicar cambios al dispositivo        |            No |      No |

La comparación es una ayuda de diagnóstico. El usuario técnico conserva la responsabilidad de interpretar la evidencia y cualquier recuperación debe ejecutarse mediante el procedimiento autorizado fuera de RedDiff.
