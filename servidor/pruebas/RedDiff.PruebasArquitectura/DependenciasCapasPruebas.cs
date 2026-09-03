using System.Reflection;
using RedDiff.Aplicacion;
using RedDiff.Dominio;
using RedDiff.Infraestructura;

namespace RedDiff.PruebasArquitectura;

public sealed class DependenciasCapasPruebas
{
    [Fact]
    public void Dominio_NoDebeDependerDeOtrasCapas()
    {
        AssertNoDependeDeCapasNoPermitidas(
            typeof(MarcadorEnsambladoDominio).Assembly);
    }

    [Fact]
    public void Aplicacion_SoloPuedeDependerDelDominio()
    {
        AssertNoDependeDeCapasNoPermitidas(
            typeof(MarcadorEnsambladoAplicacion).Assembly,
            "RedDiff.Dominio");
    }

    [Fact]
    public void Infraestructura_NoDebeDependerDePresentacionNiProcesadores()
    {
        AssertNoDependeDeCapasNoPermitidas(
            typeof(MarcadorEnsambladoInfraestructura).Assembly,
            "RedDiff.Aplicacion",
            "RedDiff.Dominio");
    }

    private static void AssertNoDependeDeCapasNoPermitidas(
        Assembly ensamblado,
        params string[] capasPermitidas)
    {
        HashSet<string> permitidas = [.. capasPermitidas];

        string[] noPermitidas = ensamblado
            .GetReferencedAssemblies()
            .Select(referencia => referencia.Name)
            .Where(nombre => nombre is not null && nombre.StartsWith("RedDiff.", StringComparison.Ordinal))
            .Select(nombre => nombre!)
            .Where(nombre => !permitidas.Contains(nombre))
            .ToArray();

        Assert.Empty(noPermitidas);
    }
}
