using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Comparaciones;

public sealed class Comparacion
{
    private Comparacion()
    {
    }

    public Comparacion(
        long versionOrigenId,
        long versionDestinoId,
        long usuarioId,
        DateTimeOffset fecha)
    {
        VersionOrigenId = ValidacionDominio.Identificador(
            versionOrigenId,
            nameof(versionOrigenId));
        VersionDestinoId = ValidacionDominio.Identificador(
            versionDestinoId,
            nameof(versionDestinoId));

        if (versionOrigenId == versionDestinoId)
        {
            throw new ArgumentException("Deben seleccionarse dos versiones diferentes.");
        }

        UsuarioId = ValidacionDominio.Identificador(usuarioId, nameof(usuarioId));
        Fecha = ValidacionDominio.FechaUtc(fecha, nameof(fecha));
        Estado = EstadoProceso.Pendiente;
    }

    public long Id { get; private set; }

    public long VersionOrigenId { get; private set; }

    public long VersionDestinoId { get; private set; }

    public long UsuarioId { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public EstadoProceso Estado { get; private set; }

    public VersionConfiguracion VersionOrigen { get; private set; } = null!;

    public VersionConfiguracion VersionDestino { get; private set; } = null!;

    public Usuario Usuario { get; private set; } = null!;

    public ICollection<DetalleDiferencia> Diferencias { get; } = [];

    public void Iniciar()
    {
        if (Estado != EstadoProceso.Pendiente)
        {
            throw new InvalidOperationException("Solo una comparación pendiente puede iniciarse.");
        }

        Estado = EstadoProceso.EnProceso;
    }

    public void Completar()
    {
        if (Estado != EstadoProceso.EnProceso)
        {
            throw new InvalidOperationException("Solo una comparación en proceso puede completarse.");
        }

        Estado = EstadoProceso.Completado;
    }

    public void Fallar()
    {
        if (Estado == EstadoProceso.Completado)
        {
            throw new InvalidOperationException("Una comparación completada no puede marcarse como fallida.");
        }

        Estado = EstadoProceso.Fallido;
    }
}
