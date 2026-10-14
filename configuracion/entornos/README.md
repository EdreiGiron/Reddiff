# Configuración por entorno

Los archivos `*.example` contienen valores no sensibles que pueden versionarse. Los scripts de preparación copian las plantillas a `desarrollo.env` o `produccion.env`; ambos archivos reales permanecen ignorados por Git.

Las contraseñas, tokens y llaves nunca se agregan en esta carpeta; pertenecen a `configuracion/secretos` o al almacén seguro de la plataforma de despliegue.
