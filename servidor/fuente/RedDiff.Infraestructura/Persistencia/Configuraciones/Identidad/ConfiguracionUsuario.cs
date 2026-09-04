using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Identidad;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Identidad;

internal sealed class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> constructor)
    {
        constructor.ToTable("usuario");
        constructor.HasKey(usuario => usuario.Id).HasName("pk_usuario");

        constructor.Property(usuario => usuario.Id)
            .HasColumnName("usuario_id")
            .ValueGeneratedOnAdd();

        constructor.Property(usuario => usuario.RolId)
            .HasColumnName("rol_id")
            .IsRequired();

        constructor.Property(usuario => usuario.NombreUsuario)
            .HasColumnName("usuario")
            .HasMaxLength(100)
            .IsRequired();

        constructor.Property(usuario => usuario.ContrasenaHash)
            .HasColumnName("contrasena_hash")
            .HasMaxLength(512)
            .IsRequired();

        constructor.Property(usuario => usuario.Estado)
            .HasColumnName("estado")
            .IsRequired();

        constructor.HasIndex(usuario => usuario.NombreUsuario)
            .IsUnique()
            .HasDatabaseName("ux_usuario_nombre");

        constructor.HasIndex(usuario => usuario.RolId)
            .HasDatabaseName("ix_usuario_rol");

        constructor.HasOne(usuario => usuario.Rol)
            .WithMany(rol => rol.Usuarios)
            .HasForeignKey(usuario => usuario.RolId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_usuario_rol");
    }
}
