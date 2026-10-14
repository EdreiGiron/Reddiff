#!/usr/bin/env bash

set -Eeuo pipefail

RUTA_SCRIPT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
RUTA_RAIZ="$(cd -- "$RUTA_SCRIPT/../../.." && pwd)"
RUTA_COMPOSE="$RUTA_RAIZ/infraestructura/contenedores/produccion/compose.yml"
RUTA_ENTORNO="$RUTA_RAIZ/configuracion/entornos/produccion.env"
RUTA_SECRETOS="$RUTA_RAIZ/configuracion/secretos/produccion"

requerir_comando() {
    local comando="$1"
    if ! command -v "$comando" >/dev/null 2>&1; then
        echo "No se encontró el comando requerido: $comando" >&2
        exit 1
    fi
}

requerir_archivo() {
    local ruta="$1"
    if [[ ! -f "$ruta" ]]; then
        echo "No existe el archivo requerido: $ruta" >&2
        exit 1
    fi
}

obtener_variable() {
    local nombre="$1"
    local valor

    valor="$(awk -F= -v clave="$nombre" '
        $1 == clave {
            sub(/^[^=]*=/, "")
            gsub(/\r$/, "")
            encontrado = $0
        }
        END { print encontrado }
    ' "$RUTA_ENTORNO")"

    if [[ -z "$valor" ]]; then
        echo "La variable $nombre no está definida en $RUTA_ENTORNO." >&2
        exit 1
    fi

    printf '%s' "$valor"
}

ejecutar_compose() {
    docker compose \
        --env-file "$RUTA_ENTORNO" \
        --file "$RUTA_COMPOSE" \
        "$@"
}

validar_requisitos_produccion() {
    requerir_comando docker
    requerir_archivo "$RUTA_COMPOSE"
    requerir_archivo "$RUTA_ENTORNO"

    local archivos=(
        postgresql_contrasena.txt
        postgresql_aplicacion_contrasena.txt
        autoridad_certificadora.crt
        postgresql_servidor.crt
        postgresql_servidor.key
        reddiff_https.crt
        reddiff_https.key
        administrador_inicial_contrasena.txt
    )

    local archivo
    for archivo in "${archivos[@]}"; do
        requerir_archivo "$RUTA_SECRETOS/$archivo"
    done
}

esperar_salud_contenedor() {
    local servicio="$1"
    local intentos="${2:-60}"
    local contenedor estado

    contenedor="$(ejecutar_compose ps --quiet "$servicio")"
    if [[ -z "$contenedor" ]]; then
        echo "No se encontró el contenedor del servicio $servicio." >&2
        exit 1
    fi

    for ((intento = 1; intento <= intentos; intento++)); do
        estado="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$contenedor")"
        case "$estado" in
            healthy)
                return 0
                ;;
            unhealthy|exited|dead)
                echo "El servicio $servicio alcanzó el estado $estado." >&2
                ejecutar_compose logs --tail 100 "$servicio" >&2
                exit 1
                ;;
        esac

        sleep 2
    done

    echo "El servicio $servicio no alcanzó el estado saludable a tiempo." >&2
    ejecutar_compose logs --tail 100 "$servicio" >&2
    exit 1
}
