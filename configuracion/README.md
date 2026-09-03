# Configuración

Plantillas de configuración compartidas y seguras para los distintos entornos.

Solo se versionan ejemplos sin secretos. Los valores locales se proporcionarán mediante variables de entorno, Secret Manager de .NET u otros mecanismos definidos para cada despliegue.

Las plantillas reutilizables se ubican en `plantillas` y emplean valores ficticios claramente identificados.

- `entornos` contiene opciones no sensibles y sus ejemplos versionados.
- `secretos` contiene únicamente instrucciones; los valores reales se generan localmente y permanecen ignorados por Git y Docker.
