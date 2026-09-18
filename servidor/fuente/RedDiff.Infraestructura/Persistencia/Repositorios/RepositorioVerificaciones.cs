using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Cumplimiento;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioVerificaciones(ContextoRedDiff contexto)
    : IRepositorioVerificaciones
{
    public Task<Verificacion?> ObtenerPorIdAsync(
        long verificacionId,
        CancellationToken cancellationToken = default)
    {
        return ConsultaCompleta()
            .SingleOrDefaultAsync(
                verificacion => verificacion.Id == verificacionId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Verificacion>> ListarAsync(
        long? dispositivoId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Verificacion> consulta = ConsultaCompleta();
        if (dispositivoId.HasValue)
        {
            consulta = consulta.Where(
                verificacion => verificacion.Version.DispositivoId == dispositivoId.Value);
        }

        return await consulta
            .OrderByDescending(verificacion => verificacion.Fecha)
            .ThenByDescending(verificacion => verificacion.Id)
            .ToArrayAsync(cancellationToken);
    }

    public void Agregar(Verificacion verificacion)
    {
        contexto.Verificaciones.Add(verificacion);
    }

    private IQueryable<Verificacion> ConsultaCompleta()
    {
        return contexto.Verificaciones
            .AsNoTracking()
            .AsSplitQuery()
            .Include(verificacion => verificacion.Baseline)
                .ThenInclude(baseline => baseline.Reglas)
            .Include(verificacion => verificacion.Version)
                .ThenInclude(version => version.Dispositivo)
            .Include(verificacion => verificacion.Usuario)
            .Include(verificacion => verificacion.Resultados)
                .ThenInclude(resultado => resultado.Regla);
    }
}
