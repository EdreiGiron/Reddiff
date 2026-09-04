using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.PruebasUnitarias.Dominio;

public sealed class CapturaPruebas
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
    public void CrearBajoDemanda_ConservaUsuarioSinEvento()
    {
        Captura captura = Captura.CrearBajoDemanda(10, 20, MedioCaptura.Ssh, Fecha);

        Assert.Equal(DisparadorCaptura.BajoDemanda, captura.Disparador);
        Assert.Equal(20L, captura.UsuarioSolicitanteId);
        Assert.Null(captura.EventoCambioId);
        Assert.Equal(EstadoCaptura.Pendiente, captura.Estado);
    }

    [Fact]
    public void CrearPorEvento_ConservaEventoSinUsuario()
    {
        Captura captura = Captura.CrearPorEvento(10, 30, MedioCaptura.Netconf, Fecha);

        Assert.Equal(DisparadorCaptura.Evento, captura.Disparador);
        Assert.Equal(30L, captura.EventoCambioId);
        Assert.Null(captura.UsuarioSolicitanteId);
    }

    [Fact]
    public void CrearPorEvento_RechazaArchivoComoMedio()
    {
        Assert.Throws<ArgumentException>(() =>
            Captura.CrearPorEvento(10, 30, MedioCaptura.Archivo, Fecha));
    }

    [Fact]
    public void Completar_RequiereQueLaCapturaEsteEnProceso()
    {
        Captura captura = Captura.CrearBajoDemanda(10, 20, MedioCaptura.Ssh, Fecha);

        Assert.Throws<InvalidOperationException>(captura.Completar);

        captura.Iniciar();
        captura.Completar();

        Assert.Equal(EstadoCaptura.Completada, captura.Estado);
    }
}
