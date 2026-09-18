using RedDiff.Dominio.Entidades.Comparaciones;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioComparaciones
{
    Task<Comparacion?> ObtenerPorIdAsync(
        long comparacionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Comparacion>> ListarAsync(
        long? dispositivoId,
        CancellationToken cancellationToken = default);

    void Agregar(Comparacion comparacion);
}
