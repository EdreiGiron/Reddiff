# API de inventario de dispositivos

## Alcance

El inventario implementa RF-03 sobre la tabla `reddiff.dispositivo` creada por la migración inicial. Registra identificación, punto de conexión y el estado de configuración del acceso remoto. La API permite administrar el bloque protegido, pero no intenta conectarse al equipo durante un alta, una edición o una actualización de credenciales.

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
| Consultar metadatos del acceso remoto      | Sí            | No      |
| Configurar, reemplazar o revocar el acceso | Sí            | No      |

La API aplica estos permisos aunque un cliente intente omitir las restricciones visuales.

## Endpoints

| Método y ruta                                 | Resultado                                                              |
| --------------------------------------------- | ---------------------------------------------------------------------- |
| `GET /api/dispositivos`                       | Lista ordenada por nombre.                                             |
| `GET /api/dispositivos/{id}`                  | Detalle del dispositivo indicado.                                      |
| `POST /api/dispositivos`                      | Registra un dispositivo en estado `NoAutorizado`.                      |
| `PUT /api/dispositivos/{id}`                  | Actualiza identificación y datos de comunicación.                      |
| `PATCH /api/dispositivos/{id}/estado`         | Cambia a `NoAutorizado`, `Autorizado` o `Inactivo`.                    |
| `GET /api/dispositivos/{id}/acceso-remoto`    | Devuelve estado, usuario, algoritmo, huella y fecha; nunca el secreto. |
| `PUT /api/dispositivos/{id}/acceso-remoto`    | Configura o reemplaza el bloque protegido.                             |
| `DELETE /api/dispositivos/{id}/acceso-remoto` | Revoca y elimina el bloque completo.                                   |

Las operaciones `POST`, `PUT`, `PATCH` y `DELETE` requieren sesión administrativa y encabezado `X-CSRF-TOKEN`. La consulta de metadatos del acceso también exige el rol `Administrador`.

Ninguna respuesta contiene `secretoAcceso` ni el valor cifrado persistido. Configurar el bloque no inicia una captura ni prueba la conexión; los conectores de solo lectura permanecen deshabilitados.

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
- las altas, ediciones, transiciones y mutaciones del acceso aceptadas se registran en `auditoria`, al igual que sus rechazos por validación o conflicto;
- el detalle de auditoría no contiene el usuario técnico, el secreto ni la huella del equipo.

El bloque protegido fue incorporado mediante la migración incremental `20260905061958_AgregarAccesoRemotoSeguro`, generada, revisada y aplicada por separado antes de habilitar esta API.
