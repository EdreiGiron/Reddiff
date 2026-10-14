#!/usr/bin/env bash

set -Eeuo pipefail

RUTA_SCRIPT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$RUTA_SCRIPT/comun.sh"

validar_requisitos_produccion

echo "Validando la configuración de Docker Compose..."
ejecutar_compose config --quiet

echo "Construyendo las imágenes de RedDiff..."
ejecutar_compose --profile herramientas build base-datos migrador api web

echo "Iniciando PostgreSQL..."
ejecutar_compose up --detach base-datos
esperar_salud_contenedor base-datos 60

echo "Aplicando migraciones con la cuenta administrativa..."
ejecutar_compose --profile herramientas run --rm migrador

echo "Restableciendo los privilegios mínimos de la cuenta de aplicación..."
ejecutar_compose exec --no-TTY base-datos \
    sh /opt/reddiff/herramientas/configurar-usuario-aplicacion.sh

echo "Iniciando la API y el cliente web..."
ejecutar_compose up --detach api web
esperar_salud_contenedor api 60
esperar_salud_contenedor web 60

ejecutar_compose ps

echo "RedDiff se desplegó correctamente."
echo "Si es la primera instalación, ejecute Inicializar-Administrador-Produccion.sh."
