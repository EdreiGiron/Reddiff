#!/usr/bin/env bash

set -Eeuo pipefail

RUTA_SCRIPT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$RUTA_SCRIPT/comun.sh"

requerir_comando docker
requerir_archivo "$RUTA_ENTORNO"

ejecutar_compose down
echo "Los contenedores se detuvieron; los volúmenes y la información se conservaron."
