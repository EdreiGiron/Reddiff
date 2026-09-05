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

    public string? UsuarioAcceso { get; private set; }

    public string? SecretoAccesoProtegido { get; private set; }

    public string? AlgoritmoClaveHost { get; private set; }

    public string? HuellaClaveHost { get; private set; }

    public DateTimeOffset? AccesoConfiguradoEn { get; private set; }

    public bool AccesoRemotoConfigurado =>
        UsuarioAcceso is not null
        && SecretoAccesoProtegido is not null
        && AlgoritmoClaveHost is not null
        && HuellaClaveHost is not null
        && AccesoConfiguradoEn.HasValue;

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

        bool cambioPuntoConexion = !string.IsNullOrEmpty(Host)
            && (!string.Equals(Host, hostValidado, StringComparison.Ordinal)
                || Protocolo != protocolo
                || Puerto != puerto);

        Nombre = nombreValidado;
        Host = hostValidado;
        Tipo = tipoValidado;
        Modelo = modeloValidado;
        Protocolo = protocolo;
        Puerto = puerto;
        FuenteEventos = fuenteEventos;

        if (cambioPuntoConexion)
        {
            EliminarAccesoRemoto();
        }
    }

    public void CambiarPuerto(int puerto)
    {
        ValidarPuerto(puerto);

        if (Puerto != puerto)
        {
            EliminarAccesoRemoto();
        }

        Puerto = puerto;
    }

    public void ConfigurarAccesoRemoto(
        string usuarioAcceso,
        string secretoAccesoProtegido,
        string algoritmoClaveHost,
        string huellaClaveHost,
        DateTimeOffset configuradoEn)
    {
        if (Estado != EstadoDispositivo.Autorizado)
        {
            throw new InvalidOperationException(
                "El acceso remoto solo puede configurarse en un dispositivo autorizado.");
        }

        string usuarioValidado = ValidacionDominio.TextoObligatorio(
            usuarioAcceso,
            nameof(usuarioAcceso),
            120);
        string secretoValidado = ValidacionDominio.ContenidoObligatorio(
            secretoAccesoProtegido,
            nameof(secretoAccesoProtegido),
            4_000);
        string algoritmoValidado = ValidacionDominio.TextoObligatorio(
            algoritmoClaveHost,
            nameof(algoritmoClaveHost),
            100).ToLowerInvariant();

        if (algoritmoValidado.Any(static caracter =>
                !char.IsAsciiLetterOrDigit(caracter)
                && caracter is not '-' and not '_' and not '.' and not '@' and not '+'))
        {
            throw new ArgumentException(
                "El algoritmo de clave del host contiene caracteres no permitidos.",
                nameof(algoritmoClaveHost));
        }

        string huellaValidada = ValidacionDominio.HuellaSha256(
            huellaClaveHost,
            nameof(huellaClaveHost));
        DateTimeOffset fechaValidada = ValidacionDominio.FechaUtc(
            configuradoEn,
            nameof(configuradoEn));

        UsuarioAcceso = usuarioValidado;
        SecretoAccesoProtegido = secretoValidado;
        AlgoritmoClaveHost = algoritmoValidado;
        HuellaClaveHost = huellaValidada;
        AccesoConfiguradoEn = fechaValidada;
    }

    public void EliminarAccesoRemoto()
    {
        UsuarioAcceso = null;
        SecretoAccesoProtegido = null;
        AlgoritmoClaveHost = null;
        HuellaClaveHost = null;
        AccesoConfiguradoEn = null;
    }

    public void Autorizar()
    {
        Estado = EstadoDispositivo.Autorizado;
    }

    public void RevocarAutorizacion()
    {
        CambiarEstado(EstadoDispositivo.NoAutorizado);
    }

    public void Desactivar()
    {
        CambiarEstado(EstadoDispositivo.Inactivo);
    }

    public void CambiarEstado(EstadoDispositivo estado)
    {
        if (!Enum.IsDefined(estado))
        {
            throw new ArgumentException("El estado indicado no es válido.", nameof(estado));
        }

        Estado = estado;

        if (estado != EstadoDispositivo.Autorizado)
        {
            EliminarAccesoRemoto();
        }
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
