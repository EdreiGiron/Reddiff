using RedDiff.Dominio.Entidades.Identidad;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioUsuarios
{
    Task<Usuario?> ObtenerPorIdAsync(
        long usuarioId,
        bool conSeguimiento,
        CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorNombreAsync(
        string nombreUsuario,
        bool conSeguimiento,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ListarAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExisteNombreAsync(
        string nombreUsuario,
        long? usuarioIdExcluido = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAlgunoAsync(CancellationToken cancellationToken = default);

    void Agregar(Usuario usuario);
}
