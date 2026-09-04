using RedDiff.Dominio.Entidades.Cumplimiento;

namespace RedDiff.PruebasUnitarias.Dominio;

public sealed class BaselinePruebas
{
    [Fact]
    public void ParaDispositivo_AsignaUnicamenteElDispositivo()
    {
        Baseline baseline = Baseline.ParaDispositivo(10, "Referencia del router principal");

        Assert.Equal(10L, baseline.DispositivoId);
        Assert.Null(baseline.TipoDispositivo);
        Assert.True(baseline.Activa);
    }

    [Fact]
    public void ParaTipoDispositivo_AsignaUnicamenteElTipo()
    {
        Baseline baseline = Baseline.ParaTipoDispositivo("Router", "Referencia general");

        Assert.Null(baseline.DispositivoId);
        Assert.Equal("Router", baseline.TipoDispositivo);
    }

    [Fact]
    public void ParaDispositivo_RechazaIdentificadorInvalido()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Baseline.ParaDispositivo(0, "Referencia inválida"));
    }
}
