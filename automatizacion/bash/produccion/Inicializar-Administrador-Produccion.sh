#!/usr/bin/env bash

set -Eeuo pipefail

RUTA_SCRIPT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$RUTA_SCRIPT/comun.sh"

validar_requisitos_produccion

RUTA_SECRETO="$RUTA_SECRETOS/administrador_inicial_contrasena.txt"
CONTRASENA=''
CONFIRMACION=''

limpiar() {
    CONTRASENA=''
    CONFIRMACION=''
    if [[ -f "$RUTA_SECRETO" ]]; then
        : > "$RUTA_SECRETO"
        chmod 0600 "$RUTA_SECRETO"
    fi
}
trap limpiar EXIT

read -r -s -p 'Contraseña del primer administrador: ' CONTRASENA
echo
read -r -s -p 'Repita la contraseña: ' CONFIRMACION
echo

if [[ "$CONTRASENA" != "$CONFIRMACION" ]]; then
    echo "Las contraseñas no coinciden." >&2
    exit 1
fi

if (( ${#CONTRASENA} < 12 || ${#CONTRASENA} > 128 )); then
    echo "La contraseña debe contener entre 12 y 128 caracteres." >&2
    exit 1
fi

umask 077
printf '%s' "$CONTRASENA" > "$RUTA_SECRETO"
chmod 0600 "$RUTA_SECRETO"

ejecutar_compose --profile herramientas run --rm inicializador

echo "La primera cuenta administrativa fue creada correctamente."
