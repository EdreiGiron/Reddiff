using RedDiff.Aplicacion.Comparaciones;

namespace RedDiff.PruebasUnitarias.Comparaciones;

public sealed class ProcesadorComparacionConfiguracionesPruebas
{
    private readonly ProcesadorComparacionConfiguraciones procesador = new();

    [Fact]
    public void Comparar_ClasificaLineasModificadasYAgregadas()
    {
        const string anterior = "hostname r1\ninterface Gi0/1\n shutdown";
        const string nueva =
            "hostname r1\ninterface Gi0/1\n no shutdown\n description enlace LAN";

        IReadOnlyList<DiferenciaCalculada> diferencias = procesador.Comparar(anterior, nueva);

        Assert.Collection(
            diferencias,
            modificada =>
            {
                Assert.Equal(3, modificada.Linea);
                Assert.Equal(" shutdown", modificada.TextoAnterior);
                Assert.Equal(" no shutdown", modificada.TextoNuevo);
            },
            agregada =>
            {
                Assert.Equal(4, agregada.Linea);
                Assert.Null(agregada.TextoAnterior);
                Assert.Equal(" description enlace LAN", agregada.TextoNuevo);
            });
    }

    [Fact]
    public void Comparar_ClasificaUnaLineaEliminada()
    {
        const string anterior = "hostname r1\nlogging host 10.0.0.5\nend";
        const string nueva = "hostname r1\nend";

        DiferenciaCalculada diferencia = Assert.Single(procesador.Comparar(anterior, nueva));

        Assert.Equal(2, diferencia.Linea);
        Assert.Equal("logging host 10.0.0.5", diferencia.TextoAnterior);
        Assert.Null(diferencia.TextoNuevo);
    }

    [Fact]
    public void Comparar_ConservaCambiosDeEspaciosComoDiferencia()
    {
        const string anterior = " description enlace";
        const string nueva = "  description enlace";

        DiferenciaCalculada diferencia = Assert.Single(procesador.Comparar(anterior, nueva));

        Assert.Equal(anterior, diferencia.TextoAnterior);
        Assert.Equal(nueva, diferencia.TextoNuevo);
    }

    [Fact]
    public void Comparar_NoGeneraDiferenciasParaContenidoIdentico()
    {
        const string contenido = "hostname r1\ninterface Gi0/1\n no shutdown";

        Assert.Empty(procesador.Comparar(contenido, contenido));
    }
}
