\set ON_ERROR_STOP on

DO $verificacion$
DECLARE
    migracion_inicial constant text := '20260904032054_Inicial';
    migracion_acceso_remoto constant text := '20260905061958_AgregarAccesoRemotoSeguro';
    columnas_acceso_remoto constant text[] := ARRAY[
        'acceso_configurado_en',
        'algoritmo_clave_host',
        'huella_clave_host',
        'secreto_acceso_protegido',
        'usuario_acceso'
    ];
    migracion_ya_aplicada boolean;
    cantidad integer;
BEGIN
    IF to_regclass('reddiff.__historial_migraciones') IS NULL THEN
        RAISE EXCEPTION 'No existe el historial de migraciones.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM reddiff.__historial_migraciones
        WHERE "MigrationId" = migracion_inicial
          AND "ProductVersion" = '10.0.11') THEN
        RAISE EXCEPTION 'La migración inicial revisada no está aplicada.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM reddiff.__historial_migraciones
        WHERE "MigrationId" NOT IN (migracion_inicial, migracion_acceso_remoto)) THEN
        RAISE EXCEPTION 'El historial contiene una migración ajena a la secuencia revisada.';
    END IF;

    SELECT EXISTS (
        SELECT 1
        FROM reddiff.__historial_migraciones
        WHERE "MigrationId" = migracion_acceso_remoto
          AND "ProductVersion" = '10.0.11')
    INTO migracion_ya_aplicada;

    SELECT count(*)
    INTO cantidad
    FROM reddiff.__historial_migraciones;

    IF (migracion_ya_aplicada AND cantidad <> 2)
        OR (NOT migracion_ya_aplicada AND cantidad <> 1) THEN
        RAISE EXCEPTION 'El historial no coincide con el estado esperado de la migración incremental.';
    END IF;

    SELECT count(*)
    INTO cantidad
    FROM pg_catalog.pg_tables
    WHERE schemaname = 'reddiff'
      AND tablename <> '__historial_migraciones';

    IF cantidad <> 13 THEN
        RAISE EXCEPTION 'Se esperaban 13 tablas del modelo y se encontraron %.', cantidad;
    END IF;

    IF NOT migracion_ya_aplicada THEN
        IF EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'reddiff'
              AND table_name = 'dispositivo'
              AND column_name = ANY(columnas_acceso_remoto)) THEN
            RAISE EXCEPTION 'Existen columnas de acceso remoto sin una migración registrada.';
        END IF;

        IF EXISTS (
            SELECT 1
            FROM pg_catalog.pg_constraint AS restriccion
            INNER JOIN pg_catalog.pg_class AS tabla
                ON tabla.oid = restriccion.conrelid
            INNER JOIN pg_catalog.pg_namespace AS espacio
                ON espacio.oid = tabla.relnamespace
            WHERE espacio.nspname = 'reddiff'
              AND tabla.relname = 'dispositivo'
              AND restriccion.conname = 'ck_dispositivo_acceso_remoto') THEN
            RAISE EXCEPTION 'Existe la restricción de acceso remoto sin una migración registrada.';
        END IF;
    END IF;
END
$verificacion$;

SELECT
    current_database() AS base_datos,
    (SELECT count(*) FROM reddiff.__historial_migraciones) AS migraciones_registradas,
    EXISTS (
        SELECT 1
        FROM reddiff.__historial_migraciones
        WHERE "MigrationId" = '20260905061958_AgregarAccesoRemotoSeguro') AS acceso_remoto_ya_aplicado;
