#!/bin/sh

set -eu

directorio_certificados=/var/lib/postgresql/certificados
mkdir -p "$directorio_certificados"

cp /run/secrets/postgresql_servidor_certificado "$directorio_certificados/servidor.crt"
cp /run/secrets/postgresql_servidor_clave "$directorio_certificados/servidor.key"
cp /run/secrets/postgresql_ca_certificado "$directorio_certificados/ca.crt"

chown -R postgres:postgres "$directorio_certificados"
chmod 0700 "$directorio_certificados"
chmod 0644 "$directorio_certificados/servidor.crt" "$directorio_certificados/ca.crt"
chmod 0600 "$directorio_certificados/servidor.key"

exec docker-entrypoint.sh "$@" \
    -c ssl=on \
    -c ssl_cert_file="$directorio_certificados/servidor.crt" \
    -c ssl_key_file="$directorio_certificados/servidor.key" \
    -c ssl_ca_file="$directorio_certificados/ca.crt"
