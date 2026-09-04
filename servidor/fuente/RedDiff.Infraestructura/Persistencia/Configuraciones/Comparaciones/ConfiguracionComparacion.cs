using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Comparaciones;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Comparaciones;

internal sealed class ConfiguracionComparacion : IEntityTypeConfiguration<Comparacion>
{
    public void Configure(EntityTypeBuilder<Comparacion> constructor)
    {
        constructor.ToTable(
            "comparacion",
            tabla => tabla.HasCheckConstraint(
                "ck_comparacion_versiones_diferentes",
                "version_origen_id <> version_destino_id"));

        constructor.HasKey(comparacion => comparacion.Id).HasName("pk_comparacion");

        constructor.Property(comparacion => comparacion.Id)
            .HasColumnName("comparacion_id")
            .ValueGeneratedOnAdd();

        constructor.Property(comparacion => comparacion.VersionOrigenId)
            .HasColumnName("version_origen_id")
            .IsRequired();

        constructor.Property(comparacion => comparacion.VersionDestinoId)
            .HasColumnName("version_destino_id")
            .IsRequired();

        constructor.Property(comparacion => comparacion.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        constructor.Property(comparacion => comparacion.Fecha)
            .HasColumnName("fecha")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        constructor.Property(comparacion => comparacion.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.HasIndex(comparacion => new
            {
                comparacion.VersionOrigenId,
                comparacion.VersionDestinoId,
                comparacion.Fecha
            })
            .HasDatabaseName("ix_comparacion_versiones_fecha");

        constructor.HasIndex(comparacion => new { comparacion.UsuarioId, comparacion.Fecha })
            .HasDatabaseName("ix_comparacion_usuario_fecha");

        constructor.HasIndex(comparacion => comparacion.VersionDestinoId)
            .HasDatabaseName("ix_comparacion_version_destino");

        constructor.HasOne(comparacion => comparacion.VersionOrigen)
            .WithMany(version => version.ComparacionesOrigen)
            .HasForeignKey(comparacion => comparacion.VersionOrigenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_comparacion_version_origen");

        constructor.HasOne(comparacion => comparacion.VersionDestino)
            .WithMany(version => version.ComparacionesDestino)
            .HasForeignKey(comparacion => comparacion.VersionDestinoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_comparacion_version_destino");

        constructor.HasOne(comparacion => comparacion.Usuario)
            .WithMany(usuario => usuario.Comparaciones)
            .HasForeignKey(comparacion => comparacion.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_comparacion_usuario");
    }
}
