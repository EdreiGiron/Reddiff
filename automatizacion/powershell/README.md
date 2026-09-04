# Automatización con PowerShell

Scripts destinados al entorno Windows de desarrollo. Deben ser compatibles con PowerShell 5.1 mientras no se documente expresamente un requisito superior.

Los archivos se guardan como UTF-8 con BOM porque Windows PowerShell 5.1 necesita esa marca para interpretar correctamente los caracteres no ASCII.

| Script | Finalidad |
| --- | --- |
| `Preparar-EntornoDesarrollo.ps1` | Copiar la configuración local y generar una contraseña criptográficamente aleatoria. |
| `Iniciar-Infraestructura.ps1` | Validar Docker Compose e iniciar los servicios hasta que estén saludables. |
| `Configurar-UsuarioAplicacion.ps1` | Crear o actualizar el usuario de PostgreSQL usado por la API con privilegios limitados. |
| `Verificar-Infraestructura.ps1` | Comprobar el contenedor y ejecutar una consulta real en PostgreSQL. |
| `Detener-Infraestructura.ps1` | Detener servicios conservando datos o, con una opción explícita, eliminar el volumen local. |
| `Iniciar-Api.ps1` | Cargar temporalmente el secreto local e iniciar la API sin escribir credenciales en el repositorio. |
| `Preparar-MigracionInicial.ps1` | Generar la migración inicial y un script SQL idempotente para revisión, sin modificar la base de datos. |
| `Aplicar-MigracionInicial.ps1` | Aplicar exclusivamente la migración inicial revisada y verificar el esquema y los privilegios. |
| `Inicializar-Administrador.ps1` | Crear localmente los roles funcionales y la primera cuenta administrativa, sin contraseña predeterminada. |
| `Rotar-CredencialesDesarrollo.ps1` | Rotar las credenciales de PostgreSQL y verificar ambas identidades antes de retirar respaldos. |
