using RedDiff.Dominio.Entidades.Cumplimiento;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioBaselines
{
    Task<Baseline?> ObtenerPorIdAsync(
        long baselineId,
        bool conSeguimiento,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Baseline>> ListarAsync(
        CancellationToken cancellationToken = default);

    void Agregar(Baseline baseline);
}
