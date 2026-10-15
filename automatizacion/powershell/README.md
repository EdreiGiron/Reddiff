# Automatización con PowerShell

Scripts destinados al desarrollo y a los despliegues de RedDiff administrados desde Windows. Deben ser compatibles con PowerShell 5.1 mientras no se documente expresamente un requisito superior.

Los scripts compatibles con Windows PowerShell 5.1 usan texto ASCII o UTF-8 con BOM cuando contienen caracteres no ASCII. Git normaliza sus finales de linea como CRLF al descargarlos en Windows.

| Script                                     | Finalidad                                                                                                        |
| ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------- |
| `Preparar-EntornoDesarrollo.ps1`           | Copiar la configuración local y generar una contraseña criptográficamente aleatoria.                             |
| `Iniciar-Infraestructura.ps1`              | Validar Docker Compose e iniciar los servicios hasta que estén saludables.                                       |
| `produccion-windows/Crear-Paquete-Produccion-Windows.ps1` | Ejecutar validaciones y generar el paquete instalable para Windows x64. |
| `Configurar-UsuarioAplicacion.ps1`         | Crear o actualizar el usuario de PostgreSQL usado por la API con privilegios limitados.                          |
| `Verificar-Infraestructura.ps1`            | Comprobar el contenedor y ejecutar una consulta real en PostgreSQL.                                              |
| `Detener-Infraestructura.ps1`              | Detener servicios conservando datos o, con una opción explícita, eliminar el volumen local.                      |
| `Iniciar-Api.ps1`                          | Cargar temporalmente el secreto local e iniciar la API sin escribir credenciales en el repositorio.              |
| `Preparar-MigracionInicial.ps1`            | Generar la migración inicial y un script SQL idempotente para revisión, sin modificar la base de datos.          |
| `Aplicar-MigracionInicial.ps1`             | Aplicar exclusivamente la migración inicial revisada y verificar el esquema y los privilegios.                   |
| `Preparar-MigracionAccesoRemotoSeguro.ps1` | Generar la migración incremental del acceso remoto y su SQL idempotente para revisión, sin modificar PostgreSQL. |
| `Aplicar-MigracionAccesoRemotoSeguro.ps1`  | Aplicar exclusivamente la migración incremental revisada y comprobar columnas, restricción y privilegios.       |
| `Inicializar-Administrador.ps1`            | Crear localmente los roles funcionales y la primera cuenta administrativa, sin contraseña predeterminada.        |
| `Rotar-CredencialesDesarrollo.ps1`         | Rotar las credenciales de PostgreSQL y verificar ambas identidades antes de retirar respaldos.                   |
| `Obtener-HuellaHostSsh.ps1`                | Consultar las claves públicas SSH y calcular candidatos SHA-256 hexadecimales para verificación independiente.   |

## Produccion con Docker Compose en Windows 11

| Script | Finalidad |
| --- | --- |
| `produccion-docker/Verificar-RequisitosWindows.ps1` | Comprobar Windows 11, WSL 2, Docker Desktop, Compose y el motor Linux. |
| `produccion-docker/Preparar-Produccion.ps1` | Crear configuracion, secretos y certificados, y opcionalmente preparar el host Windows. |
| `produccion-docker/Configurar-HostWindows.ps1` | Instalar la autoridad, configurar firewall y resolucion local. |
| `produccion-docker/Desplegar-Produccion.ps1` | Construir, migrar e iniciar la composicion productiva. |
| `produccion-docker/Inicializar-Administrador.ps1` | Crear la primera cuenta sin conservar su contrasena temporal. |
| `produccion-docker/Verificar-Produccion.ps1` | Comprobar salud, HTTPS, publicacion Syslog y aislamiento de PostgreSQL. |
| `produccion-docker/Respaldar-Produccion.ps1` | Respaldar base de datos, claves e integridad. |
| `produccion-docker/Restaurar-Produccion.ps1` | Restaurar un respaldo verificado mediante confirmacion explicita. |
| `produccion-docker/Actualizar-Produccion.ps1` | Respaldar, actualizar por avance rapido, desplegar y verificar. |
| `produccion-docker/Detener-Produccion.ps1` | Detener contenedores sin eliminar volumenes. |
