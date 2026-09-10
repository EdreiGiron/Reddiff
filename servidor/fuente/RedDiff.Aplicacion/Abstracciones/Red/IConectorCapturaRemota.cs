using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Abstracciones.Red;

public interface IConectorCapturaRemota
{
    ProtocoloConexion Protocolo { get; }

    /// <summary>
    /// Obtiene exclusivamente la configuración del dispositivo. La implementación debe
    /// validar la identidad del host contra la huella esperada antes de autenticar y no
    /// debe aceptar comandos proporcionados por el usuario ni modificar el equipo.
    /// </summary>
    Task<ResultadoConexionRemota> CapturarAsync(
        SolicitudConexionRemota solicitud,
        CancellationToken cancellationToken = default);
}

public sealed record SolicitudConexionRemota(
    string Host,
    int Puerto,
    string Usuario,
    string Secreto,
    string AlgoritmoClaveHost,
    string HuellaClaveHostEsperada,
    TimeSpan TiempoEspera);

public enum TipoFalloConexionRemota
{
    IdentidadHostNoCoincide = 1,
    AutenticacionRechazada = 2,
    TiempoAgotado = 3,
    ConexionNoDisponible = 4,
    RespuestaInvalida = 5
}

public sealed record ResultadoConexionRemota
{
    private ResultadoConexionRemota(
        string? contenido,
        TipoFalloConexionRemota? tipoFallo)
    {
        Contenido = contenido;
        TipoFallo = tipoFallo;
    }

    public bool Exitoso => TipoFallo is null;

    public string? Contenido { get; }

    public TipoFalloConexionRemota? TipoFallo { get; }

    public static ResultadoConexionRemota Correcto(string contenido)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        return new ResultadoConexionRemota(contenido, null);
    }

    public static ResultadoConexionRemota Fallido(TipoFalloConexionRemota tipoFallo)
    {
        if (!Enum.IsDefined(tipoFallo))
        {
            throw new ArgumentException("El tipo de fallo remoto no es válido.", nameof(tipoFallo));
        }

        return new ResultadoConexionRemota(null, tipoFallo);
    }
}
