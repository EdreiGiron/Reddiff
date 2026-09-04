using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Cumplimiento;

public sealed class Verificacion
{
    private Verificacion()
    {
    }

    public Verificacion(
        long baselineId,
        long versionId,
        long usuarioId,
        DateTimeOffset fecha)
    {
        BaselineId = ValidacionDominio.Identificador(baselineId, nameof(baselineId));
        VersionId = ValidacionDominio.Identificador(versionId, nameof(versionId));
        UsuarioId = ValidacionDominio.Identificador(usuarioId, nameof(usuarioId));
        Fecha = ValidacionDominio.FechaUtc(fecha, nameof(fecha));
        Estado = EstadoProceso.Pendiente;
    }

    public long Id { get; private set; }

    public long BaselineId { get; private set; }

    public long VersionId { get; private set; }

    public long UsuarioId { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public EstadoProceso Estado { get; private set; }

    public Baseline Baseline { get; private set; } = null!;

    public VersionConfiguracion Version { get; private set; } = null!;

    public Usuario Usuario { get; private set; } = null!;

    public ICollection<ResultadoRegla> Resultados { get; } = [];

    public void Iniciar(bool baselineActiva)
    {
        if (!baselineActiva)
        {
            throw new InvalidOperationException("No puede evaluarse una baseline inactiva.");
        }

        if (Estado != EstadoProceso.Pendiente)
        {
            throw new InvalidOperationException("Solo una verificación pendiente puede iniciarse.");
        }

        Estado = EstadoProceso.EnProceso;
    }

    public void Completar()
    {
        if (Estado != EstadoProceso.EnProceso)
        {
            throw new InvalidOperationException("Solo una verificación en proceso puede completarse.");
        }

        Estado = EstadoProceso.Completado;
    }

    public void Fallar()
    {
        if (Estado == EstadoProceso.Completado)
        {
            throw new InvalidOperationException("Una verificación completada no puede marcarse como fallida.");
        }

        Estado = EstadoProceso.Fallido;
    }
}
