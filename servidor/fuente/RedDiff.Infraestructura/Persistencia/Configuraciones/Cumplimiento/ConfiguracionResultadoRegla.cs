using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Cumplimiento;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Cumplimiento;

internal sealed class ConfiguracionResultadoRegla
    : IEntityTypeConfiguration<ResultadoRegla>
{
    public void Configure(EntityTypeBuilder<ResultadoRegla> constructor)
    {
        constructor.ToTable("resultado_regla");
        constructor.HasKey(resultado => resultado.Id).HasName("pk_resultado_regla");

        constructor.Property(resultado => resultado.Id)
            .HasColumnName("resultado_id")
            .ValueGeneratedOnAdd();

        constructor.Property(resultado => resultado.VerificacionId)
            .HasColumnName("verificacion_id")
            .IsRequired();

        constructor.Property(resultado => resultado.ReglaId)
            .HasColumnName("regla_id")
            .IsRequired();

        constructor.Property(resultado => resultado.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(resultado => resultado.Evidencia)
            .HasColumnName("evidencia")
            .HasColumnType("text");

        constructor.HasIndex(resultado => new
            {
                resultado.VerificacionId,
                resultado.ReglaId
            })
            .IsUnique()
            .HasDatabaseName("ux_resultado_regla_verificacion_regla");

        constructor.HasIndex(resultado => resultado.ReglaId)
            .HasDatabaseName("ix_resultado_regla_regla_baseline");

        constructor.HasOne(resultado => resultado.Verificacion)
            .WithMany(verificacion => verificacion.Resultados)
            .HasForeignKey(resultado => resultado.VerificacionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_resultado_regla_verificacion");

        constructor.HasOne(resultado => resultado.Regla)
            .WithMany(regla => regla.Resultados)
            .HasForeignKey(resultado => resultado.ReglaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_resultado_regla_regla_baseline");
    }
}
