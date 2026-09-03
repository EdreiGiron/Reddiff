#!/bin/sh

set -eu

export PGPASSWORD="$(cat /run/secrets/postgresql_aplicacion_contrasena)"

exec psql \
    --host=127.0.0.1 \
    --username="$REDDIFF_BD_USUARIO_APLICACION" \
    --dbname="$POSTGRES_DB" \
    --set=ON_ERROR_STOP=1 \
    --command="SELECT current_database() AS base_datos, current_user AS usuario_aplicacion;"
