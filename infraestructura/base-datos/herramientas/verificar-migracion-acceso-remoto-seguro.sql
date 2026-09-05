\set ON_ERROR_STOP on

DO $verificacion$
DECLARE
    elementos_invalidos text;
    definicion_restriccion text;
    restriccion_validada boolean;
    cantidad integer;
BEGIN
    IF to_regclass('reddiff.__historial_migraciones') IS NULL THEN
        RAISE EXCEPTION 'No existe el historial de migraciones.';
    END IF;

    IF (SELECT count(*) FROM reddiff.__historial_migraciones) <> 2
        OR NOT EXISTS (
            SELECT 1
            FROM reddiff.__historial_migraciones
            WHERE "MigrationId" = '20260904032054_Inicial'
              AND "ProductVersion" = '10.0.11')
        OR NOT EXISTS (
            SELECT 1
            FROM reddiff.__historial_migraciones
            WHERE "MigrationId" = '20260905061958_AgregarAccesoRemotoSeguro'
              AND "ProductVersion" = '10.0.11') THEN
        RAISE EXCEPTION 'El historial no contiene exactamente las dos migraciones revisadas.';
    END IF;

    WITH columnas_esperadas(nombre, tipo, longitud) AS (
        VALUES
            ('acceso_configurado_en', 'timestamp with time zone', NULL::integer),
            ('algoritmo_clave_host', 'character varying', 100),
            ('huella_clave_host', 'character', 64),
            ('secreto_acceso_protegido', 'text', NULL::integer),
            ('usuario_acceso', 'character varying', 120)
    )
    SELECT string_agg(esperada.nombre, ', ' ORDER BY esperada.nombre)
    INTO elementos_invalidos
    FROM columnas_esperadas AS esperada
    LEFT JOIN information_schema.columns AS actual
        ON actual.table_schema = 'reddiff'
       AND actual.table_name = 'dispositivo'
       AND actual.column_name = esperada.nombre
    WHERE actual.column_name IS NULL
       OR actual.data_type <> esperada.tipo
       OR actual.character_maximum_length IS DISTINCT FROM esperada.longitud
       OR actual.is_nullable <> 'YES'
       OR actual.column_default IS NOT NULL;

    IF elementos_invalidos IS NOT NULL THEN
        RAISE EXCEPTION 'Las columnas de acceso remoto no coinciden con el diseño revisado: %.', elementos_invalidos;
    END IF;

    SELECT
        pg_get_constraintdef(restriccion.oid),
        restriccion.convalidated
    INTO definicion_restriccion, restriccion_validada
    FROM pg_catalog.pg_constraint AS restriccion
    INNER JOIN pg_catalog.pg_class AS tabla
        ON tabla.oid = restriccion.conrelid
    INNER JOIN pg_catalog.pg_namespace AS espacio
        ON espacio.oid = tabla.relnamespace
    WHERE espacio.nspname = 'reddiff'
      AND tabla.relname = 'dispositivo'
      AND restriccion.conname = 'ck_dispositivo_acceso_remoto'
      AND restriccion.contype = 'c';

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No existe la restricción ck_dispositivo_acceso_remoto.';
    END IF;

    IF NOT restriccion_validada THEN
        RAISE EXCEPTION 'La restricción ck_dispositivo_acceso_remoto no está validada.';
    END IF;

    IF definicion_restriccion NOT LIKE '%usuario_acceso IS NULL%'
        OR definicion_restriccion NOT LIKE '%secreto_acceso_protegido IS NULL%'
        OR definicion_restriccion NOT LIKE '%algoritmo_clave_host IS NULL%'
        OR definicion_restriccion NOT LIKE '%huella_clave_host IS NULL%'
        OR definicion_restriccion NOT LIKE '%acceso_configurado_en IS NULL%'
        OR definicion_restriccion NOT LIKE '%usuario_acceso IS NOT NULL%'
        OR definicion_restriccion NOT LIKE '%secreto_acceso_protegido IS NOT NULL%'
        OR definicion_restriccion NOT LIKE '%algoritmo_clave_host IS NOT NULL%'
        OR definicion_restriccion NOT LIKE '%huella_clave_host IS NOT NULL%'
        OR definicion_restriccion NOT LIKE '%acceso_configurado_en IS NOT NULL%' THEN
        RAISE EXCEPTION 'La restricción de acceso remoto no conserva la invariancia revisada.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM reddiff.dispositivo
        WHERE NOT (
            (usuario_acceso IS NULL
                AND secreto_acceso_protegido IS NULL
                AND algoritmo_clave_host IS NULL
                AND huella_clave_host IS NULL
                AND acceso_configurado_en IS NULL)
            OR
            (usuario_acceso IS NOT NULL
                AND secreto_acceso_protegido IS NOT NULL
                AND algoritmo_clave_host IS NOT NULL
                AND huella_clave_host IS NOT NULL
                AND acceso_configurado_en IS NOT NULL))) THEN
        RAISE EXCEPTION 'Existe un dispositivo con un bloque parcial de acceso remoto.';
    END IF;

    SELECT count(*)
    INTO cantidad
    FROM pg_catalog.pg_tables
    WHERE schemaname = 'reddiff'
      AND tablename <> '__historial_migraciones';

    IF cantidad <> 13 THEN
        RAISE EXCEPTION 'Se esperaban 13 tablas del modelo y se encontraron %.', cantidad;
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

    SELECT count(*)
    INTO cantidad
    FROM pg_catalog.pg_constraint AS restriccion
    INNER JOIN pg_catalog.pg_namespace AS espacio
        ON espacio.oid = restriccion.connamespace
    WHERE espacio.nspname = 'reddiff'
      AND restriccion.contype = 'c';

    IF cantidad <> 5 THEN
        RAISE EXCEPTION 'Se esperaban 5 restricciones CHECK y se encontraron %.', cantidad;
    END IF;

    IF NOT has_table_privilege('reddiff_app', 'reddiff.dispositivo', 'SELECT')
        OR NOT has_table_privilege('reddiff_app', 'reddiff.dispositivo', 'INSERT')
        OR NOT has_table_privilege('reddiff_app', 'reddiff.dispositivo', 'UPDATE')
        OR NOT has_table_privilege('reddiff_app', 'reddiff.dispositivo', 'DELETE') THEN
        RAISE EXCEPTION 'reddiff_app no conserva todos los permisos requeridos sobre dispositivo.';
    END IF;

    IF has_schema_privilege('reddiff_app', 'reddiff', 'CREATE')
        OR has_table_privilege('reddiff_app', 'reddiff.__historial_migraciones', 'SELECT')
        OR has_table_privilege('reddiff_app', 'reddiff.__historial_migraciones', 'INSERT')
        OR has_table_privilege('reddiff_app', 'reddiff.__historial_migraciones', 'UPDATE')
        OR has_table_privilege('reddiff_app', 'reddiff.__historial_migraciones', 'DELETE') THEN
        RAISE EXCEPTION 'reddiff_app conserva privilegios administrativos no permitidos.';
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
     FROM information_schema.columns
     WHERE table_schema = 'reddiff'
       AND table_name = 'dispositivo'
       AND column_name IN (
           'acceso_configurado_en',
           'algoritmo_clave_host',
           'huella_clave_host',
           'secreto_acceso_protegido',
           'usuario_acceso')) AS columnas_acceso_remoto,
    (SELECT count(*)
     FROM pg_catalog.pg_constraint AS restriccion
     INNER JOIN pg_catalog.pg_namespace AS espacio
         ON espacio.oid = restriccion.connamespace
     WHERE espacio.nspname = 'reddiff'
       AND restriccion.contype = 'c') AS restricciones_check,
    (SELECT count(*) FROM reddiff.__historial_migraciones) AS migraciones_aplicadas;
