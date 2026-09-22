namespace RedDiff.Aplicacion.Eventos;

public sealed record EventoCambioResumen(
    long Id,
    long DispositivoId,
    string Dispositivo,
    string Fuente,
    string Tipo,
    DateTimeOffset Fecha,
    string Huella,
    string Estado,
    string Resumen,
    long? CapturaId,
    long? VersionId);

public sealed record RecepcionEventoResultado(
    EventoCambioResumen Evento,
    bool Duplicado,
    bool CapturaRealizada,
    bool VersionGenerada);

public sealed record EventoSyslogProcesado(string Contenido, string Huella);
