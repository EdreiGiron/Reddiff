# RedDiff

Sistema web para capturar, versionar, comparar y auditar configuraciones de dispositivos de red Cisco. Su finalidad es facilitar la trazabilidad de cambios y apoyar la recuperación de la red ante incidentes o configuraciones incorrectas.

> Estado actual: cimentación técnica, modelo persistente, identidad, acceso web, inventario, carga controlada, captura SSH y NETCONF, recepción Syslog, comparación diferencial, líneas base y verificación de cumplimiento implementados. La solución aún no debe utilizarse en producción.

## Tecnologías principales

- Servidor: ASP.NET Core sobre .NET 10.
- Cliente: Angular 22, TypeScript y Vitest.
- Base de datos: PostgreSQL.
- Contenedores: Docker y Docker Compose.
- Pruebas del servidor: xUnit.

Las versiones exactas se fijan mediante `global.json`, `Directory.Packages.props`, `package-lock.json`, el manifiesto local de herramientas .NET y las imágenes de los contenedores.

## Estructura general

```text
RedDiff/
|-- cliente/                  Aplicación web Angular
|-- servidor/                 API, procesos y reglas del negocio
|-- infraestructura/          Docker, base de datos y despliegue
|-- documentacion/            Arquitectura, requisitos y guías
|-- automatizacion/           Scripts repetibles de mantenimiento
|-- configuracion/            Plantillas seguras de configuración
|-- .github/                  Automatización del repositorio
|-- RedDiff.slnx              Solución principal de .NET
`-- global.json               Versión requerida del SDK de .NET
```

La explicación detallada se encuentra en [documentacion/arquitectura/estructura-repositorio.md](documentacion/arquitectura/estructura-repositorio.md).

## Validación del servidor

Desde la raíz del repositorio:

```powershell
dotnet restore .\RedDiff.slnx
dotnet build .\RedDiff.slnx --configuration Debug --no-restore
dotnet test .\RedDiff.slnx --configuration Debug --no-build
```

## Validación del cliente

```powershell
Set-Location .\cliente
npm ci
npm run build
npm test -- --watch=false
npx prettier --check src
```

## Base de datos local

La infraestructura de desarrollo utiliza PostgreSQL en Docker. La preparación genera una contraseña aleatoria local y nunca la incorpora al repositorio.

```powershell
.\automatizacion\powershell\Preparar-EntornoDesarrollo.ps1
.\automatizacion\powershell\Iniciar-Infraestructura.ps1
.\automatizacion\powershell\Configurar-UsuarioAplicacion.ps1
.\automatizacion\powershell\Verificar-Infraestructura.ps1
```

Para detener los contenedores conservando los datos:

```powershell
.\automatizacion\powershell\Detener-Infraestructura.ps1
```

Consulte [documentacion/operacion/base-datos-local.md](documentacion/operacion/base-datos-local.md) para conocer el procedimiento completo.

## API local

La API utiliza una cuenta de PostgreSQL distinta de la cuenta administradora. Su contraseña se carga temporalmente desde un archivo ignorado por Git:

```powershell
.\automatizacion\powershell\Iniciar-Api.ps1
```

Comprobaciones disponibles:

- `GET /salud/vivo`: confirma que el proceso responde;
- `GET /salud/listo`: confirma la conexión de la API con PostgreSQL.

Consulte [documentacion/operacion/api-local.md](documentacion/operacion/api-local.md) para conocer el procedimiento completo.

La autenticación utiliza una sesión web cifrada y autorización mediante los roles `Administrador` y `Tecnico`. Consulte [documentacion/seguridad/autenticacion-y-autorizacion.md](documentacion/seguridad/autenticacion-y-autorizacion.md).

El inventario admite SSH y NETCONF como canales de obtención de configuración, y SNMP trap, SNMP inform o Syslog como fuentes opcionales de avisos. Consulte [documentacion/api/dispositivos.md](documentacion/api/dispositivos.md).

El acceso remoto exige un secreto cifrado y la huella SHA-256 previamente aprobada de la clave del host. Su administración no devuelve el secreto almacenado y está limitada al rol `Administrador`. El diseño y sus límites se describen en [documentacion/seguridad/acceso-remoto-dispositivos.md](documentacion/seguridad/acceso-remoto-dispositivos.md).

Los usuarios autenticados pueden cargar archivos de configuración previamente enmascarados en dispositivos autorizados y consultar su historial inmutable y huellas SHA-256. Consulte [documentacion/api/capturas-y-versiones.md](documentacion/api/capturas-y-versiones.md).

La comparación diferencial permite seleccionar dos versiones del mismo dispositivo, identificar líneas agregadas, eliminadas o modificadas y conservar el resultado como evidencia de solo lectura. Consulte [documentacion/api/comparaciones.md](documentacion/api/comparaciones.md).

Las líneas base permiten definir criterios inmutables por dispositivo o tipo, asociar opcionalmente una versión histórica estable y verificar versiones sin alterar el equipo. Administradores y técnicos pueden ejecutar y consultar verificaciones; únicamente el administrador crea, activa o desactiva líneas base. Consulte [documentacion/api/baselines-y-verificaciones.md](documentacion/api/baselines-y-verificaciones.md).

La captura remota mediante SSH o NETCONF comprueba autorización, acceso protegido, límite de tiempo, identidad criptográfica del host y contenido antes de crear una versión. SSH utiliza una sesión controlada que solo desactiva la paginación y ejecuta `show running-config view full`; NETCONF envía exclusivamente un RPC `<get-config>` sobre el almacén `running`. Ningún adaptador admite órdenes proporcionadas por el usuario ni modifica la configuración del equipo. Las pruebas controladas se describen en [captura SSH](documentacion/operacion/captura-ssh-laboratorio.md) y [captura NETCONF](documentacion/operacion/captura-netconf-laboratorio.md).

La recepción Syslog permanece deshabilitada hasta configurarla explícitamente. Un datagrama aceptado debe proceder de la IP de un dispositivo autorizado, se limita y sanea antes de calcular su huella, y puede originar una sola captura mediante el protocolo de lectura ya configurado. Consulte [eventos Syslog en laboratorio](documentacion/operacion/eventos-syslog-laboratorio.md).

El administrador puede consultar una bitácora paginada de accesos y operaciones desde la pantalla **Auditoría**. La respuesta conserva identificadores y resultados, pero excluye configuraciones completas, contraseñas y secretos. Consulte [bitácora de auditoría](documentacion/api/auditorias.md).

## Cliente web local

Con la API activa en `http://localhost:5088`, inicie Angular en otra terminal:

```powershell
Set-Location .\cliente
npm ci
npm start
```

Abra `http://localhost:4200`. El proxy de desarrollo dirige `/api` y `/salud` a la API sin almacenar credenciales ni tokens en el navegador. Consulte [documentacion/operacion/cliente-web-local.md](documentacion/operacion/cliente-web-local.md).

## Convenciones esenciales

- El código propio utiliza nombres en español, sin tildes ni la letra `ñ` en los identificadores.
- Se conservan en inglés solo los nombres exigidos por las plataformas, por ejemplo `Program.cs`, `Dockerfile`, `package.json`, `src` y `.github/workflows`.
- Las dependencias siempre apuntan hacia el dominio y los casos de uso; el dominio no conoce infraestructura, web ni base de datos.
- Las credenciales, configuraciones reales de dispositivos y archivos `.env` nunca se almacenan en Git.
- Todo cambio funcional debe incluir pruebas en el nivel correspondiente.

Consulte [CONTRIBUCION.md](CONTRIBUCION.md) y [SEGURIDAD.md](SEGURIDAD.md) antes de incorporar cambios.
