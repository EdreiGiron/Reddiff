FROM node:24.19.0-alpine AS compilacion

WORKDIR /fuente
COPY cliente/package.json cliente/package-lock.json ./
RUN npm ci

COPY cliente/ ./
RUN npm run build -- --configuration production

FROM nginx:1.28-alpine AS ejecucion

COPY infraestructura/contenedores/produccion/web/nginx.conf /etc/nginx/nginx.conf
COPY --from=compilacion /fuente/dist/reddiff-cliente/browser /usr/share/nginx/html

EXPOSE 8443

USER 101:101

ENTRYPOINT ["nginx"]
CMD ["-g", "daemon off;"]
