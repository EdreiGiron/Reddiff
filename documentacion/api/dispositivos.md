# API de inventario de dispositivos

## Alcance

El inventario implementa RF-03 sobre la tabla `reddiff.dispositivo` creada por la migración inicial. Registra identificación y punto de conexión, pero no almacena credenciales ni intenta conectarse al equipo durante un alta o una edición.

Los canales de obtención de configuración admitidos son:

- `Ssh`, normalmente en el puerto 22;
- `Netconf`, normalmente en el puerto 830.

Las fuentes opcionales de avisos son `SnmpTrap`, `SnmpInform` y `Syslog`. Un aviso futuro podrá iniciar una captura, pero no se considera una configuración completa. La carga controlada de archivos pertenece al módulo de capturas y no es un protocolo de conexión del inventario.

## Permisos

| Operación | Administrador | Técnico |
| --- | --- | --- |
| Listar y consultar dispositivos | Sí | Sí |
| Registrar y editar dispositivos | Sí | No |
| Autorizar, revocar, desactivar o reactivar | Sí | No |

La API aplica estos permisos aunque un cliente intente omitir las restricciones visuales.

## Endpoints

| Método y ruta | Resultado |
| --- | --- |
| `GET /api/dispositivos` | Lista ordenada por nombre. |
| `GET /api/dispositivos/{id}` | Detalle del dispositivo indicado. |
| `POST /api/dispositivos` | Registra un dispositivo en estado `NoAutorizado`. |
| `PUT /api/dispositivos/{id}` | Actualiza identificación y datos de comunicación. |
| `PATCH /api/dispositivos/{id}/estado` | Cambia a `NoAutorizado`, `Autorizado` o `Inactivo`. |

Las operaciones `POST`, `PUT` y `PATCH` requieren sesión administrativa y encabezado `X-CSRF-TOKEN`.

## Reglas de validación

- `nombre`, `host` y `tipo` son obligatorios;
- `host` debe ser un nombre DNS o una dirección IPv4/IPv6, sin esquema ni ruta;
- `puerto` debe estar entre 1 y 65535;
- el nombre es único;
- la combinación de host normalizado y puerto es única;
- protocolo, fuente de eventos y estado solo aceptan los valores enumerados;
- un alta siempre comienza sin autorización y requiere revisión explícita;
- las altas, ediciones y transiciones aceptadas se registran en `auditoria`, al igual que sus rechazos por validación o conflicto.

No se requieren cambios en el esquema para esta funcionalidad porque las columnas, restricciones e índices ya se encuentran en la migración `Inicial`.
