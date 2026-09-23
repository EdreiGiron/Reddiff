using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioAuditorias(ContextoRedDiff contexto) : IRepositorioAuditorias
{
    public async Task<IReadOnlyList<string>> ListarAccionesAsync(
        CancellationToken cancellationToken = default)
    {
        return await contexto.Auditorias
            .AsNoTracking()
            .Select(auditoria => auditoria.Accion)
            .Distinct()
            .OrderBy(accion => accion)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListarEntidadesAsync(
        CancellationToken cancellationToken = default)
    {
        return await contexto.Auditorias
            .AsNoTracking()
            .Select(auditoria => auditoria.Entidad)
            .Distinct()
            .OrderBy(entidad => entidad)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Auditoria> Registros, int Total)> ListarAsync(
        long? usuarioId,
        string? accion,
        string? entidad,
        EstadoAuditoria? estado,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        int omitir,
        int tomar,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Auditoria> consulta = contexto.Auditorias
            .AsNoTracking()
            .Include(auditoria => auditoria.Usuario);

        if (usuarioId.HasValue)
        {
            consulta = consulta.Where(auditoria => auditoria.UsuarioId == usuarioId.Value);
        }

        if (accion is not null)
        {
            consulta = consulta.Where(auditoria => auditoria.Accion == accion);
        }

        if (entidad is not null)
        {
            consulta = consulta.Where(auditoria => auditoria.Entidad == entidad);
        }

        if (estado.HasValue)
        {
            consulta = consulta.Where(auditoria => auditoria.Estado == estado.Value);
        }

        if (desde.HasValue)
        {
            consulta = consulta.Where(auditoria => auditoria.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(auditoria => auditoria.Fecha <= hasta.Value);
        }

        int total = await consulta.CountAsync(cancellationToken);
        Auditoria[] registros = await consulta
            .OrderByDescending(auditoria => auditoria.Fecha)
            .ThenByDescending(auditoria => auditoria.Id)
            .Skip(omitir)
            .Take(tomar)
            .ToArrayAsync(cancellationToken);

        return (registros, total);
    }

    public void Agregar(Auditoria auditoria)
    {
        contexto.Auditorias.Add(auditoria);
    }
}
