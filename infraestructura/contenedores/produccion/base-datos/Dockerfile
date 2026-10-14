FROM postgres:18.6-alpine

COPY infraestructura/contenedores/produccion/base-datos/iniciar-postgresql-seguro.sh \
    /usr/local/bin/iniciar-postgresql-seguro.sh

RUN chmod 0755 /usr/local/bin/iniciar-postgresql-seguro.sh

ENTRYPOINT ["iniciar-postgresql-seguro.sh"]
CMD ["postgres"]
