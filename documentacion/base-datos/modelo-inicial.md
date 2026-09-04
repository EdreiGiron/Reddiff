# Modelo inicial de datos

## Alcance

El modelo implementa las trece entidades definidas en el diseño académico y prepara la persistencia para los flujos de autenticación, inventario, captura, versionado, comparación, baseline, verificación y auditoría.

| Tabla | Responsabilidad principal |
| --- | --- |
| `rol` | Catálogo de funciones asignables a los usuarios. |
| `usuario` | Cuenta autorizada y hash de su contraseña. |
| `dispositivo` | Inventario de equipos Cisco o nodos de laboratorio. |
| `evento_cambio` | Señal SNMP o Syslog validada y deduplicable. |
| `captura` | Solicitud bajo demanda o iniciada por un evento. |
| `version_config` | Contenido histórico, metadatos y huella SHA-256. |
| `baseline` | Referencia aplicable a un dispositivo o tipo de dispositivo. |
| `regla_baseline` | Criterio individual de una baseline. |
| `comparacion` | Ejecución entre una versión de origen y una de destino. |
| `detalle_diferencia` | Evidencia de cada línea agregada, eliminada o modificada. |
| `verificacion` | Evaluación de una versión contra una baseline. |
| `resultado_regla` | Estado y evidencia de cada regla evaluada. |
| `auditoria` | Trazabilidad de acciones y resultados relevantes. |

## Extensiones justificadas

El diagrama lógico del documento presenta los atributos principales. La implementación incorpora los siguientes campos necesarios para cubrir sus propios requisitos textuales:

- `usuario.contrasena_hash`: permite autenticar sin almacenar contraseñas en texto claro;
- `version_config.validada_por_usuario_id` y `validada_en`: identifican quién declaró estable una versión y cuándo lo hizo;
- `version_config.estado`: permite retiro lógico sin eliminar evidencia;
- `version_config.comentario`: conserva el comentario mostrado en el historial previsto por el diseño de interfaz;
- `baseline.tipo_dispositivo`: permite aplicar una baseline a un equipo específico o a un tipo de equipo.

## Integridad

- Los identificadores utilizan `bigint` generado por PostgreSQL.
- Las fechas se guardan como `timestamp with time zone` y el dominio las normaliza a UTC.
- Las huellas de eventos y versiones deben ser valores SHA-256 hexadecimales de 64 caracteres.
- La combinación de dispositivo y huella de evento es única para evitar duplicidades.
- Una captura conserva exclusivamente al usuario solicitante o al evento que la originó, según su disparador.
- Una captura produce como máximo una versión y el número de versión es único dentro del dispositivo.
- Una versión estable debe conservar simultáneamente usuario validador y fecha de validación.
- Una baseline debe referirse a un dispositivo o a un tipo de dispositivo, nunca a ambos.
- Una verificación solo puede contener un resultado por regla.
- Todas las claves foráneas restringen la eliminación en cascada para proteger la evidencia histórica.

Las verificaciones que requieren consultar varias filas, como confirmar que dos versiones pertenecen al mismo dispositivo, se implementarán en los casos de uso de la capa de aplicación y no se delegarán únicamente a la interfaz.
