\set ON_ERROR_STOP on
\getenv base_datos REDDIFF_BD_NOMBRE
\getenv usuario_admin REDDIFF_BD_USUARIO_ADMIN
\getenv usuario_aplicacion REDDIFF_BD_USUARIO_APLICACION
\getenv contrasena_admin REDDIFF_BD_CONTRASENA_ADMIN
\getenv contrasena_aplicacion REDDIFF_BD_CONTRASENA_APLICACION

SET password_encryption = 'scram-sha-256';

SELECT format('CREATE ROLE %I', :'usuario_admin')
WHERE NOT EXISTS (
    SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = :'usuario_admin')
\gexec

SELECT format(
    'ALTER ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS CONNECTION LIMIT 10',
    :'usuario_admin',
    :'contrasena_admin')
\gexec

SELECT format('CREATE ROLE %I', :'usuario_aplicacion')
WHERE NOT EXISTS (
    SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = :'usuario_aplicacion')
\gexec

SELECT format(
    'ALTER ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS CONNECTION LIMIT 20',
    :'usuario_aplicacion',
    :'contrasena_aplicacion')
\gexec

SELECT format(
    'CREATE DATABASE %I WITH OWNER %I ENCODING %L TEMPLATE template0',
    :'base_datos',
    :'usuario_admin',
    'UTF8')
WHERE NOT EXISTS (
    SELECT 1 FROM pg_catalog.pg_database WHERE datname = :'base_datos')
\gexec

SELECT format('ALTER DATABASE %I OWNER TO %I', :'base_datos', :'usuario_admin')
\gexec

SELECT format('REVOKE CONNECT ON DATABASE %I FROM PUBLIC', :'base_datos')
\gexec

SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'base_datos', :'usuario_admin')
\gexec

SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'base_datos', :'usuario_aplicacion')
\gexec

SELECT format(
    'ALTER ROLE %I IN DATABASE %I SET search_path TO reddiff, public',
    :'usuario_admin',
    :'base_datos')
\gexec

SELECT format(
    'ALTER ROLE %I IN DATABASE %I SET search_path TO reddiff, public',
    :'usuario_aplicacion',
    :'base_datos')
\gexec
