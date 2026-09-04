using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Capturas;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Capturas;

internal sealed class ConfiguracionCaptura : IEntityTypeConfiguration<Captura>
{
    public void Configure(EntityTypeBuilder<Captura> constructor)
    {
        constructor.ToTable(
            "captura",
            tabla => tabla.HasCheckConstraint(
                "ck_captura_origen",
                "(disparador = 'BajoDemanda' AND usuario_id IS NOT NULL AND evento_id IS NULL) "
                + "OR (disparador = 'Evento' AND usuario_id IS NULL AND evento_id IS NOT NULL)"));

        constructor.HasKey(captura => captura.Id).HasName("pk_captura");

        constructor.Property(captura => captura.Id)
            .HasColumnName("captura_id")
            .ValueGeneratedOnAdd();

        constructor.Property(captura => captura.DispositivoId)
            .HasColumnName("dispositivo_id")
            .IsRequired();

        constructor.Property(captura => captura.EventoCambioId)
            .HasColumnName("evento_id");

        constructor.Property(captura => captura.UsuarioSolicitanteId)
            .HasColumnName("usuario_id");

        constructor.Property(captura => captura.Disparador)
            .HasColumnName("disparador")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(captura => captura.Medio)
            .HasColumnName("protocolo")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(captura => captura.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(captura => captura.Fecha)
            .HasColumnName("fecha")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        constructor.Property(captura => captura.Error)
            .HasColumnName("error")
            .HasMaxLength(2_000);

        constructor.HasIndex(captura => captura.EventoCambioId)
            .IsUnique()
            .HasDatabaseName("ux_captura_evento");

        constructor.HasIndex(captura => new { captura.DispositivoId, captura.Fecha })
            .HasDatabaseName("ix_captura_dispositivo_fecha");

        constructor.HasIndex(captura => new
            {
                captura.DispositivoId,
                captura.Disparador,
                captura.Fecha
            })
            .HasDatabaseName("ix_captura_dispositivo_disparador_fecha");

        constructor.HasIndex(captura => captura.Estado)
            .HasDatabaseName("ix_captura_estado");

        constructor.HasIndex(captura => captura.UsuarioSolicitanteId)
            .HasDatabaseName("ix_captura_usuario");

        constructor.HasOne(captura => captura.Dispositivo)
            .WithMany(dispositivo => dispositivo.Capturas)
            .HasForeignKey(captura => captura.DispositivoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_captura_dispositivo");

        constructor.HasOne(captura => captura.EventoCambio)
            .WithOne(evento => evento.Captura)
            .HasForeignKey<Captura>(captura => captura.EventoCambioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_captura_evento_cambio");

        constructor.HasOne(captura => captura.UsuarioSolicitante)
            .WithMany(usuario => usuario.CapturasSolicitadas)
            .HasForeignKey(captura => captura.UsuarioSolicitanteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_captura_usuario");
    }
}
