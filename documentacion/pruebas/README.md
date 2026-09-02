# Estrategia de pruebas

La calidad se verificará en varios niveles:

- pruebas unitarias para reglas del dominio y casos de uso;
- pruebas de integración para PostgreSQL, API y adaptadores externos controlados;
- pruebas de arquitectura para impedir dependencias no permitidas;
- pruebas del cliente para componentes, servicios y flujos críticos;
- pruebas de extremo a extremo para los recorridos principales cuando exista el primer incremento funcional;
- revisiones de seguridad y dependencias en la integración continua.

Cada requisito funcional deberá relacionarse con al menos un criterio de aceptación y una evidencia de prueba.
