# Cliente web local

## Alcance

El cliente permite iniciar y cerrar sesión, restaurar una sesión vigente, consultar el inventario, cargar configuraciones enmascaradas, solicitar capturas SSH y revisar el historial. El rol `Administrador` también puede gestionar usuarios, dispositivos y su acceso remoto protegido. El rol `Tecnico` puede operar capturas y consultar los equipos, pero no modificar el inventario, administrar credenciales ni abrir la administración de usuarios.

## Requisitos previos

1. La migración inicial debe estar aplicada.
2. El administrador inicial debe existir.
3. PostgreSQL y la API deben estar activos y saludables.

Desde la raíz del repositorio, inicie la infraestructura y la API en una primera terminal:

```powershell
.\automatizacion\powershell\Iniciar-Infraestructura.ps1
.\automatizacion\powershell\Iniciar-Api.ps1
```

## Iniciar Angular

En una segunda terminal:

```powershell
Set-Location C:\Proyectos\RedDiff\cliente
npm ci
npm start
```

Abra `http://localhost:4200`. No abra directamente los archivos generados ni use el puerto de la API como dirección del cliente.

El servidor de desarrollo de Angular utiliza `proxy.conf.json` para reenviar `/api` y `/salud` hacia `http://localhost:5088`. Así, la aplicación utiliza rutas relativas y las cookies permanecen en un contexto de mismo sitio durante el desarrollo.

## Comprobación funcional

1. Inicie sesión con `administrador.principal` y la contraseña creada localmente.
2. Confirme que la cabecera muestre el usuario y el rol `Administrador`.
3. Abra **Usuarios** y compruebe que aparece la cuenta administrativa.
4. Cree un usuario de prueba con el rol `Tecnico` y una contraseña temporal de al menos 12 caracteres.
5. Abra **Dispositivos**, registre un equipo de laboratorio y confirme que comience como **No autorizado**.
6. Revise sus datos, autorícelo y confirme que el estado cambie sin recargar la página.
7. En la fila autorizada, seleccione **Configurar acceso** e ingrese únicamente datos ficticios de laboratorio.
8. Registre un usuario técnico de solo lectura, un secreto de prueba, el algoritmo de la clave del host y una huella hexadecimal SHA-256 de 64 caracteres; marque la confirmación solo para esta prueba controlada.
9. Confirme que la fila indique **Configurado**. Abra **Reemplazar acceso** y compruebe que aparezcan los metadatos, pero que el campo del secreto permanezca vacío.
10. Reemplace el acceso con otro secreto ficticio y luego use **Revocar credencial**; confirme que la fila vuelva a **Sin configurar**.
11. Prepare un archivo `prueba.cfg` con datos ficticios y sin credenciales, por ejemplo `hostname laboratorio`.
12. Abra **Capturas**, seleccione el dispositivo autorizado, cargue `prueba.cfg` y confirme que se cree la versión 1.
13. Abra el detalle, compruebe el contenido, el comentario y la huella SHA-256.
14. Modifique el archivo con una segunda línea, vuelva a cargarlo y confirme que se cree la versión 2 sin desaparecer la versión 1.
15. Cierre la sesión administrativa e ingrese con el usuario técnico.
16. Confirme que el técnico vea **Dispositivos** y **Capturas**, pero no **Usuarios** ni controles de modificación del equipo o sus credenciales.
17. Confirme que el técnico pueda cargar un archivo enmascarado en el dispositivo autorizado.
18. Escriba `http://localhost:4200/usuarios` y confirme que el sistema lo devuelva al inicio.
19. Cierre la sesión técnica, ingrese nuevamente como administrador y desactive la cuenta de prueba.

La prueba de una conexión SSH real se realiza aparte y únicamente contra GNS3 o un equipo autorizado. Siga [captura SSH en laboratorio](captura-ssh-laboratorio.md) para obtener y verificar la huella, preparar una cuenta de consulta y solicitar la captura desde la nueva tarjeta **Captura SSH**.

No escriba contraseñas en comandos, archivos de configuración, capturas de pantalla, documentación o mensajes. Sustituya todo dato sensible del archivo por `[PROTEGIDO]` antes de cargarlo. El navegador no debe conservar las credenciales de acceso después de enviar el formulario.

## Validación automatizada

Desde `cliente`:

```powershell
npm run build
npm test -- --watch=false
npx prettier --check src angular.json proxy.conf.json README.md
```

Las pruebas comprueban el envío de cookies, la inclusión de CSRF en operaciones de escritura, la restauración de sesión, los guardianes de rutas, las validaciones del acceso, la restricción que impide desactivar la propia cuenta y la presentación del inventario y del historial según el rol. También verifican que el formulario no reciba el secreto persistido, limpie el secreto nuevo después de enviarlo y registre el resultado de una captura SSH sin reemplazar versiones anteriores.

## Solución de problemas

- **La página no inicia sesión:** compruebe primero `http://localhost:5088/salud/listo` y revise la terminal de la API.
- **Una operación devuelve 400:** recargue la página para renovar la protección CSRF y repita la acción una sola vez.
- **Una operación devuelve 401:** la sesión expiró o la cuenta fue desactivada; vuelva a iniciar sesión.
- **Una operación devuelve 403:** el usuario autenticado no tiene el rol requerido.
- **El proxy muestra conexión rechazada:** la API no está activa en `http://localhost:5088`.
