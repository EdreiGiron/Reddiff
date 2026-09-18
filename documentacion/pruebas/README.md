# Estrategia de pruebas

La calidad se verificará en varios niveles:

- pruebas unitarias para reglas del dominio y casos de uso;
- pruebas de integración para PostgreSQL, API y adaptadores externos controlados;
- pruebas de arquitectura para impedir dependencias no permitidas;
- pruebas del cliente para componentes, servicios y flujos críticos;
- pruebas de extremo a extremo para los recorridos principales cuando exista el primer incremento funcional;
- revisiones de seguridad y dependencias en la integración continua.

Los adaptadores externos deben probarse primero mediante dobles controlados. Estos dobles viven únicamente en los proyectos de pruebas y no pueden registrarse en la aplicación ejecutable. Una prueba contra equipos reales requiere un laboratorio autorizado o un escenario aislado en GNS3.

El adaptador SSH conserva sus pruebas de identidad del host separadas de la red: calculan la huella SHA-256 con claves públicas efímeras y comprueban algoritmos compatibles. Las pruebas de API sustituyen el conector real antes de construir el servicio para garantizar que una ejecución automatizada nunca abra una conexión SSH.

Cada requisito funcional deberá relacionarse con al menos un criterio de aceptación y una evidencia de prueba.
