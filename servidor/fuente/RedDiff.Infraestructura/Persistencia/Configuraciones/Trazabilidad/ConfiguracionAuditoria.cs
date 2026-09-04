using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Trazabilidad;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Trazabilidad;

internal sealed class ConfiguracionAuditoria : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> constructor)
    {
        constructor.ToTable("auditoria");
        constructor.HasKey(auditoria => auditoria.Id).HasName("pk_auditoria");

        constructor.Property(auditoria => auditoria.Id)
            .HasColumnName("auditoria_id")
            .ValueGeneratedOnAdd();

        constructor.Property(auditoria => auditoria.UsuarioId)
            .HasColumnName("usuario_id");

        constructor.Property(auditoria => auditoria.Accion)
            .HasColumnName("accion")
            .HasMaxLength(120)
            .IsRequired();

        constructor.Property(auditoria => auditoria.Entidad)
            .HasColumnName("entidad")
            .HasMaxLength(120)
            .IsRequired();

        constructor.Property(auditoria => auditoria.EntidadId)
            .HasColumnName("entidad_id");

        constructor.Property(auditoria => auditoria.Fecha)
            .HasColumnName("fecha")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        constructor.Property(auditoria => auditoria.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(auditoria => auditoria.Detalle)
            .HasColumnName("detalle")
            .HasColumnType("text");

        constructor.HasIndex(auditoria => auditoria.Fecha)
            .HasDatabaseName("ix_auditoria_fecha");

        constructor.HasIndex(auditoria => new { auditoria.UsuarioId, auditoria.Fecha })
            .HasDatabaseName("ix_auditoria_usuario_fecha");

        constructor.HasIndex(auditoria => new
            {
                auditoria.Entidad,
                auditoria.EntidadId,
                auditoria.Fecha
            })
            .HasDatabaseName("ix_auditoria_entidad_fecha");

        constructor.HasOne(auditoria => auditoria.Usuario)
            .WithMany(usuario => usuario.Auditorias)
            .HasForeignKey(auditoria => auditoria.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_auditoria_usuario");
    }
}
