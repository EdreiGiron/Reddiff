using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Inventario;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Inventario;

internal sealed class ConfiguracionDispositivo : IEntityTypeConfiguration<Dispositivo>
{
    public void Configure(EntityTypeBuilder<Dispositivo> constructor)
    {
        constructor.ToTable("dispositivo");
        constructor.HasKey(dispositivo => dispositivo.Id).HasName("pk_dispositivo");

        constructor.Property(dispositivo => dispositivo.Id)
            .HasColumnName("dispositivo_id")
            .ValueGeneratedOnAdd();

        constructor.Property(dispositivo => dispositivo.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(120)
            .IsRequired();

        constructor.Property(dispositivo => dispositivo.Host)
            .HasColumnName("host")
            .HasMaxLength(255)
            .IsRequired();

        constructor.Property(dispositivo => dispositivo.Tipo)
            .HasColumnName("tipo")
            .HasMaxLength(100)
            .IsRequired();

        constructor.Property(dispositivo => dispositivo.Modelo)
            .HasColumnName("modelo")
            .HasMaxLength(120);

        constructor.Property(dispositivo => dispositivo.Protocolo)
            .HasColumnName("protocolo")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        constructor.Property(dispositivo => dispositivo.Puerto)
            .HasColumnName("puerto")
            .IsRequired();

        constructor.Property(dispositivo => dispositivo.FuenteEventos)
            .HasColumnName("fuente_eventos")
            .HasConversion<string>()
            .HasMaxLength(30);

        constructor.Property(dispositivo => dispositivo.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.HasIndex(dispositivo => dispositivo.Nombre)
            .IsUnique()
            .HasDatabaseName("ux_dispositivo_nombre");

        constructor.HasIndex(dispositivo => new { dispositivo.Host, dispositivo.Puerto })
            .IsUnique()
            .HasDatabaseName("ux_dispositivo_host_puerto");
    }
}
