#!/bin/sh

set -eu

export PGPASSWORD="$(cat /run/secrets/postgresql_contrasena)"

exec psql \
    --host=127.0.0.1 \
    --username="$POSTGRES_USER" \
    --dbname="$POSTGRES_DB" \
    --file=/opt/reddiff/herramientas/configurar-usuario-aplicacion.sql
