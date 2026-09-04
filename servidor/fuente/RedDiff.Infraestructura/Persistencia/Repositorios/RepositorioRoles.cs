using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Identidad;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioRoles(ContextoRedDiff contexto) : IRepositorioRoles
{
    public Task<Rol?> ObtenerPorIdAsync(
        long rolId,
        bool conSeguimiento,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Rol> consulta = contexto.Roles;
        if (!conSeguimiento)
        {
            consulta = consulta.AsNoTracking();
        }

        return consulta.SingleOrDefaultAsync(rol => rol.Id == rolId, cancellationToken);
    }

    public Task<Rol?> ObtenerPorNombreAsync(
        string nombre,
        bool conSeguimiento,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Rol> consulta = contexto.Roles;
        if (!conSeguimiento)
        {
            consulta = consulta.AsNoTracking();
        }

        return consulta.SingleOrDefaultAsync(rol => rol.Nombre == nombre, cancellationToken);
    }

    public async Task<IReadOnlyList<Rol>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await contexto.Roles
            .AsNoTracking()
            .OrderBy(rol => rol.Id)
            .ToArrayAsync(cancellationToken);
    }

    public void Agregar(Rol rol)
    {
        contexto.Roles.Add(rol);
    }
}
