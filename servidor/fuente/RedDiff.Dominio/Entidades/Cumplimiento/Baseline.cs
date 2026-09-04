using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Cumplimiento;

public sealed class Baseline
{
    private Baseline()
    {
    }

    private Baseline(
        long? dispositivoId,
        string? tipoDispositivo,
        long? versionReferenciaId,
        string nombre)
    {
        DispositivoId = dispositivoId;
        TipoDispositivo = tipoDispositivo;
        VersionReferenciaId = versionReferenciaId;
        Nombre = ValidacionDominio.TextoObligatorio(nombre, nameof(nombre), 150);
        Activa = true;
    }

    public long Id { get; private set; }

    public long? DispositivoId { get; private set; }

    public string? TipoDispositivo { get; private set; }

    public long? VersionReferenciaId { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public bool Activa { get; private set; }

    public Dispositivo? Dispositivo { get; private set; }

    public VersionConfiguracion? VersionReferencia { get; private set; }

    public ICollection<ReglaBaseline> Reglas { get; } = [];

    public ICollection<Verificacion> Verificaciones { get; } = [];

    public static Baseline ParaDispositivo(
        long dispositivoId,
        string nombre,
        long? versionReferenciaId = null)
    {
        ValidacionDominio.Identificador(dispositivoId, nameof(dispositivoId));
        ValidarVersionReferencia(versionReferenciaId);

        return new Baseline(dispositivoId, null, versionReferenciaId, nombre);
    }

    public static Baseline ParaTipoDispositivo(
        string tipoDispositivo,
        string nombre,
        long? versionReferenciaId = null)
    {
        string tipo = ValidacionDominio.TextoObligatorio(
            tipoDispositivo,
            nameof(tipoDispositivo),
            100);
        ValidarVersionReferencia(versionReferenciaId);

        return new Baseline(null, tipo, versionReferenciaId, nombre);
    }

    public void CambiarNombre(string nombre)
    {
        Nombre = ValidacionDominio.TextoObligatorio(nombre, nameof(nombre), 150);
    }

    public void CambiarVersionReferencia(long? versionReferenciaId)
    {
        ValidarVersionReferencia(versionReferenciaId);
        VersionReferenciaId = versionReferenciaId;
    }

    public void Activar()
    {
        Activa = true;
    }

    public void Desactivar()
    {
        Activa = false;
    }

    private static void ValidarVersionReferencia(long? versionReferenciaId)
    {
        if (versionReferenciaId.HasValue)
        {
            ValidacionDominio.Identificador(
                versionReferenciaId.Value,
                nameof(versionReferenciaId));
        }
    }
}
