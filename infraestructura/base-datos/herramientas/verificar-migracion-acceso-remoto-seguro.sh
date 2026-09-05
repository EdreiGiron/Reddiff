#!/bin/sh

set -eu

fase="${1:-}"

case "$fase" in
    previa)
        script_sql=/opt/reddiff/herramientas/verificar-precondiciones-migracion-acceso-remoto-seguro.sql
        ;;
    final)
        script_sql=/opt/reddiff/herramientas/verificar-migracion-acceso-remoto-seguro.sql
        ;;
    *)
        echo 'Fase no válida. Use previa o final.' >&2
        exit 2
        ;;
esac

export PGPASSWORD="$(cat /run/secrets/postgresql_contrasena)"

exec psql \
    --host=127.0.0.1 \
    --username="$POSTGRES_USER" \
    --dbname="$POSTGRES_DB" \
    --set=ON_ERROR_STOP=1 \
    --file="$script_sql"
