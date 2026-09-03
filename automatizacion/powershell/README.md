# Automatización con PowerShell

Scripts destinados al entorno Windows de desarrollo. Deben ser compatibles con PowerShell 5.1 mientras no se documente expresamente un requisito superior.

Los archivos se guardan como UTF-8 con BOM porque Windows PowerShell 5.1 necesita esa marca para interpretar correctamente los caracteres no ASCII.

| Script | Finalidad |
| --- | --- |
| `Preparar-EntornoDesarrollo.ps1` | Copiar la configuración local y generar una contraseña criptográficamente aleatoria. |
| `Iniciar-Infraestructura.ps1` | Validar Docker Compose e iniciar los servicios hasta que estén saludables. |
| `Verificar-Infraestructura.ps1` | Comprobar el contenedor y ejecutar una consulta real en PostgreSQL. |
| `Detener-Infraestructura.ps1` | Detener servicios conservando datos o, con una opción explícita, eliminar el volumen local. |
