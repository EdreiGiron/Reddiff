\set ON_ERROR_STOP on
\getenv usuario_admin POSTGRES_USER
\getenv usuario_aplicacion REDDIFF_BD_USUARIO_APLICACION
\getenv contrasena_admin REDDIFF_NUEVA_CONTRASENA_ADMIN
\getenv contrasena_aplicacion REDDIFF_NUEVA_CONTRASENA_APLICACION

BEGIN;

SELECT format(
    'ALTER ROLE %I WITH PASSWORD %L',
    :'usuario_admin',
    :'contrasena_admin')
\gexec

SELECT format(
    'ALTER ROLE %I WITH PASSWORD %L',
    :'usuario_aplicacion',
    :'contrasena_aplicacion')
\gexec

COMMIT;
