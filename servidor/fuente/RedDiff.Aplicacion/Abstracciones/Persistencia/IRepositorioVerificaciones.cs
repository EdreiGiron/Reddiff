using RedDiff.Dominio.Entidades.Cumplimiento;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioVerificaciones
{
    Task<Verificacion?> ObtenerPorIdAsync(
        long verificacionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Verificacion>> ListarAsync(
        long? dispositivoId,
        CancellationToken cancellationToken = default);

    void Agregar(Verificacion verificacion);
}
