\set ON_ERROR_STOP on
\getenv usuario_aplicacion REDDIFF_BD_USUARIO_APLICACION
\set contrasena_aplicacion `cat /run/secrets/postgresql_aplicacion_contrasena`

SELECT format(
    'CREATE ROLE %I',
    :'usuario_aplicacion')
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_roles
    WHERE rolname = :'usuario_aplicacion')
\gexec

SELECT format(
    'ALTER ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION CONNECTION LIMIT 20',
    :'usuario_aplicacion',
    :'contrasena_aplicacion')
\gexec

CREATE SCHEMA IF NOT EXISTS reddiff;
REVOKE ALL ON SCHEMA reddiff FROM PUBLIC;

SELECT format(
    'GRANT CONNECT ON DATABASE %I TO %I',
    current_database(),
    :'usuario_aplicacion')
\gexec

SELECT format('GRANT USAGE ON SCHEMA reddiff TO %I', :'usuario_aplicacion')
\gexec

SELECT format(
    'GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA reddiff TO %I',
    :'usuario_aplicacion')
\gexec

SELECT format(
    'GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA reddiff TO %I',
    :'usuario_aplicacion')
\gexec

SELECT format(
    'ALTER DEFAULT PRIVILEGES IN SCHEMA reddiff GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I',
    :'usuario_aplicacion')
\gexec

SELECT format(
    'ALTER DEFAULT PRIVILEGES IN SCHEMA reddiff GRANT USAGE, SELECT ON SEQUENCES TO %I',
    :'usuario_aplicacion')
\gexec

SELECT format(
    'ALTER ROLE %I IN DATABASE %I SET search_path TO reddiff, public',
    :'usuario_aplicacion',
    current_database())
\gexec
