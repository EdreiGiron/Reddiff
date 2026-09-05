using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioVersionesConfiguracion(ContextoRedDiff contexto)
    : IRepositorioVersionesConfiguracion
{
    public Task<VersionConfiguracion?> ObtenerPorIdAsync(
        long versionId,
        CancellationToken cancellationToken = default)
    {
        return contexto.VersionesConfiguracion
            .AsNoTracking()
            .Include(version => version.Dispositivo)
            .Include(version => version.Captura)
                .ThenInclude(captura => captura.UsuarioSolicitante)
            .SingleOrDefaultAsync(version => version.Id == versionId, cancellationToken);
    }

    public async Task<IReadOnlyList<VersionConfiguracion>> ListarAsync(
        long? dispositivoId,
        OrigenVersion? origen,
        EstadoVersion? estado,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default)
    {
        IQueryable<VersionConfiguracion> consulta = contexto.VersionesConfiguracion
            .AsNoTracking()
            .Include(version => version.Dispositivo)
            .Include(version => version.Captura)
                .ThenInclude(captura => captura.UsuarioSolicitante);

        if (dispositivoId.HasValue)
        {
            consulta = consulta.Where(
                version => version.DispositivoId == dispositivoId.Value);
        }

        if (origen.HasValue)
        {
            consulta = consulta.Where(version => version.Origen == origen.Value);
        }

        if (estado.HasValue)
        {
            consulta = consulta.Where(version => version.Estado == estado.Value);
        }

        if (desde.HasValue)
        {
            consulta = consulta.Where(version => version.CapturadaEn >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(version => version.CapturadaEn <= hasta.Value);
        }

        return await consulta
            .OrderByDescending(version => version.CapturadaEn)
            .ThenByDescending(version => version.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<int> ObtenerSiguienteNumeroAsync(
        long dispositivoId,
        CancellationToken cancellationToken = default)
    {
        int ultimoNumero = await contexto.VersionesConfiguracion
            .Where(version => version.DispositivoId == dispositivoId)
            .Select(version => (int?)version.Numero)
            .MaxAsync(cancellationToken)
            ?? 0;

        return checked(ultimoNumero + 1);
    }

    public void Agregar(VersionConfiguracion version)
    {
        contexto.VersionesConfiguracion.Add(version);
    }
}
