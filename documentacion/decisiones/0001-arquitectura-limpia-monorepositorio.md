# Decisión 0001: arquitectura limpia en monorepositorio

- Estado: aceptada.
- Fecha: 2026-09-02.

## Contexto

El código anterior presentaba responsabilidades mezcladas, dependencias difíciles de seguir y alto riesgo de afectar funciones no relacionadas durante una modificación. El proyecto también necesita coordinar una aplicación Angular, una API, un procesador asíncrono, PostgreSQL, contenedores y documentación académica y técnica.

## Decisión

Se conservarán todos los componentes en un monorepositorio. El servidor se dividirá en Dominio, Aplicación, Infraestructura, API y Procesador, con dependencias dirigidas hacia el núcleo. El cliente se organizará por módulos funcionales y mantendrá separados los elementos transversales y reutilizables.

Las fronteras se verificarán mediante pruebas de arquitectura, pruebas unitarias y pruebas de integración.

## Consecuencias

### Positivas

- Responsabilidades y dependencias más fáciles de comprender.
- Cambios funcionales con menor acoplamiento.
- Posibilidad de probar reglas sin base de datos ni red.
- Versión común para cliente, servidor, infraestructura y documentación.

### Costos

- Mayor cantidad inicial de proyectos y carpetas.
- Necesidad de mantener contratos claros entre capas.
- Disciplina para evitar que las reglas migren a controladores, componentes o repositorios.
