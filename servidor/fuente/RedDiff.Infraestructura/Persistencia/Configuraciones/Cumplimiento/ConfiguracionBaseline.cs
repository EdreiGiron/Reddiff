using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Cumplimiento;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Cumplimiento;

internal sealed class ConfiguracionBaseline : IEntityTypeConfiguration<Baseline>
{
    public void Configure(EntityTypeBuilder<Baseline> constructor)
    {
        constructor.ToTable(
            "baseline",
            tabla => tabla.HasCheckConstraint(
                "ck_baseline_destino",
                "(dispositivo_id IS NOT NULL AND tipo_dispositivo IS NULL) "
                + "OR (dispositivo_id IS NULL AND tipo_dispositivo IS NOT NULL)"));

        constructor.HasKey(baseline => baseline.Id).HasName("pk_baseline");

        constructor.Property(baseline => baseline.Id)
            .HasColumnName("baseline_id")
            .ValueGeneratedOnAdd();

        constructor.Property(baseline => baseline.DispositivoId)
            .HasColumnName("dispositivo_id");

        constructor.Property(baseline => baseline.TipoDispositivo)
            .HasColumnName("tipo_dispositivo")
            .HasMaxLength(100);

        constructor.Property(baseline => baseline.VersionReferenciaId)
            .HasColumnName("version_id");

        constructor.Property(baseline => baseline.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        constructor.Property(baseline => baseline.Activa)
            .HasColumnName("activa")
            .IsRequired();

        constructor.HasIndex(baseline => new { baseline.DispositivoId, baseline.Activa })
            .HasDatabaseName("ix_baseline_dispositivo_activa");

        constructor.HasIndex(baseline => new { baseline.TipoDispositivo, baseline.Activa })
            .HasDatabaseName("ix_baseline_tipo_activa");

        constructor.HasIndex(baseline => baseline.VersionReferenciaId)
            .HasDatabaseName("ix_baseline_version_config");

        constructor.HasOne(baseline => baseline.Dispositivo)
            .WithMany(dispositivo => dispositivo.Baselines)
            .HasForeignKey(baseline => baseline.DispositivoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_baseline_dispositivo");

        constructor.HasOne(baseline => baseline.VersionReferencia)
            .WithMany(version => version.BaselinesReferencia)
            .HasForeignKey(baseline => baseline.VersionReferenciaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_baseline_version_config");
    }
}
