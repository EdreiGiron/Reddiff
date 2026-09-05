using System.Net;
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
        ActualizarDatos(nombre, host, tipo, modelo, protocolo, puerto, fuenteEventos);
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

    public void ActualizarDatos(
        string nombre,
        string host,
        string tipo,
        string? modelo,
        ProtocoloConexion protocolo,
        int puerto,
        FuenteEvento? fuenteEventos)
    {
        if (!Enum.IsDefined(protocolo))
        {
            throw new ArgumentException("El protocolo indicado no es válido.", nameof(protocolo));
        }

        if (fuenteEventos.HasValue && !Enum.IsDefined(fuenteEventos.Value))
        {
            throw new ArgumentException(
                "La fuente de eventos indicada no es válida.",
                nameof(fuenteEventos));
        }

        ValidarPuerto(puerto);
        string nombreValidado = ValidacionDominio.TextoObligatorio(
            nombre,
            nameof(nombre),
            120);
        string hostValidado = NormalizarHost(host);
        string tipoValidado = ValidacionDominio.TextoObligatorio(tipo, nameof(tipo), 100);
        string? modeloValidado = ValidacionDominio.TextoOpcional(
            modelo,
            nameof(modelo),
            120);

        Nombre = nombreValidado;
        Host = hostValidado;
        Tipo = tipoValidado;
        Modelo = modeloValidado;
        Protocolo = protocolo;
        Puerto = puerto;
        FuenteEventos = fuenteEventos;
    }

    public void CambiarPuerto(int puerto)
    {
        ValidarPuerto(puerto);
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

    public void CambiarEstado(EstadoDispositivo estado)
    {
        if (!Enum.IsDefined(estado))
        {
            throw new ArgumentException("El estado indicado no es válido.", nameof(estado));
        }

        Estado = estado;
    }

    public static string NormalizarHost(string host)
    {
        string valor = ValidacionDominio.TextoObligatorio(host, nameof(host), 255)
            .ToLowerInvariant();

        if (IPAddress.TryParse(valor, out IPAddress? direccion))
        {
            return direccion.ToString();
        }

        if (Uri.CheckHostName(valor) != UriHostNameType.Dns)
        {
            throw new ArgumentException(
                "El host debe ser un nombre DNS o una dirección IP válida, sin protocolo ni ruta.",
                nameof(host));
        }

        return valor;
    }

    private static void ValidarPuerto(int puerto)
    {
        if (puerto is <= 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(puerto),
                "El puerto debe estar entre 1 y 65535.");
        }
    }
}
