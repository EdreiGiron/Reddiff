using RedDiff.Dominio.Entidades.Comparaciones;
using RedDiff.Dominio.Entidades.Cumplimiento;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Capturas;

public sealed class VersionConfiguracion
{
    private VersionConfiguracion()
    {
    }

    public VersionConfiguracion(
        long dispositivoId,
        long capturaId,
        int numero,
        OrigenVersion origen,
        string contenido,
        string hash,
        DateTimeOffset capturadaEn,
        string? comentario = null)
    {
        DispositivoId = ValidacionDominio.Identificador(dispositivoId, nameof(dispositivoId));
        CapturaId = ValidacionDominio.Identificador(capturaId, nameof(capturaId));
        Numero = ValidacionDominio.EnteroPositivo(numero, nameof(numero));
        Origen = origen;
        Contenido = ValidacionDominio.ContenidoObligatorio(
            contenido,
            nameof(contenido),
            5_000_000);
        Hash = ValidacionDominio.HuellaSha256(hash, nameof(hash));
        CapturadaEn = ValidacionDominio.FechaUtc(capturadaEn, nameof(capturadaEn));
        Comentario = ValidacionDominio.TextoOpcional(comentario, nameof(comentario), 500);
        Estado = EstadoVersion.Activa;
    }

    public long Id { get; private set; }

    public long DispositivoId { get; private set; }

    public long CapturaId { get; private set; }

    public int Numero { get; private set; }

    public OrigenVersion Origen { get; private set; }

    public string Contenido { get; private set; } = string.Empty;

    public string Hash { get; private set; } = string.Empty;

    public DateTimeOffset CapturadaEn { get; private set; }

    public string? Comentario { get; private set; }

    public EstadoVersion Estado { get; private set; }

    public bool Estable { get; private set; }

    public long? ValidadaPorUsuarioId { get; private set; }

    public DateTimeOffset? ValidadaEn { get; private set; }

    public Dispositivo Dispositivo { get; private set; } = null!;

    public Captura Captura { get; private set; } = null!;

    public Usuario? ValidadaPorUsuario { get; private set; }

    public ICollection<Baseline> BaselinesReferencia { get; } = [];

    public ICollection<Comparacion> ComparacionesOrigen { get; } = [];

    public ICollection<Comparacion> ComparacionesDestino { get; } = [];

    public ICollection<Verificacion> Verificaciones { get; } = [];

    public void MarcarComoEstable(long usuarioId, DateTimeOffset fecha)
    {
        if (Estado != EstadoVersion.Activa)
        {
            throw new InvalidOperationException("Una versión retirada no puede marcarse como estable.");
        }

        ValidadaPorUsuarioId = ValidacionDominio.Identificador(usuarioId, nameof(usuarioId));
        ValidadaEn = ValidacionDominio.FechaUtc(fecha, nameof(fecha));
        Estable = true;
    }

    public void QuitarMarcaEstable()
    {
        Estable = false;
        ValidadaPorUsuarioId = null;
        ValidadaEn = null;
    }

    public void Retirar()
    {
        QuitarMarcaEstable();
        Estado = EstadoVersion.Retirada;
    }
}
