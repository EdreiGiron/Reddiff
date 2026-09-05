using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Infraestructura.Persistencia.Repositorios;

internal sealed class RepositorioCapturas(ContextoRedDiff contexto) : IRepositorioCapturas
{
    public async Task<IReadOnlyList<Captura>> ListarAsync(
        long? dispositivoId,
        EstadoCaptura? estado,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Captura> consulta = contexto.Capturas
            .AsNoTracking()
            .Include(captura => captura.Dispositivo)
            .Include(captura => captura.UsuarioSolicitante)
            .Include(captura => captura.VersionConfiguracion);

        if (dispositivoId.HasValue)
        {
            consulta = consulta.Where(
                captura => captura.DispositivoId == dispositivoId.Value);
        }

        if (estado.HasValue)
        {
            consulta = consulta.Where(captura => captura.Estado == estado.Value);
        }

        return await consulta
            .OrderByDescending(captura => captura.Fecha)
            .ThenByDescending(captura => captura.Id)
            .ToArrayAsync(cancellationToken);
    }

    public void Agregar(Captura captura)
    {
        contexto.Capturas.Add(captura);
    }
}
