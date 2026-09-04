using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Capturas;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Capturas;

internal sealed class ConfiguracionVersionConfiguracion
    : IEntityTypeConfiguration<VersionConfiguracion>
{
    public void Configure(EntityTypeBuilder<VersionConfiguracion> constructor)
    {
        constructor.ToTable(
            "version_config",
            tabla => tabla.HasCheckConstraint(
                "ck_version_config_estable",
                "(estable = FALSE AND validada_por_usuario_id IS NULL AND validada_en IS NULL) "
                + "OR (estable = TRUE AND validada_por_usuario_id IS NOT NULL AND validada_en IS NOT NULL)"));

        constructor.HasKey(version => version.Id).HasName("pk_version_config");

        constructor.Property(version => version.Id)
            .HasColumnName("version_id")
            .ValueGeneratedOnAdd();

        constructor.Property(version => version.DispositivoId)
            .HasColumnName("dispositivo_id")
            .IsRequired();

        constructor.Property(version => version.CapturaId)
            .HasColumnName("captura_id")
            .IsRequired();

        constructor.Property(version => version.Numero)
            .HasColumnName("numero")
            .IsRequired();

        constructor.Property(version => version.Origen)
            .HasColumnName("origen")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(version => version.Contenido)
            .HasColumnName("contenido")
            .HasColumnType("text")
            .IsRequired();

        constructor.Property(version => version.Hash)
            .HasColumnName("hash")
            .HasColumnType("character(64)")
            .IsRequired();

        constructor.Property(version => version.CapturadaEn)
            .HasColumnName("capturada_en")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        constructor.Property(version => version.Comentario)
            .HasColumnName("comentario")
            .HasMaxLength(500);

        constructor.Property(version => version.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(version => version.Estable)
            .HasColumnName("estable")
            .IsRequired();

        constructor.Property(version => version.ValidadaPorUsuarioId)
            .HasColumnName("validada_por_usuario_id");

        constructor.Property(version => version.ValidadaEn)
            .HasColumnName("validada_en")
            .HasColumnType("timestamp with time zone");

        constructor.HasIndex(version => version.CapturaId)
            .IsUnique()
            .HasDatabaseName("ux_version_config_captura");

        constructor.HasIndex(version => new { version.DispositivoId, version.Numero })
            .IsUnique()
            .HasDatabaseName("ux_version_config_dispositivo_numero");

        constructor.HasIndex(version => new { version.DispositivoId, version.CapturadaEn })
            .HasDatabaseName("ix_version_config_dispositivo_fecha");

        constructor.HasIndex(version => new
            {
                version.DispositivoId,
                version.Origen,
                version.CapturadaEn
            })
            .HasDatabaseName("ix_version_config_dispositivo_origen_fecha");

        constructor.HasIndex(version => new
            {
                version.DispositivoId,
                version.Estado,
                version.CapturadaEn
            })
            .HasDatabaseName("ix_version_config_dispositivo_estado_fecha");

        constructor.HasIndex(version => version.Hash)
            .HasDatabaseName("ix_version_config_hash");

        constructor.HasIndex(version => version.ValidadaPorUsuarioId)
            .HasDatabaseName("ix_version_config_usuario_validador");

        constructor.HasOne(version => version.Dispositivo)
            .WithMany(dispositivo => dispositivo.VersionesConfiguracion)
            .HasForeignKey(version => version.DispositivoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_version_config_dispositivo");

        constructor.HasOne(version => version.Captura)
            .WithOne(captura => captura.VersionConfiguracion)
            .HasForeignKey<VersionConfiguracion>(version => version.CapturaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_version_config_captura");

        constructor.HasOne(version => version.ValidadaPorUsuario)
            .WithMany(usuario => usuario.VersionesValidadas)
            .HasForeignKey(version => version.ValidadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_version_config_usuario_validador");
    }
}
