# Documentación de la API

Esta carpeta contendrá el contrato HTTP del sistema, sus reglas de versionado y ejemplos seguros de solicitudes y respuestas.

La especificación OpenAPI generada por la aplicación será la fuente principal del contrato. Los ejemplos no deben incluir tokens, credenciales, direcciones reales ni configuraciones obtenidas de dispositivos.

Los endpoints iniciales de autenticación, roles y usuarios se documentan en [autenticación y autorización](../seguridad/autenticacion-y-autorizacion.md). Las operaciones que modifican datos requieren sesión y token antifalsificación.

El contrato del inventario y su matriz de permisos se describen en [dispositivos](dispositivos.md).

La carga controlada de archivos, la base de captura remota, las capturas y el historial de versiones se describen en [capturas y versiones](capturas-y-versiones.md).

El cliente consume rutas relativas bajo `/api`. Durante el desarrollo, Angular las dirige a `http://localhost:5088` mediante `cliente/proxy.conf.json`; esta configuración no forma parte de la compilación de producción.
