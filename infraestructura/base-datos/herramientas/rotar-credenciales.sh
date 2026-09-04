#!/bin/sh

set -eu

IFS= read -r nueva_admin
IFS= read -r nueva_aplicacion

nueva_admin="$(printf '%s' "$nueva_admin" | tr -d '\r')"
nueva_aplicacion="$(printf '%s' "$nueva_aplicacion" | tr -d '\r')"

if [ -z "$nueva_admin" ] || [ -z "$nueva_aplicacion" ]; then
    echo 'No se recibieron las dos credenciales nuevas.' >&2
    exit 1
fi

export PGPASSWORD="$(cat /run/secrets/postgresql_contrasena)"
export REDDIFF_NUEVA_CONTRASENA_ADMIN="$nueva_admin"
export REDDIFF_NUEVA_CONTRASENA_APLICACION="$nueva_aplicacion"

exec psql \
    --host=127.0.0.1 \
    --username="$POSTGRES_USER" \
    --dbname="$POSTGRES_DB" \
    --file=/opt/reddiff/herramientas/rotar-credenciales.sql
