#!/bin/sh

set -eu

nombre_host="${1:-}"
salida="${REDDIFF_DIRECTORIO_SALIDA:-/salida}"

if [ -z "$nombre_host" ]; then
    echo "Debe indicar el nombre DNS o la direccion IP de RedDiff." >&2
    exit 1
fi

case "$nombre_host" in
    *[!A-Za-z0-9.-]*)
        echo "El nombre del host contiene caracteres no permitidos." >&2
        exit 1
        ;;
esac

archivos='autoridad_certificadora.crt autoridad_certificadora.key postgresql_servidor.crt postgresql_servidor.key reddiff_https.crt reddiff_https.key'
for archivo in $archivos; do
    if [ -e "$salida/$archivo" ]; then
        echo "El archivo $salida/$archivo ya existe; no se reemplazara." >&2
        exit 1
    fi
done

temporal="$(mktemp -d)"
trap 'rm -rf "$temporal"' EXIT
umask 077

openssl req -x509 -newkey rsa:4096 -sha256 -nodes \
    -days 3650 \
    -subj '/CN=RedDiff Autoridad Interna' \
    -keyout "$salida/autoridad_certificadora.key" \
    -out "$salida/autoridad_certificadora.crt"

cat > "$temporal/postgresql.ext" <<'EOF'
[v3_servidor]
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=DNS:base-datos
EOF

openssl req -new -newkey rsa:3072 -sha256 -nodes \
    -subj '/CN=base-datos' \
    -keyout "$salida/postgresql_servidor.key" \
    -out "$temporal/postgresql.csr"

openssl x509 -req -sha256 -days 825 \
    -in "$temporal/postgresql.csr" \
    -CA "$salida/autoridad_certificadora.crt" \
    -CAkey "$salida/autoridad_certificadora.key" \
    -CAcreateserial \
    -extfile "$temporal/postgresql.ext" \
    -extensions v3_servidor \
    -out "$salida/postgresql_servidor.crt"

case "$nombre_host" in
    *[!0-9.]*) san_web="DNS:$nombre_host" ;;
    *) san_web="IP:$nombre_host" ;;
esac

cat > "$temporal/web.ext" <<EOF
[v3_servidor]
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=$san_web
EOF

openssl req -new -newkey rsa:3072 -sha256 -nodes \
    -subj "/CN=$nombre_host" \
    -keyout "$salida/reddiff_https.key" \
    -out "$temporal/web.csr"

openssl x509 -req -sha256 -days 825 \
    -in "$temporal/web.csr" \
    -CA "$salida/autoridad_certificadora.crt" \
    -CAkey "$salida/autoridad_certificadora.key" \
    -CAcreateserial \
    -extfile "$temporal/web.ext" \
    -extensions v3_servidor \
    -out "$salida/reddiff_https.crt"

rm -f "$salida/autoridad_certificadora.srl"
chmod 0600 "$salida"/*.key
chmod 0644 "$salida"/*.crt

echo "Certificados generados para $nombre_host."
