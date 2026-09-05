using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Inventario;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Inventario;

internal sealed class ConfiguracionDispositivo : IEntityTypeConfiguration<Dispositivo>
{
    public void Configure(EntityTypeBuilder<Dispositivo> constructor)
    {
        constructor.ToTable(
            "dispositivo",
            tabla => tabla.HasCheckConstraint(
                "ck_dispositivo_acceso_remoto",
                "(usuario_acceso IS NULL AND secreto_acceso_protegido IS NULL "
                + "AND algoritmo_clave_host IS NULL AND huella_clave_host IS NULL "
                + "AND acceso_configurado_en IS NULL) OR "
                + "(usuario_acceso IS NOT NULL AND secreto_acceso_protegido IS NOT NULL "
                + "AND algoritmo_clave_host IS NOT NULL AND huella_clave_host IS NOT NULL "
                + "AND acceso_configurado_en IS NOT NULL)"));
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

        constructor.Property(dispositivo => dispositivo.UsuarioAcceso)
            .HasColumnName("usuario_acceso")
            .HasMaxLength(120);

        constructor.Property(dispositivo => dispositivo.SecretoAccesoProtegido)
            .HasColumnName("secreto_acceso_protegido")
            .HasColumnType("text");

        constructor.Property(dispositivo => dispositivo.AlgoritmoClaveHost)
            .HasColumnName("algoritmo_clave_host")
            .HasMaxLength(100);

        constructor.Property(dispositivo => dispositivo.HuellaClaveHost)
            .HasColumnName("huella_clave_host")
            .HasColumnType("character(64)");

        constructor.Property(dispositivo => dispositivo.AccesoConfiguradoEn)
            .HasColumnName("acceso_configurado_en")
            .HasColumnType("timestamp with time zone");

        constructor.Ignore(dispositivo => dispositivo.AccesoRemotoConfigurado);

        constructor.HasIndex(dispositivo => dispositivo.Nombre)
            .IsUnique()
            .HasDatabaseName("ux_dispositivo_nombre");

        constructor.HasIndex(dispositivo => new { dispositivo.Host, dispositivo.Puerto })
            .IsUnique()
            .HasDatabaseName("ux_dispositivo_host_puerto");
    }
}
