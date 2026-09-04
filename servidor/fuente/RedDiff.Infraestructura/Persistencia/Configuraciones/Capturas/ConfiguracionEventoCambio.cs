using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Capturas;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Capturas;

internal sealed class ConfiguracionEventoCambio : IEntityTypeConfiguration<EventoCambio>
{
    public void Configure(EntityTypeBuilder<EventoCambio> constructor)
    {
        constructor.ToTable("evento_cambio");
        constructor.HasKey(evento => evento.Id).HasName("pk_evento_cambio");

        constructor.Property(evento => evento.Id)
            .HasColumnName("evento_id")
            .ValueGeneratedOnAdd();

        constructor.Property(evento => evento.DispositivoId)
            .HasColumnName("dispositivo_id")
            .IsRequired();

        constructor.Property(evento => evento.Fuente)
            .HasColumnName("fuente")
            .HasMaxLength(255)
            .IsRequired();

        constructor.Property(evento => evento.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(evento => evento.Fecha)
            .HasColumnName("fecha")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        constructor.Property(evento => evento.Huella)
            .HasColumnName("huella")
            .HasColumnType("character(64)")
            .IsRequired();

        constructor.Property(evento => evento.ContenidoEvento)
            .HasColumnName("contenido_evento")
            .HasColumnType("text")
            .IsRequired();

        constructor.Property(evento => evento.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.HasIndex(evento => new { evento.DispositivoId, evento.Huella })
            .IsUnique()
            .HasDatabaseName("ux_evento_cambio_dispositivo_huella");

        constructor.HasIndex(evento => new { evento.Estado, evento.Fecha })
            .HasDatabaseName("ix_evento_cambio_estado_fecha");

        constructor.HasOne(evento => evento.Dispositivo)
            .WithMany(dispositivo => dispositivo.EventosCambio)
            .HasForeignKey(evento => evento.DispositivoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_evento_cambio_dispositivo");
    }
}
