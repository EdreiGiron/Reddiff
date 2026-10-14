#!/bin/sh

set -eu

exec psql \
    --username="$POSTGRES_USER" \
    --dbname="$POSTGRES_DB" \
    --file=/opt/reddiff/herramientas/configurar-usuario-aplicacion.sql
