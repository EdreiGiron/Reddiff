using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Cumplimiento;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Cumplimiento;

internal sealed class ConfiguracionVerificacion : IEntityTypeConfiguration<Verificacion>
{
    public void Configure(EntityTypeBuilder<Verificacion> constructor)
    {
        constructor.ToTable("verificacion");
        constructor.HasKey(verificacion => verificacion.Id).HasName("pk_verificacion");

        constructor.Property(verificacion => verificacion.Id)
            .HasColumnName("verificacion_id")
            .ValueGeneratedOnAdd();

        constructor.Property(verificacion => verificacion.BaselineId)
            .HasColumnName("baseline_id")
            .IsRequired();

        constructor.Property(verificacion => verificacion.VersionId)
            .HasColumnName("version_id")
            .IsRequired();

        constructor.Property(verificacion => verificacion.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        constructor.Property(verificacion => verificacion.Fecha)
            .HasColumnName("fecha")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        constructor.Property(verificacion => verificacion.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.HasIndex(verificacion => new
            {
                verificacion.BaselineId,
                verificacion.VersionId,
                verificacion.Fecha
            })
            .HasDatabaseName("ix_verificacion_baseline_version_fecha");

        constructor.HasIndex(verificacion => new
            {
                verificacion.UsuarioId,
                verificacion.Fecha
            })
            .HasDatabaseName("ix_verificacion_usuario_fecha");

        constructor.HasIndex(verificacion => verificacion.VersionId)
            .HasDatabaseName("ix_verificacion_version_config");

        constructor.HasOne(verificacion => verificacion.Baseline)
            .WithMany(baseline => baseline.Verificaciones)
            .HasForeignKey(verificacion => verificacion.BaselineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_verificacion_baseline");

        constructor.HasOne(verificacion => verificacion.Version)
            .WithMany(version => version.Verificaciones)
            .HasForeignKey(verificacion => verificacion.VersionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_verificacion_version_config");

        constructor.HasOne(verificacion => verificacion.Usuario)
            .WithMany(usuario => usuario.Verificaciones)
            .HasForeignKey(verificacion => verificacion.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_verificacion_usuario");
    }
}
