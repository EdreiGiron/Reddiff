# Documentación de seguridad

Aquí se mantendrán el modelo de amenazas, la matriz de riesgos, los controles aplicados y las revisiones de seguridad.

Como mínimo se analizarán autenticación, autorización por roles, manejo de secretos, acceso de solo lectura a dispositivos, protección de configuraciones, auditoría, validación de entradas, dependencias y seguridad de los contenedores.

La implementación vigente de identidad, sus controles y el contrato protegido se describen en [autenticacion-y-autorizacion.md](autenticacion-y-autorizacion.md).

Las reglas de validación, enmascaramiento y exposición del contenido histórico se describen en [capturas y versiones](../api/capturas-y-versiones.md) y en la [decisión sobre carga controlada](../decisiones/0006-carga-controlada-y-contenido-canonico.md).

La comparación opera únicamente sobre versiones ya saneadas, persiste evidencia de solo lectura y audita cantidades sin copiar contenido. Consulte [comparación diferencial](../api/comparaciones.md).

La verificación de cumplimiento opera sobre versiones preservadas, mantiene reglas y resultados como evidencia inmutable y registra en auditoría solo identificadores, cantidades y el resultado general. La creación y el cambio de estado de líneas base están reservados al administrador. Consulte [líneas base y verificaciones](../api/baselines-y-verificaciones.md).

El almacenamiento cifrado de secretos de equipos, la confianza explícita de la clave del host y la invalidación del acceso ante cambios se describen en [acceso remoto a dispositivos](acceso-remoto-dispositivos.md).
