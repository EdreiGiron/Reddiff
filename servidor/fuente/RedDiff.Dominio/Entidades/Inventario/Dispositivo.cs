using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Cumplimiento;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Inventario;

public sealed class Dispositivo
{
    private Dispositivo()
    {
    }

    public Dispositivo(
        string nombre,
        string host,
        string tipo,
        string? modelo,
        ProtocoloConexion protocolo,
        int puerto,
        FuenteEvento? fuenteEventos)
    {
        Nombre = ValidacionDominio.TextoObligatorio(nombre, nameof(nombre), 120);
        Host = ValidacionDominio.TextoObligatorio(host, nameof(host), 255);
        Tipo = ValidacionDominio.TextoObligatorio(tipo, nameof(tipo), 100);
        Modelo = ValidacionDominio.TextoOpcional(modelo, nameof(modelo), 120);
        Protocolo = protocolo;
        CambiarPuerto(puerto);
        FuenteEventos = fuenteEventos;
        Estado = EstadoDispositivo.NoAutorizado;
    }

    public long Id { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public string Host { get; private set; } = string.Empty;

    public string Tipo { get; private set; } = string.Empty;

    public string? Modelo { get; private set; }

    public ProtocoloConexion Protocolo { get; private set; }

    public int Puerto { get; private set; }

    public FuenteEvento? FuenteEventos { get; private set; }

    public EstadoDispositivo Estado { get; private set; }

    public ICollection<EventoCambio> EventosCambio { get; } = [];

    public ICollection<Captura> Capturas { get; } = [];

    public ICollection<VersionConfiguracion> VersionesConfiguracion { get; } = [];

    public ICollection<Baseline> Baselines { get; } = [];

    public void CambiarPuerto(int puerto)
    {
        if (puerto is <= 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(puerto),
                "El puerto debe estar entre 1 y 65535.");
        }

        Puerto = puerto;
    }

    public void Autorizar()
    {
        Estado = EstadoDispositivo.Autorizado;
    }

    public void RevocarAutorizacion()
    {
        Estado = EstadoDispositivo.NoAutorizado;
    }

    public void Desactivar()
    {
        Estado = EstadoDispositivo.Inactivo;
    }
}
