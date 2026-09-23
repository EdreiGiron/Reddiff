# ADR 0016: Consulta segura y paginada de auditoría

## Estado

Aceptada.

## Contexto

Las operaciones de autenticación, inventario, capturas, comparaciones, verificaciones, usuarios y eventos ya conservaban auditorías. Faltaba una forma administrativa de consultar esa evidencia sin acceder directamente a PostgreSQL ni exponer información técnica sensible.

## Decisión

- Incorporar un endpoint de solo lectura reservado al rol `Administrador`.
- Consultar en servidor con filtros por usuario, acción, entidad, estado y rango de fechas.
- Obtener un catálogo administrativo con los usuarios, acciones y entidades realmente disponibles para presentar filtros guiados sin alterar la coincidencia exacta aplicada en servidor.
- Paginar los resultados, con 25 registros por defecto y un máximo de 100 por solicitud.
- Ordenar por fecha e identificador descendentes para presentar primero la actividad reciente.
- Mostrar el usuario asociado o identificar el registro como generado por un proceso del sistema.
- Conservar en la respuesta únicamente identificadores, resultado y detalle operacional ya saneado.
- No copiar configuraciones, contraseñas ni secretos a la bitácora o a su interfaz.
- Mantener la auditoría inmutable desde la API: no se ofrecen operaciones de creación, edición ni eliminación manual.

## Consecuencias

El administrador puede reconstruir el uso del sistema desde la interfaz web y aplicar filtros mediante listas legibles sin descargar toda la tabla ni memorizar identificadores o nombres internos. El técnico no puede consultar la bitácora. La etapa utiliza el modelo persistente existente y no requiere una migración de base de datos.
