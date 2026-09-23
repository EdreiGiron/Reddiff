# Flujos de integración continua

`validacion-continua.yml` constituye la primera puerta de calidad reproducible para servidor y cliente. Se ejecuta al enviar cambios a `main`, al abrir o actualizar una solicitud de incorporación dirigida a `main` y manualmente desde GitHub Actions.

El flujo aplica permisos de solo lectura, no utiliza secretos, instala las versiones declaradas por el proyecto y ejecuta en paralelo:

- restauración, compilación y pruebas de la solución .NET;
- instalación reproducible, formato, pruebas y compilación de Angular.

Las acciones externas oficiales se fijan por su SHA completo y conservan como comentario la versión revisada. Cualquier actualización debe verificar primero la publicación oficial y sustituir simultáneamente el SHA y su comentario.
