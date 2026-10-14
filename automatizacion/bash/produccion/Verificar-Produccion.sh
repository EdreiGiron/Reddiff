#!/usr/bin/env bash

set -Eeuo pipefail

RUTA_SCRIPT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$RUTA_SCRIPT/comun.sh"

validar_requisitos_produccion
requerir_comando curl

esperar_salud_contenedor base-datos 5
esperar_salud_contenedor api 5
esperar_salud_contenedor web 5

NOMBRE_HOST="$(obtener_variable REDDIFF_NOMBRE_HOST)"
PUERTO_HTTPS="$(obtener_variable REDDIFF_HTTPS_PUERTO)"

curl \
    --fail \
    --silent \
    --show-error \
    --cacert "$RUTA_SECRETOS/autoridad_certificadora.crt" \
    --resolve "$NOMBRE_HOST:$PUERTO_HTTPS:127.0.0.1" \
    "https://$NOMBRE_HOST:$PUERTO_HTTPS/salud/listo"
echo

ejecutar_compose ps
echo "La verificación técnica de RedDiff finalizó correctamente."
