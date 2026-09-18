using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Comparaciones;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioComparaciones(ContextoRedDiff contexto)
    : IRepositorioComparaciones
{
    public Task<Comparacion?> ObtenerPorIdAsync(
        long comparacionId,
        CancellationToken cancellationToken = default)
    {
        return ConsultaCompleta()
            .SingleOrDefaultAsync(
                comparacion => comparacion.Id == comparacionId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Comparacion>> ListarAsync(
        long? dispositivoId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Comparacion> consulta = ConsultaCompleta();
        if (dispositivoId.HasValue)
        {
            consulta = consulta.Where(
                comparacion => comparacion.VersionOrigen.DispositivoId == dispositivoId.Value);
        }

        return await consulta
            .OrderByDescending(comparacion => comparacion.Fecha)
            .ThenByDescending(comparacion => comparacion.Id)
            .ToArrayAsync(cancellationToken);
    }

    public void Agregar(Comparacion comparacion)
    {
        contexto.Comparaciones.Add(comparacion);
    }

    private IQueryable<Comparacion> ConsultaCompleta()
    {
        return contexto.Comparaciones
            .AsNoTracking()
            .AsSplitQuery()
            .Include(comparacion => comparacion.VersionOrigen)
                .ThenInclude(version => version.Dispositivo)
            .Include(comparacion => comparacion.VersionDestino)
            .Include(comparacion => comparacion.Usuario)
            .Include(comparacion => comparacion.Diferencias);
    }
}
