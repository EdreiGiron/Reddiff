# Guía de contribución

## Flujo de trabajo

1. Partir de una rama `main` actualizada y estable.
2. Crear una rama pequeña con uno de estos prefijos:
   - `funcionalidad/`
   - `correccion/`
   - `refactorizacion/`
   - `documentacion/`
3. Implementar un cambio de alcance limitado.
4. Ejecutar formato, compilación y pruebas.
5. Revisar que no se hayan agregado secretos ni archivos generados.
6. Integrar el cambio solamente cuando las verificaciones sean correctas.

## Mensajes de commit

Se utilizan prefijos compatibles con Conventional Commits:

- `feat:` nueva funcionalidad.
- `fix:` corrección de un defecto.
- `refactor:` mejora interna sin cambiar el comportamiento esperado.
- `test:` incorporación o ajuste de pruebas.
- `docs:` documentación.
- `chore:` mantenimiento del repositorio.
- `ci:` integración continua.

Ejemplo:

```text
feat: agregar registro de dispositivos
```

## Nombres y organización

- Carpetas, clases, métodos, propiedades y archivos propios se nombran en español.
- Los identificadores de código no llevan tildes ni caracteres especiales.
- Cada clase mantiene una responsabilidad clara.
- Los controladores coordinan solicitudes; no contienen reglas del negocio.
- Los casos de uso se ubican en el proyecto `RedDiff.Aplicacion` y las reglas centrales en `RedDiff.Dominio`.
- El acceso a datos, red, archivos y servicios externos se implementa en Infraestructura.
- En Angular, cada capacidad se organiza dentro de `modulos`; lo reutilizable pertenece a `compartido` y los servicios transversales a `nucleo`.

## Verificaciones obligatorias

Servidor:

```powershell
dotnet build .\RedDiff.slnx --configuration Debug
dotnet test .\RedDiff.slnx --configuration Debug --no-build
```

Cliente:

```powershell
Set-Location .\cliente
npm run build
npm test -- --watch=false
npx prettier --check src
npm audit
```

## Lista de revisión

- [ ] El cambio responde a un requisito identificado.
- [ ] No rompe las dependencias de la arquitectura.
- [ ] Incluye pruebas suficientes.
- [ ] No expone credenciales, configuraciones ni datos sensibles.
- [ ] Mantiene los nombres y la documentación en español.
- [ ] Compila sin errores y supera todas las pruebas.
