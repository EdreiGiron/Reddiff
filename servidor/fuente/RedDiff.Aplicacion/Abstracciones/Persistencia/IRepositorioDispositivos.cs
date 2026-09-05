using RedDiff.Dominio.Entidades.Inventario;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioDispositivos
{
    Task<Dispositivo?> ObtenerPorIdAsync(
        long dispositivoId,
        bool conSeguimiento,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Dispositivo>> ListarAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreAsync(
        string nombre,
        long? dispositivoIdExcluido = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteConexionAsync(
        string host,
        int puerto,
        long? dispositivoIdExcluido = null,
        CancellationToken cancellationToken = default);

    void Agregar(Dispositivo dispositivo);
}
