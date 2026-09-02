# Infraestructura

Recursos necesarios para ejecutar y desplegar el sistema sin mezclar detalles operativos con las reglas del negocio.

| Carpeta | Finalidad |
| --- | --- |
| `contenedores` | Imágenes y composición de servicios. |
| `base-datos` | Inicialización y recursos operativos de PostgreSQL. |
| `certificados` | Instrucciones y plantillas; nunca llaves privadas versionadas. |
| `proxy-inverso` | Enrutamiento, TLS y cabeceras de seguridad. |
| `observabilidad` | Logs, métricas, trazas y alertas. |

No se almacenan secretos, datos productivos ni configuraciones reales de dispositivos en esta carpeta.
