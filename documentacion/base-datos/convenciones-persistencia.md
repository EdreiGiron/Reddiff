# Convenciones de persistencia

## Responsabilidades

- `RedDiff.Dominio` contendrá entidades y reglas sin referencias a Entity Framework Core.
- `RedDiff.Aplicacion` declarará contratos de persistencia requeridos por los casos de uso.
- `RedDiff.Infraestructura` implementará esos contratos mediante `ContextoRedDiff`.
- `RedDiff.Api` solamente compondrá dependencias y expondrá los resultados por HTTP.

## PostgreSQL

- Esquema de aplicación: `reddiff`.
- Historial de migraciones: `reddiff.__historial_migraciones`.
- Identidad de migración y administración: `reddiff_admin`.
- Identidad de ejecución de la API: `reddiff_app`.
- Versión objetivo local: PostgreSQL 18.6.

Las tablas, columnas, restricciones e índices utilizarán nombres descriptivos en español y formato `snake_case`. Las configuraciones de Entity Framework Core se mantendrán separadas por entidad dentro de Infraestructura.

## Secretos y diagnóstico

Las cadenas de conexión no se guardan en archivos versionados. En desarrollo, `Iniciar-Api.ps1` lee el secreto local, lo entrega al proceso mediante una variable temporal y restaura el entorno al finalizar.

`EnableSensitiveDataLogging` no debe habilitarse. Los errores públicos y las comprobaciones de salud tampoco deben devolver cadenas de conexión, consultas, nombres de archivos de secretos ni mensajes internos del proveedor.

El contenido de una configuración no se incluirá en listados, errores, auditorías ni registros de diagnóstico. Solo la consulta autenticada del detalle puede devolverlo y debe impedir su almacenamiento en caché.

## Migraciones

La migración inicial fue revisada, incluida en Git y aplicada explícitamente con la cuenta administradora. Cualquier migración futura deberá seguir el mismo procedimiento de generación, revisión del SQL y aplicación confirmada. La API no aplicará migraciones automáticamente durante su inicio.
