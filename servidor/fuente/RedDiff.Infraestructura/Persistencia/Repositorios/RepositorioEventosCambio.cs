using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioEventosCambio(ContextoRedDiff contexto)
    : IRepositorioEventosCambio
{
    public Task<EventoCambio?> ObtenerPorHuellaAsync(
        long dispositivoId,
        string huella,
        CancellationToken cancellationToken = default)
    {
        return contexto.EventosCambio
            .AsNoTracking()
            .Include(evento => evento.Dispositivo)
            .Include(evento => evento.Captura!)
                .ThenInclude(captura => captura.VersionConfiguracion)
            .SingleOrDefaultAsync(
                evento => evento.DispositivoId == dispositivoId && evento.Huella == huella,
                cancellationToken);
    }

    public async Task<IReadOnlyList<EventoCambio>> ListarAsync(
        long? dispositivoId,
        EstadoEventoCambio? estado,
        int limite,
        CancellationToken cancellationToken = default)
    {
        IQueryable<EventoCambio> consulta = contexto.EventosCambio
            .AsNoTracking()
            .Include(evento => evento.Dispositivo)
            .Include(evento => evento.Captura!)
                .ThenInclude(captura => captura.VersionConfiguracion);

        if (dispositivoId.HasValue)
        {
            consulta = consulta.Where(evento => evento.DispositivoId == dispositivoId.Value);
        }

        if (estado.HasValue)
        {
            consulta = consulta.Where(evento => evento.Estado == estado.Value);
        }

        return await consulta
            .OrderByDescending(evento => evento.Fecha)
            .ThenByDescending(evento => evento.Id)
            .Take(limite)
            .ToArrayAsync(cancellationToken);
    }

    public void Agregar(EventoCambio evento)
    {
        contexto.EventosCambio.Add(evento);
    }
}
