# Producción con Docker Compose

## Alcance

Este procedimiento instala RedDiff en un único servidor Linux mediante contenedores reproducibles. La composición incluye:

- Nginx con la compilación productiva de Angular y terminación HTTPS;
- la API ASP.NET Core sobre .NET 10;
- PostgreSQL 18.6 con TLS y almacenamiento persistente;
- operaciones aisladas para migrar la base de datos e inicializar la primera cuenta.

`RedDiff.Procesador` no se despliega porque actualmente contiene únicamente el trabajador de plantilla. La recepción Syslog y el procesamiento funcional vigente se ejecutan como servicios hospedados por `RedDiff.Api`.

## Requisitos del servidor

- distribución Linux con soporte vigente;
- Docker Engine y el complemento Docker Compose;
- OpenSSL y cURL;
- nombre DNS estable para RedDiff;
- al menos 4 vCPU, 8 GB de RAM y 80 GB de almacenamiento como punto de partida;
- hora sincronizada mediante NTP;
- acceso restringido al servidor para los operadores autorizados.

El host necesita las siguientes reglas de red:

| Dirección | Puerto | Uso |
| --- | ---: | --- |
| Entrada | TCP 443 | interfaz web de RedDiff |
| Entrada desde dispositivos | UDP 514 | recepción Syslog |
| Salida hacia dispositivos | TCP 22 | captura SSH |
| Salida hacia dispositivos | TCP 830 | captura NETCONF |

PostgreSQL y el puerto HTTP de la API no se publican en el host.

Antes del despliegue confirme que ningún servicio local, por ejemplo `rsyslog`, esté ocupando UDP 514. Si el puerto ya está reservado, cambie `REDDIFF_SYSLOG_PUERTO_PUBLICO` y configure el mismo puerto de destino en los dispositivos Cisco.

## 1. Preparar configuración y certificados

Desde la raíz del repositorio:

```bash
./automatizacion/bash/produccion/Preparar-Produccion.sh reddiff.labymed.local
```

El script:

- crea `configuracion/entornos/produccion.env` si no existe;
- genera contraseñas independientes para la administración y la aplicación;
- crea una autoridad certificadora interna;
- emite un certificado para PostgreSQL con el nombre `base-datos`;
- emite el certificado HTTPS para el nombre indicado;
- nunca reemplaza secretos o certificados existentes.

Revise antes de continuar:

```bash
nano configuracion/entornos/produccion.env
```

Si LABYMED dispone de una PKI corporativa, sustituya únicamente `reddiff_https.crt` y `reddiff_https.key` por el certificado y la clave correspondientes. PostgreSQL puede continuar utilizando la autoridad interna.

La autoridad `autoridad_certificadora.crt` debe incorporarse al almacén de confianza de los equipos que accederán a RedDiff cuando se utilice el certificado interno.

## 2. Desplegar

```bash
./automatizacion/bash/produccion/Desplegar-Produccion.sh
```

El script valida Compose, construye las imágenes, inicia PostgreSQL, aplica las migraciones con la identidad administrativa, restablece los privilegios limitados de `reddiff_app` e inicia la API y la web. La operación se detiene si un servicio no alcanza un estado saludable.

## 3. Crear la primera cuenta

Esta operación se realiza una sola vez:

```bash
./automatizacion/bash/produccion/Inicializar-Administrador-Produccion.sh
```

La contraseña se solicita sin mostrarla, se entrega al contenedor mediante el archivo reservado para esa operación y dicho archivo se vacía al finalizar. Las cuentas posteriores se administran desde la interfaz web.

## 4. Verificar

```bash
./automatizacion/bash/produccion/Verificar-Produccion.sh
```

La comprobación exige que PostgreSQL, la API y Nginx estén saludables y consulta `/salud/listo` validando la autoridad certificadora configurada.

También puede revisar:

```bash
docker compose \
  --env-file configuracion/entornos/produccion.env \
  --file infraestructura/contenedores/produccion/compose.yml \
  ps
```

## Actualización

Antes de actualizar, cree un respaldo:

```bash
./automatizacion/bash/produccion/Respaldar-Produccion.sh
git pull --ff-only
./automatizacion/bash/produccion/Desplegar-Produccion.sh
./automatizacion/bash/produccion/Verificar-Produccion.sh
```

No utilice `docker compose down --volumes`: eliminaría los datos y las claves protegidas.

## Respaldos

```bash
./automatizacion/bash/produccion/Respaldar-Produccion.sh
```

Cada respaldo contiene:

- `base-datos.dump`, generado por `pg_dump` en formato personalizado;
- `claves-proteccion.tar.gz`, necesario para descifrar las credenciales remotas conservadas;
- `manifiesto.sha256`, para comprobar integridad.

Copie el directorio resultante a un medio cifrado fuera del servidor y pruebe periódicamente una restauración en un ambiente aislado. Perder las claves de Data Protection vuelve irrecuperables los secretos SSH y NETCONF almacenados.

## Reversión

1. Conserve el respaldo previo a cada actualización.
2. Seleccione en Git la etiqueta o confirmación anterior.
3. Ejecute nuevamente `Desplegar-Produccion.sh` para reconstruir la versión anterior.
4. Si la actualización modificó el esquema de forma incompatible, restaure conjuntamente la base de datos y el anillo de claves desde el mismo respaldo.

La reversión de datos no se automatiza porque es destructiva. Debe realizarse durante una ventana autorizada, con RedDiff detenido y después de validar la copia elegida.

## Detención segura

```bash
./automatizacion/bash/produccion/Detener-Produccion.sh
```

El comando detiene los contenedores y conserva ambos volúmenes.

## Secretos y permisos

Los archivos reales de `configuracion/entornos` y `configuracion/secretos` están excluidos de Git. El operador debe limitar su lectura y respaldar por separado la clave de la autoridad certificadora. Ninguna contraseña se incorpora a las imágenes o al archivo Compose.
