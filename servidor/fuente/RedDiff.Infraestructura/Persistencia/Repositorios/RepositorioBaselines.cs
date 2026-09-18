using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Cumplimiento;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioBaselines(ContextoRedDiff contexto) : IRepositorioBaselines
{
    public Task<Baseline?> ObtenerPorIdAsync(
        long baselineId,
        bool conSeguimiento,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Baseline> consulta = ConsultaCompleta();
        if (!conSeguimiento)
        {
            consulta = consulta.AsNoTracking();
        }

        return consulta.SingleOrDefaultAsync(
            baseline => baseline.Id == baselineId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Baseline>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await ConsultaCompleta()
            .AsNoTracking()
            .OrderByDescending(baseline => baseline.Activa)
            .ThenBy(baseline => baseline.Nombre)
            .ThenByDescending(baseline => baseline.Id)
            .ToArrayAsync(cancellationToken);
    }

    public void Agregar(Baseline baseline)
    {
        contexto.Baselines.Add(baseline);
    }

    private IQueryable<Baseline> ConsultaCompleta()
    {
        return contexto.Baselines
            .AsSplitQuery()
            .Include(baseline => baseline.Dispositivo)
            .Include(baseline => baseline.VersionReferencia)
                .ThenInclude(version => version!.Dispositivo)
            .Include(baseline => baseline.Reglas);
    }
}
