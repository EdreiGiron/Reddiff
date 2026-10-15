# Integración continua

## Propósito

La integración continua convierte las verificaciones locales de RedDiff en una puerta de calidad visible y repetible dentro de GitHub. Su objetivo es detectar errores de compilación, pruebas o formato antes de considerar estable un cambio; no realiza despliegues ni sustituye las pruebas funcionales en el laboratorio Cisco.

## Activación

El flujo `.github/workflows/validacion-continua.yml` se ejecuta en estos casos:

- cada envío a la rama `main`;
- cada solicitud de incorporación cuyo destino sea `main`;
- una ejecución manual desde la pestaña **Actions**.

La concurrencia cancela una ejecución anterior de la misma rama cuando llega un cambio más reciente. Así se evita consumir tiempo en una revisión que ya fue reemplazada.

## Puertas aplicadas

| Trabajo         | Verificaciones                                                          |
| --------------- | ----------------------------------------------------------------------- |
| Servidor .NET   | Restauración, compilación `Release` y todas las pruebas de la solución. |
| Cliente Angular | `npm ci`, Prettier, pruebas Vitest y compilación de producción.         |
| Imágenes y Docker Compose | Validación de Compose y construcción de certificados, PostgreSQL, API y web. |
| Paquete Windows | Validación sintáctica de PowerShell y publicación nativa alternativa `win-x64`. |

Servidor y cliente se ejecutan en paralelo. Las imágenes, Compose y la automatización PowerShell se validan únicamente cuando ambos trabajos terminan correctamente. Las pruebas de API usan sus dobles controlados y una base de datos en memoria; el flujo no abre conexiones SSH o NETCONF ni recibe Syslog real.

## Seguridad y reproducibilidad

- El `GITHUB_TOKEN` recibe únicamente permiso de lectura de contenido.
- El flujo no utiliza eventos `pull_request_target`, secretos ni comandos derivados de datos aportados por usuarios.
- `actions/checkout` no conserva credenciales después de obtener el código.
- Las acciones oficiales se fijan mediante SHA completo para impedir que una etiqueta móvil cambie el código ejecutado.
- .NET se resuelve desde `global.json`; Node.js se fija en `24.19.0` y npm instala exactamente `package-lock.json` mediante `npm ci`.
- No se cargan configuraciones, resultados ni artefactos que puedan contener información sensible.

## Equivalencia local

Antes de subir un cambio se deben ejecutar las mismas comprobaciones desde PowerShell:

```powershell
Set-Location C:\Proyectos\RedDiff

dotnet restore .\RedDiff.slnx
dotnet build .\RedDiff.slnx --configuration Release --no-restore
dotnet test .\RedDiff.slnx --configuration Release --no-build

Set-Location .\cliente
npm ci
npx prettier --check src
npm test -- --watch=false
npm run build
Set-Location ..
```

Un resultado local correcto no garantiza por sí solo que GitHub finalice correctamente: después del `push` también debe comprobarse que **Servidor .NET**, **Cliente Angular**, **Imágenes y Docker Compose** y **Paquete Windows** aparezcan en verde.

## Protección recomendada de `main`

Después de validar el primer flujo en GitHub, puede configurarse una regla de protección para requerir los cuatro trabajos antes de integrar solicitudes de incorporación. Esta regla se configura en GitHub y no debe activarse hasta confirmar los nombres exactos que aparecen en la primera ejecución.
