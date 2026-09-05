# Cliente web de RedDiff

Aplicación Angular para operar RedDiff desde el navegador. Incluye inicio y cierre de sesión, restauración segura de la sesión, administración de usuarios e inventario de dispositivos según el rol autenticado.

## Requisitos

- Node.js y npm en las versiones establecidas por el repositorio;
- API disponible en `http://localhost:5088`;
- administrador inicial creado mediante los scripts del servidor.

## Desarrollo local

Desde la carpeta `cliente`:

```powershell
npm ci
npm start
```

Abra `http://localhost:4200`. `proxy.conf.json` redirige las rutas `/api` y `/salud` al servidor local, por lo que el código no contiene direcciones de entornos concretos.

## Seguridad de la sesión

- La API conserva la sesión en una cookie cifrada, `HttpOnly` y de duración limitada.
- El cliente mantiene en memoria únicamente el resumen del usuario autenticado.
- Las contraseñas no se guardan en `localStorage`, `sessionStorage` ni cookies creadas por Angular.
- El interceptor solicita la protección CSRF y agrega `X-CSRF-TOKEN` a `POST`, `PUT`, `PATCH` y `DELETE`.
- Los guardianes de rutas mejoran la navegación, pero la API sigue siendo la autoridad final para autorizar cada operación.

## Validación

```powershell
npm run build
npm test -- --watch=false
npx prettier --check src angular.json proxy.conf.json README.md
```

La compilación de producción mantiene carga diferida para las páginas de acceso, inicio, usuarios y dispositivos.
