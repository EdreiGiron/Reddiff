using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Comparaciones;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Comparaciones;

internal sealed class ConfiguracionDetalleDiferencia
    : IEntityTypeConfiguration<DetalleDiferencia>
{
    public void Configure(EntityTypeBuilder<DetalleDiferencia> constructor)
    {
        constructor.ToTable("detalle_diferencia");
        constructor.HasKey(diferencia => diferencia.Id).HasName("pk_detalle_diferencia");

        constructor.Property(diferencia => diferencia.Id)
            .HasColumnName("diferencia_id")
            .ValueGeneratedOnAdd();

        constructor.Property(diferencia => diferencia.ComparacionId)
            .HasColumnName("comparacion_id")
            .IsRequired();

        constructor.Property(diferencia => diferencia.Linea)
            .HasColumnName("linea")
            .IsRequired();

        constructor.Property(diferencia => diferencia.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(diferencia => diferencia.TextoAnterior)
            .HasColumnName("texto_anterior")
            .HasColumnType("text");

        constructor.Property(diferencia => diferencia.TextoNuevo)
            .HasColumnName("texto_nuevo")
            .HasColumnType("text");

        constructor.HasIndex(diferencia => new
            {
                diferencia.ComparacionId,
                diferencia.Linea
            })
            .HasDatabaseName("ix_detalle_diferencia_comparacion_linea");

        constructor.HasOne(diferencia => diferencia.Comparacion)
            .WithMany(comparacion => comparacion.Diferencias)
            .HasForeignKey(diferencia => diferencia.ComparacionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_detalle_diferencia_comparacion");
    }
}
