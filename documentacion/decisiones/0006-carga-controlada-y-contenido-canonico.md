# ADR 0006: Carga controlada y contenido canónico

- Estado: aceptada.
- Fecha: 2026-09-05.

## Contexto

El sistema debe conservar configuraciones históricas obtenidas bajo demanda o mediante carga de archivo, identificar su origen, calcular su integridad y evitar que una nueva evidencia sobrescriba la anterior. Los archivos de laboratorio también pueden contener accidentalmente contraseñas, comunidades o claves.

La captura remota requiere resolver primero el almacenamiento cifrado de credenciales del dispositivo y la confianza explícita de su identidad criptográfica. Aceptar automáticamente una identidad desconocida o simular una conexión produciría una garantía de seguridad falsa.

## Decisión

- Implementar primero la carga controlada para dispositivos autorizados.
- Admitir únicamente archivos de texto UTF-8 con extensiones explícitas y un máximo de 5 MB.
- Normalizar finales de línea a `LF` y Unicode a Form C antes de guardar y calcular SHA-256, de modo que la huella corresponda exactamente al contenido persistido.
- Rechazar caracteres de control y patrones reconocibles de credenciales o comunidades que no estén enmascarados.
- Crear una captura y un número de versión nuevo en cada carga, sin operaciones de actualización o eliminación de contenido histórico.
- Excluir el contenido del listado, los errores y la auditoría; entregarlo únicamente en la consulta autenticada del detalle con caché deshabilitada.
- Mantener la API estrictamente orientada a lectura y evidencia: no exponer comandos arbitrarios ni aplicación de configuraciones.
- Incorporar la captura remota por SSH o NETCONF en un bloque posterior, después de implementar secretos cifrados y validación de identidad del host.

## Consecuencias

La carga de archivos y el historial pueden probarse de extremo a extremo sin introducir credenciales de equipos. El contenido equivalente conserva una representación estable entre Windows y Linux, y una nueva versión siempre mantiene disponible la anterior.

El reconocimiento de datos sensibles es una defensa adicional y no garantiza detectar todos los formatos posibles. El operador continúa siendo responsable de anonimizar y revisar el archivo antes de cargarlo. La captura remota queda incompleta hasta que los controles criptográficos previos estén disponibles.
