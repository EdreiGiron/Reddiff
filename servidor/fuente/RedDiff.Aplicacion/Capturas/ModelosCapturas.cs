namespace RedDiff.Aplicacion.Capturas;

public sealed record CargarArchivoConfiguracionSolicitud(
    long DispositivoId,
    string NombreArchivo,
    byte[] Contenido,
    string? Comentario);

public sealed record CapturarConfiguracionRemotaSolicitud(long DispositivoId);

public sealed record CapturaResumen(
    long Id,
    long DispositivoId,
    string Dispositivo,
    string? UsuarioSolicitante,
    string Disparador,
    string Medio,
    string Estado,
    DateTimeOffset Fecha,
    string? Error,
    long? VersionId);

public sealed record VersionConfiguracionResumen(
    long Id,
    long DispositivoId,
    string Dispositivo,
    long CapturaId,
    int Numero,
    string Origen,
    string Hash,
    DateTimeOffset CapturadaEn,
    string? Comentario,
    string Estado,
    bool Estable,
    string? UsuarioSolicitante);

public sealed record VersionConfiguracionDetalle(
    long Id,
    long DispositivoId,
    string Dispositivo,
    long CapturaId,
    int Numero,
    string Origen,
    string Hash,
    DateTimeOffset CapturadaEn,
    string? Comentario,
    string Estado,
    bool Estable,
    string? UsuarioSolicitante,
    string Contenido);

public sealed record CargaArchivoResultado(
    CapturaResumen Captura,
    VersionConfiguracionResumen Version);

public sealed record CapturaRemotaResultado(
    CapturaResumen Captura,
    VersionConfiguracionResumen Version);

public sealed record ArchivoConfiguracionProcesado(
    string NombreArchivo,
    string Contenido,
    string Hash,
    string Comentario);

public sealed record ContenidoConfiguracionProcesado(
    string Contenido,
    string Hash);
