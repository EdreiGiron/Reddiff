FROM alpine:3.22

RUN apk add --no-cache openssl

COPY infraestructura/contenedores/produccion/certificados/generar-certificados.sh \
    /usr/local/bin/generar-certificados.sh

RUN chmod 0555 /usr/local/bin/generar-certificados.sh

ENTRYPOINT ["/usr/local/bin/generar-certificados.sh"]
