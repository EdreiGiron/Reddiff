namespace RedDiff.Aplicacion.Inventario.Dispositivos;

public sealed record CrearDispositivoSolicitud(
    string Nombre,
    string Host,
    string Tipo,
    string? Modelo,
    string Protocolo,
    int Puerto,
    string? FuenteEventos);

public sealed record ActualizarDispositivoSolicitud(
    string Nombre,
    string Host,
    string Tipo,
    string? Modelo,
    string Protocolo,
    int Puerto,
    string? FuenteEventos);

public sealed record CambiarEstadoDispositivoSolicitud(string Estado);

public sealed record ConfigurarAccesoRemotoSolicitud(
    string UsuarioAcceso,
    string SecretoAcceso,
    string AlgoritmoClaveHost,
    string HuellaClaveHost,
    bool HuellaClaveHostConfirmada);

public sealed record DispositivoResumen(
    long Id,
    string Nombre,
    string Host,
    string Tipo,
    string? Modelo,
    string Protocolo,
    int Puerto,
    string? FuenteEventos,
    string Estado,
    bool AccesoRemotoConfigurado);

public sealed record AccesoRemotoResumen(
    long DispositivoId,
    bool Configurado,
    string? UsuarioAcceso,
    string? AlgoritmoClaveHost,
    string? HuellaClaveHost,
    DateTimeOffset? ConfiguradoEn);
