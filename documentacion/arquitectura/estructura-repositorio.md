# Estructura del repositorio

## Principio de organización

El proyecto utiliza un monorepositorio para mantener cliente, servidor, infraestructura, pruebas y documentación bajo la misma versión. El servidor sigue arquitectura limpia y el cliente se divide por capacidades funcionales.

## Servidor

| Proyecto | Responsabilidad | Dependencias permitidas |
| --- | --- | --- |
| `RedDiff.Dominio` | Entidades, objetos de valor, reglas e interfaces centrales. | Ningún otro proyecto de la solución. |
| `RedDiff.Aplicacion` | Casos de uso, contratos, validación y puertos de salida. | Dominio. |
| `RedDiff.Infraestructura` | PostgreSQL, red, archivos y servicios externos. | Aplicación y Dominio. |
| `RedDiff.Api` | Endpoints HTTP, autenticación y composición de dependencias. | Aplicación e Infraestructura. |
| `RedDiff.Procesador` | Trabajos asíncronos, cola y capturas programadas. | Aplicación e Infraestructura. |

Las pruebas se separan en unitarias, integración y arquitectura para detectar tanto defectos funcionales como dependencias incorrectas.

## Cliente

```text
cliente/src/app/
|-- nucleo/       Servicios transversales creados una sola vez
|-- compartido/   Componentes, directivas y utilidades reutilizables
|-- modulos/      Capacidades funcionales con carga diferida
|-- aplicacion.ts
|-- configuracion-aplicacion.ts
`-- rutas-aplicacion.ts
```

Los módulos funcionales no deben acceder directamente a detalles internos de otros módulos. La comunicación compartida se realiza mediante contratos y servicios bien delimitados.

## Infraestructura y soporte

```text
infraestructura/
|-- contenedores/    Dockerfiles y composición local
|-- base-datos/      Inicialización y recursos operativos de PostgreSQL
|-- proxy-inverso/   Entrada HTTP, TLS y enrutamiento cuando se habilite
`-- observabilidad/  Logs, métricas, trazas y tableros

automatizacion/
|-- powershell/      Tareas para el entorno Windows del proyecto
`-- bash/            Tareas utilizadas en Linux y contenedores

configuracion/       Plantillas sin secretos para los distintos entornos
```

## Reglas de dependencia

1. Dominio no depende de frameworks, base de datos, HTTP ni dispositivos Cisco.
2. Aplicación expresa lo que el sistema hace, pero no cómo se conecta a recursos externos.
3. Infraestructura implementa los contratos definidos hacia el interior.
4. API y Procesador son puntos de entrada independientes y no comparten reglas mediante referencias directas entre ellos.
5. Ningún controlador, componente visual o repositorio concentra varias responsabilidades del negocio.
6. Cada nueva capacidad debe indicar su requisito, sus pruebas y la decisión de arquitectura relevante.
