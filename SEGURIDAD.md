# Política de seguridad

## Información sensible

Este repositorio no debe contener:

- contraseñas, tokens, llaves privadas o cadenas de conexión reales;
- archivos `.env` con valores locales o productivos;
- respaldos de base de datos;
- configuraciones reales descargadas de dispositivos;
- direcciones, nombres de comunidad SNMP u otros datos de una red real;
- certificados privados o archivos de credenciales.

Los secretos de desarrollo se administrarán mediante variables de entorno o Secret Manager de .NET. En despliegues se utilizará el mecanismo de secretos propio de la plataforma.

## Acceso a dispositivos

- Utilizar una cuenta técnica exclusiva para el sistema.
- Aplicar privilegio mínimo y operaciones de solo lectura durante la captura.
- Preferir protocolos cifrados, como SSH y NETCONF sobre SSH.
- No registrar contraseñas ni el contenido completo de configuraciones en los logs.
- Establecer tiempos de espera, límites de concurrencia y validación estricta de huellas o identidades cuando corresponda.

## Protección de la aplicación

- Autenticación y autorización por roles en todos los recursos protegidos.
- Validación de datos en los límites de entrada.
- Cifrado en tránsito y protección de datos sensibles en reposo.
- Consultas parametrizadas y acceso a datos mediante componentes controlados.
- Registro de auditoría sin secretos ni datos innecesarios.
- Dependencias fijadas, revisadas y actualizadas de forma controlada.

## Reporte de vulnerabilidades

No publique detalles explotables en una incidencia pública. Registre el hallazgo por un canal privado del responsable del proyecto, incluyendo componente afectado, pasos de reproducción, impacto y una propuesta de mitigación si está disponible.
