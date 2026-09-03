# Configuración por entorno

Los archivos `*.example` contienen valores no sensibles que pueden versionarse. El script de preparación copia la plantilla de desarrollo a `desarrollo.env`, archivo que permanece ignorado por Git.

Las contraseñas, tokens y llaves nunca se agregan en esta carpeta; pertenecen a `configuracion/secretos` o al almacén seguro de la plataforma de despliegue.
