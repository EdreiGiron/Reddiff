#!/usr/bin/env bash

set -Eeuo pipefail

RUTA_SCRIPT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$RUTA_SCRIPT/comun.sh"

requerir_comando openssl

PLANTILLA_ENTORNO="$RUTA_RAIZ/configuracion/entornos/produccion.env.example"
NOMBRE_HOST="${1:-}"

if [[ ! -f "$RUTA_ENTORNO" ]]; then
    requerir_archivo "$PLANTILLA_ENTORNO"
    cp "$PLANTILLA_ENTORNO" "$RUTA_ENTORNO"
    chmod 0600 "$RUTA_ENTORNO"
fi

actualizar_variable() {
    local nombre="$1"
    local valor="$2"
    local temporal
    temporal="$(mktemp)"

    awk -F= -v clave="$nombre" -v nuevo="$valor" '
        BEGIN { actualizado = 0 }
        $1 == clave {
            print clave "=" nuevo
            actualizado = 1
            next
        }
        { print }
        END {
            if (!actualizado) {
                print clave "=" nuevo
            }
        }
    ' "$RUTA_ENTORNO" > "$temporal"

    mv "$temporal" "$RUTA_ENTORNO"
    chmod 0600 "$RUTA_ENTORNO"
}

if [[ -n "$NOMBRE_HOST" ]]; then
    if [[ ! "$NOMBRE_HOST" =~ ^[A-Za-z0-9.-]+$ ]]; then
        echo "El nombre del host contiene caracteres no permitidos." >&2
        exit 1
    fi

    actualizar_variable REDDIFF_NOMBRE_HOST "$NOMBRE_HOST"
    actualizar_variable REDDIFF_ORIGEN_CLIENTE "https://$NOMBRE_HOST"
fi

NOMBRE_HOST="$(obtener_variable REDDIFF_NOMBRE_HOST)"
mkdir -p "$RUTA_SECRETOS"
chmod 0700 "$RUTA_SECRETOS"
umask 077

generar_secreto_si_falta() {
    local ruta="$1"
    if [[ ! -f "$ruta" ]]; then
        openssl rand -base64 48 | tr -d '\n' > "$ruta"
        chmod 0600 "$ruta"
    fi
}

generar_secreto_si_falta "$RUTA_SECRETOS/postgresql_contrasena.txt"
generar_secreto_si_falta "$RUTA_SECRETOS/postgresql_aplicacion_contrasena.txt"

if [[ ! -f "$RUTA_SECRETOS/administrador_inicial_contrasena.txt" ]]; then
    install -m 0600 /dev/null "$RUTA_SECRETOS/administrador_inicial_contrasena.txt"
fi

ARCHIVOS_CERTIFICADOS=(
    autoridad_certificadora.crt
    autoridad_certificadora.key
    postgresql_servidor.crt
    postgresql_servidor.key
    reddiff_https.crt
    reddiff_https.key
)

faltantes=0
existentes=0
for archivo in "${ARCHIVOS_CERTIFICADOS[@]}"; do
    if [[ -f "$RUTA_SECRETOS/$archivo" ]]; then
        ((existentes += 1))
    else
        ((faltantes += 1))
    fi
done

if (( existentes > 0 && faltantes > 0 )); then
    echo "El conjunto de certificados está incompleto. No se sobrescribirá material existente." >&2
    echo "Revise el directorio $RUTA_SECRETOS antes de continuar." >&2
    exit 1
fi

if (( existentes == 0 )); then
    TEMPORAL="$(mktemp -d)"
    trap 'rm -rf "$TEMPORAL"' EXIT

    openssl req -x509 -newkey rsa:4096 -sha256 -nodes \
        -days 3650 \
        -subj "/CN=RedDiff Autoridad Interna" \
        -keyout "$RUTA_SECRETOS/autoridad_certificadora.key" \
        -out "$RUTA_SECRETOS/autoridad_certificadora.crt"

    cat > "$TEMPORAL/postgresql.ext" <<'EOF'
[v3_servidor]
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=DNS:base-datos
EOF

    openssl req -new -newkey rsa:3072 -sha256 -nodes \
        -subj "/CN=base-datos" \
        -keyout "$RUTA_SECRETOS/postgresql_servidor.key" \
        -out "$TEMPORAL/postgresql.csr"
    openssl x509 -req -sha256 -days 825 \
        -in "$TEMPORAL/postgresql.csr" \
        -CA "$RUTA_SECRETOS/autoridad_certificadora.crt" \
        -CAkey "$RUTA_SECRETOS/autoridad_certificadora.key" \
        -CAcreateserial \
        -extfile "$TEMPORAL/postgresql.ext" \
        -extensions v3_servidor \
        -out "$RUTA_SECRETOS/postgresql_servidor.crt"

    if [[ "$NOMBRE_HOST" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]]; then
        SAN_WEB="IP:$NOMBRE_HOST"
    else
        SAN_WEB="DNS:$NOMBRE_HOST"
    fi

    cat > "$TEMPORAL/web.ext" <<EOF
[v3_servidor]
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=$SAN_WEB
EOF

    openssl req -new -newkey rsa:3072 -sha256 -nodes \
        -subj "/CN=$NOMBRE_HOST" \
        -keyout "$RUTA_SECRETOS/reddiff_https.key" \
        -out "$TEMPORAL/web.csr"
    openssl x509 -req -sha256 -days 825 \
        -in "$TEMPORAL/web.csr" \
        -CA "$RUTA_SECRETOS/autoridad_certificadora.crt" \
        -CAkey "$RUTA_SECRETOS/autoridad_certificadora.key" \
        -CAcreateserial \
        -extfile "$TEMPORAL/web.ext" \
        -extensions v3_servidor \
        -out "$RUTA_SECRETOS/reddiff_https.crt"

    rm -f "$RUTA_SECRETOS/autoridad_certificadora.srl"
fi

chmod 0600 "$RUTA_SECRETOS"/*.key "$RUTA_SECRETOS"/*.txt
chmod 0644 "$RUTA_SECRETOS"/*.crt

echo "Preparación completada para https://$NOMBRE_HOST."
echo "Revise $RUTA_ENTORNO y luego ejecute Desplegar-Produccion.sh."
echo "Instale autoridad_certificadora.crt como autoridad confiable en los equipos clientes."
