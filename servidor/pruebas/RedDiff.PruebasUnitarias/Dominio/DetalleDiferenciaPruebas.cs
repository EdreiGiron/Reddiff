using RedDiff.Dominio.Entidades.Comparaciones;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.PruebasUnitarias.Dominio;

public sealed class DetalleDiferenciaPruebas
{
    [Fact]
    public void DiferenciaAgregada_RequiereTextoNuevo()
    {
        Assert.Throws<ArgumentException>(() =>
            new DetalleDiferencia(10, 1, TipoDiferencia.Agregada, null, null));
    }

    [Fact]
    public void DiferenciaModificada_ConservaAmbosTextos()
    {
        DetalleDiferencia diferencia = new(
            10,
            1,
            TipoDiferencia.Modificada,
            "shutdown",
            "no shutdown");

        Assert.Equal("shutdown", diferencia.TextoAnterior);
        Assert.Equal("no shutdown", diferencia.TextoNuevo);
    }
}
