using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Identidad;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Identidad;

internal sealed class ConfiguracionRol : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> constructor)
    {
        constructor.ToTable("rol");
        constructor.HasKey(rol => rol.Id).HasName("pk_rol");

        constructor.Property(rol => rol.Id)
            .HasColumnName("rol_id")
            .ValueGeneratedOnAdd();

        constructor.Property(rol => rol.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(80)
            .IsRequired();

        constructor.Property(rol => rol.Estado)
            .HasColumnName("estado")
            .IsRequired();

        constructor.HasIndex(rol => rol.Nombre)
            .IsUnique()
            .HasDatabaseName("ux_rol_nombre");
    }
}
