\set ON_ERROR_STOP on

DO $verificacion$
DECLARE
    atributos record;
BEGIN
    SELECT
        rolsuper,
        rolcreatedb,
        rolcreaterole,
        rolinherit,
        rolreplication,
        rolbypassrls
    INTO atributos
    FROM pg_catalog.pg_roles
    WHERE rolname = current_user;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No fue posible consultar el usuario de aplicación.';
    END IF;

    IF atributos.rolsuper
        OR atributos.rolcreatedb
        OR atributos.rolcreaterole
        OR atributos.rolinherit
        OR atributos.rolreplication
        OR atributos.rolbypassrls THEN
        RAISE EXCEPTION 'El usuario de aplicación conserva privilegios administrativos.';
    END IF;

    IF NOT has_database_privilege(current_user, current_database(), 'CONNECT') THEN
        RAISE EXCEPTION 'El usuario de aplicación no puede conectarse a la base de datos.';
    END IF;

    IF NOT has_schema_privilege(current_user, 'reddiff', 'USAGE') THEN
        RAISE EXCEPTION 'El usuario de aplicación no puede utilizar el esquema reddiff.';
    END IF;

    IF has_database_privilege(current_user, current_database(), 'CREATE')
        OR has_schema_privilege(current_user, 'reddiff', 'CREATE') THEN
        RAISE EXCEPTION 'El usuario de aplicación puede crear objetos administrativos.';
    END IF;

    IF to_regclass('reddiff.__historial_migraciones') IS NOT NULL THEN
        IF
            has_table_privilege(current_user, 'reddiff.__historial_migraciones', 'SELECT')
            OR has_table_privilege(current_user, 'reddiff.__historial_migraciones', 'INSERT')
            OR has_table_privilege(current_user, 'reddiff.__historial_migraciones', 'UPDATE')
            OR has_table_privilege(current_user, 'reddiff.__historial_migraciones', 'DELETE') THEN
            RAISE EXCEPTION 'El usuario de aplicación puede modificar el historial de migraciones.';
        END IF;
    END IF;
END
$verificacion$;

SELECT
    current_database() AS base_datos,
    current_user AS usuario_aplicacion,
    has_schema_privilege(current_user, 'reddiff', 'USAGE') AS puede_usar_esquema,
    has_schema_privilege(current_user, 'reddiff', 'CREATE') AS puede_crear_en_esquema;
