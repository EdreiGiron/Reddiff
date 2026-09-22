using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioEventosCambio
{
    Task<EventoCambio?> ObtenerPorHuellaAsync(
        long dispositivoId,
        string huella,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventoCambio>> ListarAsync(
        long? dispositivoId,
        EstadoEventoCambio? estado,
        int limite,
        CancellationToken cancellationToken = default);

    void Agregar(EventoCambio evento);
}
