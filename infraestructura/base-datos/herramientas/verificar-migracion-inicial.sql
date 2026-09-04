\set ON_ERROR_STOP on

DO $verificacion$
DECLARE
    tablas_esperadas text[] := ARRAY[
        'auditoria',
        'baseline',
        'captura',
        'comparacion',
        'detalle_diferencia',
        'dispositivo',
        'evento_cambio',
        'regla_baseline',
        'resultado_regla',
        'rol',
        'usuario',
        'verificacion',
        'version_config'
    ];
    indices_esperados text[] := ARRAY[
        'ix_auditoria_entidad_fecha',
        'ix_auditoria_fecha',
        'ix_auditoria_usuario_fecha',
        'ix_baseline_dispositivo_activa',
        'ix_baseline_tipo_activa',
        'ix_baseline_version_config',
        'ix_captura_dispositivo_disparador_fecha',
        'ix_captura_dispositivo_fecha',
        'ix_captura_estado',
        'ix_captura_usuario',
        'ix_comparacion_usuario_fecha',
        'ix_comparacion_version_destino',
        'ix_comparacion_versiones_fecha',
        'ix_detalle_diferencia_comparacion_linea',
        'ix_evento_cambio_estado_fecha',
        'ix_regla_baseline_baseline',
        'ix_resultado_regla_regla_baseline',
        'ix_usuario_rol',
        'ix_verificacion_baseline_version_fecha',
        'ix_verificacion_usuario_fecha',
        'ix_verificacion_version_config',
        'ix_version_config_dispositivo_estado_fecha',
        'ix_version_config_dispositivo_fecha',
        'ix_version_config_dispositivo_origen_fecha',
        'ix_version_config_hash',
        'ix_version_config_usuario_validador',
        'ux_captura_evento',
        'ux_dispositivo_host_puerto',
        'ux_dispositivo_nombre',
        'ux_evento_cambio_dispositivo_huella',
        'ux_resultado_regla_verificacion_regla',
        'ux_rol_nombre',
        'ux_usuario_nombre',
        'ux_version_config_captura',
        'ux_version_config_dispositivo_numero'
    ];
    elementos_faltantes text;
    cantidad integer;
BEGIN
    SELECT string_agg(tabla, ', ' ORDER BY tabla)
    INTO elementos_faltantes
    FROM unnest(tablas_esperadas) AS esperada(tabla)
    WHERE to_regclass(format('reddiff.%I', tabla)) IS NULL;

    IF elementos_faltantes IS NOT NULL THEN
        RAISE EXCEPTION 'Faltan tablas del modelo: %', elementos_faltantes;
    END IF;

    SELECT count(*)
    INTO cantidad
    FROM pg_catalog.pg_tables
    WHERE schemaname = 'reddiff'
      AND tablename <> '__historial_migraciones';

    IF cantidad <> 13 THEN
        RAISE EXCEPTION 'Se esperaban 13 tablas del modelo y se encontraron %.', cantidad;
    END IF;

    IF to_regclass('reddiff.__historial_migraciones') IS NULL THEN
        RAISE EXCEPTION 'No existe el historial de migraciones.';
    END IF;

    IF (SELECT count(*) FROM reddiff.__historial_migraciones) <> 1
        OR NOT EXISTS (
            SELECT 1
            FROM reddiff.__historial_migraciones
            WHERE "MigrationId" = '20260904032054_Inicial'
              AND "ProductVersion" = '10.0.11') THEN
        RAISE EXCEPTION 'El historial no contiene exclusivamente la migración inicial revisada.';
    END IF;

    SELECT count(*)
    INTO cantidad
    FROM pg_catalog.pg_constraint AS restriccion
    INNER JOIN pg_catalog.pg_namespace AS espacio
        ON espacio.oid = restriccion.connamespace
    WHERE espacio.nspname = 'reddiff'
      AND restriccion.contype = 'f';

    IF cantidad <> 21 THEN
        RAISE EXCEPTION 'Se esperaban 21 claves foráneas y se encontraron %.', cantidad;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_constraint AS restriccion
        INNER JOIN pg_catalog.pg_namespace AS espacio
            ON espacio.oid = restriccion.connamespace
        WHERE espacio.nspname = 'reddiff'
          AND restriccion.contype = 'f'
          AND restriccion.confdeltype <> 'r') THEN
        RAISE EXCEPTION 'Existe una clave foránea sin ON DELETE RESTRICT.';
    END IF;

    SELECT count(*)
    INTO cantidad
    FROM pg_catalog.pg_constraint AS restriccion
    INNER JOIN pg_catalog.pg_namespace AS espacio
        ON espacio.oid = restriccion.connamespace
    WHERE espacio.nspname = 'reddiff'
      AND restriccion.contype = 'c';

    IF cantidad <> 4 THEN
        RAISE EXCEPTION 'Se esperaban 4 restricciones CHECK y se encontraron %.', cantidad;
    END IF;

    SELECT string_agg(indice, ', ' ORDER BY indice)
    INTO elementos_faltantes
    FROM unnest(indices_esperados) AS esperado(indice)
    WHERE NOT EXISTS (
        SELECT 1
        FROM pg_catalog.pg_indexes
        WHERE schemaname = 'reddiff'
          AND indexname = indice);

    IF elementos_faltantes IS NOT NULL THEN
        RAISE EXCEPTION 'Faltan índices del modelo: %', elementos_faltantes;
    END IF;
END
$verificacion$;

SELECT
    current_database() AS base_datos,
    (SELECT count(*)
     FROM pg_catalog.pg_tables
     WHERE schemaname = 'reddiff'
       AND tablename <> '__historial_migraciones') AS tablas_modelo,
    (SELECT count(*)
     FROM pg_catalog.pg_constraint AS restriccion
     INNER JOIN pg_catalog.pg_namespace AS espacio
         ON espacio.oid = restriccion.connamespace
     WHERE espacio.nspname = 'reddiff'
       AND restriccion.contype = 'f') AS claves_foraneas,
    (SELECT count(*)
     FROM pg_catalog.pg_constraint AS restriccion
     INNER JOIN pg_catalog.pg_namespace AS espacio
         ON espacio.oid = restriccion.connamespace
     WHERE espacio.nspname = 'reddiff'
       AND restriccion.contype = 'c') AS restricciones_check,
    (SELECT count(*)
     FROM pg_catalog.pg_indexes
     WHERE schemaname = 'reddiff'
       AND (indexname LIKE 'ix_%' OR indexname LIKE 'ux_%')) AS indices_modelo;
