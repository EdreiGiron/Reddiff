using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedDiff.Dominio.Entidades.Cumplimiento;

namespace RedDiff.Infraestructura.Persistencia.Configuraciones.Cumplimiento;

internal sealed class ConfiguracionReglaBaseline : IEntityTypeConfiguration<ReglaBaseline>
{
    public void Configure(EntityTypeBuilder<ReglaBaseline> constructor)
    {
        constructor.ToTable("regla_baseline");
        constructor.HasKey(regla => regla.Id).HasName("pk_regla_baseline");

        constructor.Property(regla => regla.Id)
            .HasColumnName("regla_id")
            .ValueGeneratedOnAdd();

        constructor.Property(regla => regla.BaselineId)
            .HasColumnName("baseline_id")
            .IsRequired();

        constructor.Property(regla => regla.Criterio)
            .HasColumnName("criterio")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        constructor.Property(regla => regla.Esperado)
            .HasColumnName("esperado")
            .HasColumnType("text")
            .IsRequired();

        constructor.Property(regla => regla.Obligatoria)
            .HasColumnName("obligatorio")
            .IsRequired();

        constructor.HasIndex(regla => regla.BaselineId)
            .HasDatabaseName("ix_regla_baseline_baseline");

        constructor.HasOne(regla => regla.Baseline)
            .WithMany(baseline => baseline.Reglas)
            .HasForeignKey(regla => regla.BaselineId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_regla_baseline_baseline");
    }
}
