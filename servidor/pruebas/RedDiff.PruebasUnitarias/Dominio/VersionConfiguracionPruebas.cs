using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.PruebasUnitarias.Dominio;

public sealed class VersionConfiguracionPruebas
{
    private static readonly DateTimeOffset Fecha = new(
        2026,
        9,
        3,
        12,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public void Crear_NormalizaHuellaSha256AMinusculas()
    {
        VersionConfiguracion version = CrearVersion(new string('A', 64));

        Assert.Equal(new string('a', 64), version.Hash);
        Assert.Equal(EstadoVersion.Activa, version.Estado);
        Assert.False(version.Estable);
    }

    [Fact]
    public void Crear_RechazaHuellaQueNoSeaSha256Hexadecimal()
    {
        Assert.Throws<ArgumentException>(() => CrearVersion("huella-invalida"));
    }

    [Fact]
    public void MarcarComoEstable_ConservaUsuarioYFecha()
    {
        VersionConfiguracion version = CrearVersion(new string('a', 64));

        version.MarcarComoEstable(25, Fecha);

        Assert.True(version.Estable);
        Assert.Equal(25L, version.ValidadaPorUsuarioId);
        Assert.Equal(Fecha, version.ValidadaEn!.Value);
    }

    [Fact]
    public void Retirar_QuitaLaMarcaDeEstabilidad()
    {
        VersionConfiguracion version = CrearVersion(new string('a', 64));
        version.MarcarComoEstable(25, Fecha);

        version.Retirar();

        Assert.Equal(EstadoVersion.Retirada, version.Estado);
        Assert.False(version.Estable);
        Assert.Null(version.ValidadaPorUsuarioId);
        Assert.Null(version.ValidadaEn);
    }

    private static VersionConfiguracion CrearVersion(string hash)
    {
        return new VersionConfiguracion(
            10,
            20,
            1,
            OrigenVersion.CapturaSsh,
            "hostname RTR-PRINCIPAL",
            hash,
            Fecha);
    }
}
