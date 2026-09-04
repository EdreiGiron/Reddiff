using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Identidad;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioUsuarios(ContextoRedDiff contexto) : IRepositorioUsuarios
{
    public Task<Usuario?> ObtenerPorIdAsync(
        long usuarioId,
        bool conSeguimiento,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Usuario> consulta = contexto.Usuarios.Include(usuario => usuario.Rol);
        if (!conSeguimiento)
        {
            consulta = consulta.AsNoTracking();
        }

        return consulta.SingleOrDefaultAsync(
            usuario => usuario.Id == usuarioId,
            cancellationToken);
    }

    public Task<Usuario?> ObtenerPorNombreAsync(
        string nombreUsuario,
        bool conSeguimiento,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Usuario> consulta = contexto.Usuarios.Include(usuario => usuario.Rol);
        if (!conSeguimiento)
        {
            consulta = consulta.AsNoTracking();
        }

        return consulta.SingleOrDefaultAsync(
            usuario => usuario.NombreUsuario == nombreUsuario,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Usuario>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await contexto.Usuarios
            .AsNoTracking()
            .Include(usuario => usuario.Rol)
            .OrderBy(usuario => usuario.NombreUsuario)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> ExisteNombreAsync(
        string nombreUsuario,
        long? usuarioIdExcluido = null,
        CancellationToken cancellationToken = default)
    {
        return contexto.Usuarios.AnyAsync(
            usuario => usuario.NombreUsuario == nombreUsuario
                && (!usuarioIdExcluido.HasValue || usuario.Id != usuarioIdExcluido.Value),
            cancellationToken);
    }

    public Task<bool> ExisteAlgunoAsync(CancellationToken cancellationToken = default)
    {
        return contexto.Usuarios.AnyAsync(cancellationToken);
    }

    public void Agregar(Usuario usuario)
    {
        contexto.Usuarios.Add(usuario);
    }
}
