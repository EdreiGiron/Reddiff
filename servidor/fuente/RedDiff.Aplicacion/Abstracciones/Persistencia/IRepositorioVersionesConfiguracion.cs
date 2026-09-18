using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

public interface IRepositorioVersionesConfiguracion
{
    Task<VersionConfiguracion?> ObtenerPorIdAsync(
        long versionId,
        CancellationToken cancellationToken = default);

    Task<VersionConfiguracion?> ObtenerPorIdConSeguimientoAsync(
        long versionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VersionConfiguracion>> ListarAsync(
        long? dispositivoId,
        OrigenVersion? origen,
        EstadoVersion? estado,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default);

    Task<int> ObtenerSiguienteNumeroAsync(
        long dispositivoId,
        CancellationToken cancellationToken = default);

    void Agregar(VersionConfiguracion version);
}
