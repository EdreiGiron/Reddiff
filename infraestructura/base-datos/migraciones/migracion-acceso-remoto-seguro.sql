START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM reddiff.__historial_migraciones WHERE "MigrationId" = '20260905061958_AgregarAccesoRemotoSeguro') THEN
    ALTER TABLE reddiff.dispositivo ADD acceso_configurado_en timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM reddiff.__historial_migraciones WHERE "MigrationId" = '20260905061958_AgregarAccesoRemotoSeguro') THEN
    ALTER TABLE reddiff.dispositivo ADD algoritmo_clave_host character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM reddiff.__historial_migraciones WHERE "MigrationId" = '20260905061958_AgregarAccesoRemotoSeguro') THEN
    ALTER TABLE reddiff.dispositivo ADD huella_clave_host character(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM reddiff.__historial_migraciones WHERE "MigrationId" = '20260905061958_AgregarAccesoRemotoSeguro') THEN
    ALTER TABLE reddiff.dispositivo ADD secreto_acceso_protegido text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM reddiff.__historial_migraciones WHERE "MigrationId" = '20260905061958_AgregarAccesoRemotoSeguro') THEN
    ALTER TABLE reddiff.dispositivo ADD usuario_acceso character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM reddiff.__historial_migraciones WHERE "MigrationId" = '20260905061958_AgregarAccesoRemotoSeguro') THEN
    ALTER TABLE reddiff.dispositivo ADD CONSTRAINT ck_dispositivo_acceso_remoto CHECK ((usuario_acceso IS NULL AND secreto_acceso_protegido IS NULL AND algoritmo_clave_host IS NULL AND huella_clave_host IS NULL AND acceso_configurado_en IS NULL) OR (usuario_acceso IS NOT NULL AND secreto_acceso_protegido IS NOT NULL AND algoritmo_clave_host IS NOT NULL AND huella_clave_host IS NOT NULL AND acceso_configurado_en IS NOT NULL));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM reddiff.__historial_migraciones WHERE "MigrationId" = '20260905061958_AgregarAccesoRemotoSeguro') THEN
    INSERT INTO reddiff.__historial_migraciones ("MigrationId", "ProductVersion")
    VALUES ('20260905061958_AgregarAccesoRemotoSeguro', '10.0.11');
    END IF;
END $EF$;
COMMIT;

