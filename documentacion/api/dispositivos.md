# API de inventario de dispositivos

## Alcance

El inventario implementa RF-03 sobre la tabla `reddiff.dispositivo` creada por la migración inicial. Registra identificación y punto de conexión. El dominio ya prepara un bloque cifrado de acceso remoto, pero esta etapa no lo expone por API ni intenta conectarse al equipo durante un alta o una edición.

Los canales de obtención de configuración admitidos son:

- `Ssh`, normalmente en el puerto 22;
- `Netconf`, normalmente en el puerto 830.

Las fuentes opcionales de avisos son `SnmpTrap`, `SnmpInform` y `Syslog`. Un aviso futuro podrá iniciar una captura, pero no se considera una configuración completa. La carga controlada de archivos pertenece al módulo de capturas y no es un protocolo de conexión del inventario.

## Permisos

| Operación                                  | Administrador | Técnico |
| ------------------------------------------ | ------------- | ------- |
| Listar y consultar dispositivos            | Sí            | Sí      |
| Registrar y editar dispositivos            | Sí            | No      |
| Autorizar, revocar, desactivar o reactivar | Sí            | No      |

La API aplica estos permisos aunque un cliente intente omitir las restricciones visuales.

## Endpoints

| Método y ruta                         | Resultado                                           |
| ------------------------------------- | --------------------------------------------------- |
| `GET /api/dispositivos`               | Lista ordenada por nombre.                          |
| `GET /api/dispositivos/{id}`          | Detalle del dispositivo indicado.                   |
| `POST /api/dispositivos`              | Registra un dispositivo en estado `NoAutorizado`.   |
| `PUT /api/dispositivos/{id}`          | Actualiza identificación y datos de comunicación.   |
| `PATCH /api/dispositivos/{id}/estado` | Cambia a `NoAutorizado`, `Autorizado` o `Inactivo`. |

Las operaciones `POST`, `PUT` y `PATCH` requieren sesión administrativa y encabezado `X-CSRF-TOKEN`.

No existe todavía un endpoint para configurar credenciales o iniciar una captura remota. Esas operaciones permanecerán deshabilitadas hasta aplicar la migración incremental revisada y completar los conectores de solo lectura.

## Reglas de validación

- `nombre`, `host` y `tipo` son obligatorios;
- `host` debe ser un nombre DNS o una dirección IPv4/IPv6, sin esquema ni ruta;
- `puerto` debe estar entre 1 y 65535;
- el nombre es único;
- la combinación de host normalizado y puerto es única;
- protocolo, fuente de eventos y estado solo aceptan los valores enumerados;
- un alta siempre comienza sin autorización y requiere revisión explícita;
- un acceso remoto solo puede asociarse a un dispositivo autorizado y requiere una huella SHA-256 de la clave del host aprobada previamente;
- cambiar host, puerto o protocolo, revocar la autorización o desactivar el dispositivo invalida el acceso remoto;
- las altas, ediciones y transiciones aceptadas se registran en `auditoria`, al igual que sus rechazos por validación o conflicto.

La gestión actual del inventario no requiere datos de acceso. El bloque protegido se incorpora mediante la migración incremental `20260905061958_AgregarAccesoRemotoSeguro`, generada, revisada y aplicada por separado antes de habilitar su API.
