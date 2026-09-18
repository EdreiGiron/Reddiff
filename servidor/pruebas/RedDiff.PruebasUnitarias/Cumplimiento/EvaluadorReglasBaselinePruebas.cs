using RedDiff.Aplicacion.Cumplimiento;
using RedDiff.Dominio.Entidades.Cumplimiento;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.PruebasUnitarias.Cumplimiento;

public sealed class EvaluadorReglasBaselinePruebas
{
    private readonly EvaluadorReglasBaseline evaluador = new();

    [Fact]
    public void Contiene_MarcaCumplidaCuandoEncuentraElContenidoExacto()
    {
        ReglaBaseline regla = CrearRegla(TipoCriterioRegla.Contiene, "no ip http server");

        ResultadoEvaluacionRegla resultado = evaluador.Evaluar(
            regla,
            "hostname Router\nno ip http server\n");

        Assert.Equal("Cumplida", resultado.Estado);
    }

    [Fact]
    public void Contiene_DistingueMayusculasYMinusculas()
    {
        ReglaBaseline regla = CrearRegla(TipoCriterioRegla.Contiene, "hostname Router");

        ResultadoEvaluacionRegla resultado = evaluador.Evaluar(
            regla,
            "hostname router\n");

        Assert.Equal("Incumplida", resultado.Estado);
    }

    [Fact]
    public void NoContiene_MarcaIncumplidaCuandoEncuentraContenidoProhibido()
    {
        ReglaBaseline regla = CrearRegla(TipoCriterioRegla.NoContiene, "transport input telnet");

        ResultadoEvaluacionRegla resultado = evaluador.Evaluar(
            regla,
            "line vty 0 4\n transport input telnet\n");

        Assert.Equal("Incumplida", resultado.Estado);
    }

    [Fact]
    public void ExpresionRegular_EvaluaEnModoMultilinea()
    {
        ReglaBaseline regla = CrearRegla(
            TipoCriterioRegla.CoincideExpresionRegular,
            "^interface FastEthernet0/0$");

        ResultadoEvaluacionRegla resultado = evaluador.Evaluar(
            regla,
            "hostname Router\ninterface FastEthernet0/0\n ip address 192.0.2.1 255.255.255.0\n");

        Assert.Equal("Cumplida", resultado.Estado);
    }

    [Fact]
    public void ExpresionRegularInvalida_DevuelveNoEvaluable()
    {
        ReglaBaseline regla = CrearRegla(
            TipoCriterioRegla.CoincideExpresionRegular,
            "[expresion-incompleta");

        ResultadoEvaluacionRegla resultado = evaluador.Evaluar(regla, "hostname Router\n");

        Assert.Equal("NoEvaluable", resultado.Estado);
        Assert.Contains("no es válida", resultado.Evidencia);
    }

    private static ReglaBaseline CrearRegla(TipoCriterioRegla criterio, string esperado)
    {
        return new ReglaBaseline(1, criterio, esperado, true);
    }
}
