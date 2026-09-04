using RedDiff.Dominio.Entidades.Identidad;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioRoles
{
    Task<Rol?> ObtenerPorIdAsync(
        long rolId,
        bool conSeguimiento,
        CancellationToken cancellationToken = default);

    Task<Rol?> ObtenerPorNombreAsync(
        string nombre,
        bool conSeguimiento,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Rol>> ListarAsync(
        CancellationToken cancellationToken = default);

    void Agregar(Rol rol);
}
