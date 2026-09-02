# Inicialización local de PostgreSQL

Scripts destinados exclusivamente a preparar una base de datos nueva en el entorno local de contenedores.

Los archivos ejecutables se numerarán para dejar explícito el orden, serán idempotentes cuando resulte posible y no contendrán datos productivos ni credenciales. Las modificaciones posteriores del esquema se realizarán mediante migraciones de `RedDiff.Infraestructura`.
