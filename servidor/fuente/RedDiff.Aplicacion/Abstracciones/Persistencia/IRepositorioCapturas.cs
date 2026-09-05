using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioCapturas
{
    Task<IReadOnlyList<Captura>> ListarAsync(
        long? dispositivoId,
        EstadoCaptura? estado,
        CancellationToken cancellationToken = default);

    void Agregar(Captura captura);
}
