using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioAuditorias
{
    Task<IReadOnlyList<string>> ListarAccionesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListarEntidadesAsync(
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Auditoria> Registros, int Total)> ListarAsync(
        long? usuarioId,
        string? accion,
        string? entidad,
        EstadoAuditoria? estado,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        int omitir,
        int tomar,
        CancellationToken cancellationToken = default);

    void Agregar(Auditoria auditoria);
}
