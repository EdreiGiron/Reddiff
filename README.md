# RedDiff

Sistema web para capturar, versionar, comparar y auditar configuraciones de dispositivos de red Cisco. Su finalidad es facilitar la trazabilidad de cambios y apoyar la recuperación de la red ante incidentes o configuraciones incorrectas.

> Estado actual: cimentación técnica del proyecto. La solución aún no debe utilizarse en producción.

## Tecnologías principales

- Servidor: ASP.NET Core sobre .NET 10.
- Cliente: Angular 22, TypeScript y Vitest.
- Base de datos: PostgreSQL.
- Contenedores: Docker y Docker Compose.
- Pruebas del servidor: xUnit.

Las versiones exactas se fijan mediante `global.json`, `package-lock.json` y, posteriormente, las imágenes de los contenedores.

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

## Convenciones esenciales

- El código propio utiliza nombres en español, sin tildes ni la letra `ñ` en los identificadores.
- Se conservan en inglés solo los nombres exigidos por las plataformas, por ejemplo `Program.cs`, `Dockerfile`, `package.json`, `src` y `.github/workflows`.
- Las dependencias siempre apuntan hacia el dominio y los casos de uso; el dominio no conoce infraestructura, web ni base de datos.
- Las credenciales, configuraciones reales de dispositivos y archivos `.env` nunca se almacenan en Git.
- Todo cambio funcional debe incluir pruebas en el nivel correspondiente.

Consulte [CONTRIBUCION.md](CONTRIBUCION.md) y [SEGURIDAD.md](SEGURIDAD.md) antes de incorporar cambios.
