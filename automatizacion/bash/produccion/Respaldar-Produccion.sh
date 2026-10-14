#!/usr/bin/env bash

set -Eeuo pipefail

RUTA_SCRIPT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$RUTA_SCRIPT/comun.sh"

validar_requisitos_produccion

RUTA_RESPALDOS="$RUTA_RAIZ/respaldos/produccion"
MARCA_TIEMPO="$(date -u +%Y%m%dT%H%M%SZ)"
RUTA_FINAL="$RUTA_RESPALDOS/reddiff-$MARCA_TIEMPO"
RUTA_TEMPORAL="$RUTA_FINAL.tmp"

mkdir -p "$RUTA_RESPALDOS"
chmod 0700 "$RUTA_RESPALDOS"
umask 077
mkdir "$RUTA_TEMPORAL"
trap 'rm -rf "$RUTA_TEMPORAL"' EXIT

ejecutar_compose exec --no-TTY base-datos sh -ec '
    export PGPASSWORD="$(cat /run/secrets/postgresql_contrasena)"
    exec pg_dump \
        --host=127.0.0.1 \
        --username="$POSTGRES_USER" \
        --dbname="$POSTGRES_DB" \
        --format=custom \
        --no-password
' > "$RUTA_TEMPORAL/base-datos.dump"

CONTENEDOR_API="$(ejecutar_compose ps --quiet api)"
if [[ -z "$CONTENEDOR_API" ]]; then
    echo "No se encontró el contenedor de la API para respaldar sus claves." >&2
    exit 1
fi

VOLUMEN_CLAVES="$(docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/lib/reddiff/claves-proteccion"}}{{.Name}}{{end}}{{end}}' "$CONTENEDOR_API")"
if [[ -z "$VOLUMEN_CLAVES" ]]; then
    echo "No se encontró el volumen de claves de protección." >&2
    exit 1
fi

docker run --rm \
    --volume "$VOLUMEN_CLAVES:/origen:ro" \
    --volume "$RUTA_TEMPORAL:/destino" \
    alpine:3.22 \
    tar -czf /destino/claves-proteccion.tar.gz -C /origen .

(
    cd "$RUTA_TEMPORAL"
    sha256sum base-datos.dump claves-proteccion.tar.gz > manifiesto.sha256
)

mv "$RUTA_TEMPORAL" "$RUTA_FINAL"
chmod -R go-rwx "$RUTA_FINAL"

echo "Respaldo creado: $RUTA_FINAL"
