# Líneas base y verificaciones de cumplimiento

## Alcance implementado

Una línea base reúne criterios esperados para las configuraciones de un dispositivo concreto o de todos los equipos de un mismo tipo. Puede asociar opcionalmente una versión histórica compatible como referencia estable, pero sus reglas constituyen el criterio evaluable.

La verificación compara una versión ya preservada con esas reglas, conserva un resultado por regla y calcula un resultado general. No abre una conexión al dispositivo, no ejecuta comandos, no modifica configuraciones y no certifica por sí sola que un cambio sea correcto.

## Criterios admitidos

| Criterio                   | Cumple cuando                                                                                    |
| -------------------------- | ------------------------------------------------------------------------------------------------ |
| `Contiene`                 | El contenido canónico incluye exactamente el texto esperado.                                     |
| `NoContiene`               | El contenido canónico no incluye el texto esperado.                                              |
| `CoincideExpresionRegular` | Una expresión regular válida encuentra una coincidencia; se usa modo multilínea y tiempo límite. |

Cada línea base contiene entre 1 y 100 reglas no duplicadas. Una regla puede ser obligatoria u opcional. Los criterios quedan inmutables después de crear la línea base; para sustituirlos se crea otra línea base y se desactiva la anterior.

## Resultado

- `Incumple`: al menos una regla obligatoria quedó `Incumplida`.
- `NoEvaluable`: no hubo incumplimiento obligatorio, pero al menos una regla obligatoria no pudo evaluarse.
- `Cumple`: todas las reglas obligatorias fueron cumplidas; una regla opcional incumplida queda visible como evidencia, pero no cambia el resultado general.

El porcentaje corresponde a reglas cumplidas entre reglas totales. El detalle conserva criterio, valor esperado, obligatoriedad, estado y evidencia breve, sin duplicar la configuración completa.

## Endpoints

Todas las operaciones requieren una sesión activa. `POST` y `PATCH` requieren el encabezado CSRF administrado por el cliente web.

| Método y ruta                              | Permiso                 | Resultado                                                                    |
| ------------------------------------------ | ----------------------- | ---------------------------------------------------------------------------- |
| `GET /api/baselines`                       | Administrador y técnico | Lista líneas base con alcance, vigencia, referencia y reglas.                |
| `GET /api/baselines/{baselineId}`          | Administrador y técnico | Devuelve una línea base.                                                     |
| `POST /api/baselines`                      | Administrador           | Crea una línea base y marca como estable la referencia histórica opcional.   |
| `PATCH /api/baselines/{baselineId}/estado` | Administrador           | Activa o desactiva la línea base sin eliminar evidencia.                     |
| `POST /api/verificaciones`                 | Administrador y técnico | Evalúa `baselineId` y `versionId`; devuelve `201` con el detalle persistido. |
| `GET /api/verificaciones`                  | Administrador y técnico | Lista verificaciones; admite el filtro opcional `dispositivoId`.             |
| `GET /api/verificaciones/{verificacionId}` | Administrador y técnico | Devuelve el resultado general y la evidencia ordenada de cada regla.         |

## Validaciones principales

- El alcance debe ser exactamente `Dispositivo` o `TipoDispositivo` y no puede mezclar ambos destinos.
- El tipo debe corresponder con al menos un dispositivo registrado.
- La versión de referencia debe estar activa y corresponder con el alcance.
- Una línea base inactiva no admite nuevas verificaciones.
- La versión evaluada debe pertenecer al dispositivo indicado o coincidir con el tipo definido.
- Una expresión regular inválida, agotada por tiempo o imposible de evaluar no se acepta o produce evidencia `NoEvaluable`, según el momento del fallo.
- Los errores usan `ProblemDetails`; no se devuelven secretos ni se registra contenido completo en auditoría.

## Límites deliberados

La función es una ayuda de diagnóstico. RedDiff no aplica cambios, no restaura versiones y no corrige incumplimientos automáticamente. La interpretación técnica y cualquier remediación deben seguir un procedimiento autorizado fuera del módulo.
