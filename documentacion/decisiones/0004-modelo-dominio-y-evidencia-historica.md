# ADR 0004: Modelo de dominio y conservación de evidencia histórica

- Estado: aceptada.
- Fecha: 2026-09-03.

## Contexto

El diseño académico define trece entidades para conservar dispositivos, capturas, versiones, comparaciones, verificaciones y auditoría. También exige integridad, trazabilidad, control de acceso y ausencia de modificaciones automáticas sobre los dispositivos.

## Decisión

- Representar cada entidad mediante una clase sellada del proyecto `RedDiff.Dominio`.
- Mantener setters no públicos y exponer métodos para los cambios de estado válidos.
- Utilizar enumeraciones del dominio y persistirlas como texto comprensible.
- Normalizar fechas a UTC y validar huellas SHA-256 antes de persistirlas.
- Conservar relaciones históricas con `DeleteBehavior.Restrict`, sin borrado en cascada.
- Configurar cada entidad en un archivo independiente de `RedDiff.Infraestructura`.
- Añadir los campos de seguridad y trazabilidad requeridos por el texto del documento aunque no aparezcan resumidos en su figura lógica.

## Consecuencias

El dominio puede probarse sin Entity Framework Core y las decisiones de PostgreSQL permanecen fuera de la lógica de negocio. Los casos de uso deberán aplicar las reglas que requieren consultar varias entidades, por ejemplo validar que las dos versiones de una comparación pertenezcan al mismo dispositivo.

La migración inicial se generará y revisará después de comprobar la compilación y el modelo de EF Core. No se aplicará automáticamente durante el inicio de la API.
