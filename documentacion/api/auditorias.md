# Bitácora de auditoría

La bitácora permite al administrador reconstruir las operaciones relevantes de RedDiff sin duplicar configuraciones ni secretos. Su consulta está protegida por sesión y por la política del rol `Administrador`.

## Consulta

`GET /api/auditorias`

| Parámetro | Tipo | Descripción |
| --- | --- | --- |
| `usuarioId` | entero opcional | Limita los resultados al usuario indicado. |
| `accion` | texto opcional | Coincidencia exacta con la acción registrada. |
| `entidad` | texto opcional | Coincidencia exacta con la entidad afectada. |
| `estado` | `Exitoso` o `Fallido` | Filtra por el resultado de la operación. |
| `desde` | fecha y hora ISO 8601 | Incluye registros desde ese instante. |
| `hasta` | fecha y hora ISO 8601 | Incluye registros hasta ese instante. |
| `pagina` | entero | Página solicitada; el valor predeterminado es `1`. |
| `tamanoPagina` | entero | Cantidad entre `1` y `100`; el valor predeterminado es `25`. |

La respuesta contiene `registros`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`. Cada registro expone identificadores, usuario, acción, entidad, fecha, estado y un detalle limitado. No devuelve contraseñas, secretos de acceso ni contenido completo de configuraciones.

## Catálogo para filtros guiados

`GET /api/auditorias/catalogo-filtros`

Devuelve los usuarios disponibles por identificador y nombre, junto con las acciones y entidades distintas que realmente existen en la bitácora. La interfaz utiliza este catálogo para presentar listas desplegables y evita que el administrador tenga que conocer o escribir valores internos exactos.

Las etiquetas de acción y entidad se presentan separando sus palabras para facilitar la lectura, pero la solicitud conserva el valor original exacto. El catálogo no incluye contraseñas, roles sensibles ni contenido de configuraciones.

## Autorización y conservación

- Un técnico recibe `403 Forbidden` aunque tenga una sesión válida.
- Tanto la consulta como el catálogo de filtros requieren el rol `Administrador`.
- La consulta no modifica ni elimina registros.
- Los resultados se ordenan por fecha e identificador descendentes.
- Los errores de filtros se entregan mediante `ProblemDetails`.
- El registro puede tener usuario nulo cuando la operación procede de un proceso del sistema.
