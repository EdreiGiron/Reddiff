using RedDiff.Aplicacion.Abstracciones.Red;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.PruebasIntegracion.Api;

internal sealed class ConectorCapturaRemotaSimulado(ProtocoloConexion protocolo)
    : IConectorCapturaRemota
{
    public ProtocoloConexion Protocolo { get; } = protocolo;

    public Task<ResultadoConexionRemota> CapturarAsync(
        SolicitudConexionRemota solicitud,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (solicitud.Host.Contains("huella-no-coincide", StringComparison.Ordinal))
        {
            return Task.FromResult(ResultadoConexionRemota.Fallido(
                TipoFalloConexionRemota.IdentidadHostNoCoincide));
        }

        if (solicitud.Host.Contains("contenido-sensible", StringComparison.Ordinal))
        {
            return Task.FromResult(ResultadoConexionRemota.Correcto(
                "hostname sensible\nenable secret clave-no-almacenable\n"));
        }

        if (solicitud.Host.Contains("conexion-no-disponible", StringComparison.Ordinal))
        {
            return Task.FromResult(ResultadoConexionRemota.Fallido(
                TipoFalloConexionRemota.ConexionNoDisponible));
        }

        string contenido = Protocolo == ProtocoloConexion.Ssh
            ? "hostname ssh-simulado\ninterface GigabitEthernet0/1\n description enlace-pruebas\n"
            : "<data><native><hostname>netconf-simulado</hostname></native></data>\n";

        return Task.FromResult(ResultadoConexionRemota.Correcto(contenido));
    }
}
