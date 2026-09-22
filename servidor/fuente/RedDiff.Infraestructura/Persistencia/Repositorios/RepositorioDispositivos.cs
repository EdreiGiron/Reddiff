using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Inventario;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioDispositivos(ContextoRedDiff contexto)
    : IRepositorioDispositivos
{
    public Task<Dispositivo?> ObtenerPorIdAsync(
        long dispositivoId,
        bool conSeguimiento,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Dispositivo> consulta = contexto.Dispositivos;
        if (!conSeguimiento)
        {
            consulta = consulta.AsNoTracking();
        }

        return consulta.SingleOrDefaultAsync(
            dispositivo => dispositivo.Id == dispositivoId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Dispositivo>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await contexto.Dispositivos
            .AsNoTracking()
            .OrderBy(dispositivo => dispositivo.Nombre)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Dispositivo>> ListarPorHostAsync(
        string host,
        CancellationToken cancellationToken = default)
    {
        return await contexto.Dispositivos
            .Where(dispositivo => dispositivo.Host == host)
            .OrderBy(dispositivo => dispositivo.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> ExisteNombreAsync(
        string nombre,
        long? dispositivoIdExcluido = null,
        CancellationToken cancellationToken = default)
    {
        return contexto.Dispositivos.AnyAsync(
            dispositivo => dispositivo.Nombre == nombre
                && (!dispositivoIdExcluido.HasValue
                    || dispositivo.Id != dispositivoIdExcluido.Value),
            cancellationToken);
    }

    public Task<bool> ExisteConexionAsync(
        string host,
        int puerto,
        long? dispositivoIdExcluido = null,
        CancellationToken cancellationToken = default)
    {
        return contexto.Dispositivos.AnyAsync(
            dispositivo => dispositivo.Host == host
                && dispositivo.Puerto == puerto
                && (!dispositivoIdExcluido.HasValue
                    || dispositivo.Id != dispositivoIdExcluido.Value),
            cancellationToken);
    }

    public void Agregar(Dispositivo dispositivo)
    {
        contexto.Dispositivos.Add(dispositivo);
    }
}
