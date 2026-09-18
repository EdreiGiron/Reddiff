namespace RedDiff.Aplicacion.Comparaciones;

public sealed record CompararVersionesSolicitud(
    long VersionOrigenId,
    long VersionDestinoId);

public sealed record VersionComparadaResumen(
    long Id,
    int Numero,
    string Hash,
    DateTimeOffset CapturadaEn,
    bool Estable);

public sealed record ComparacionResumen(
    long Id,
    long DispositivoId,
    string Dispositivo,
    VersionComparadaResumen VersionOrigen,
    VersionComparadaResumen VersionDestino,
    string Usuario,
    DateTimeOffset Fecha,
    string Estado,
    int Agregadas,
    int Eliminadas,
    int Modificadas);

public sealed record DiferenciaComparacionResumen(
    int Linea,
    string Tipo,
    string? TextoAnterior,
    string? TextoNuevo);

public sealed record ComparacionDetalle(
    long Id,
    long DispositivoId,
    string Dispositivo,
    VersionComparadaResumen VersionOrigen,
    VersionComparadaResumen VersionDestino,
    string Usuario,
    DateTimeOffset Fecha,
    string Estado,
    int Agregadas,
    int Eliminadas,
    int Modificadas,
    IReadOnlyList<DiferenciaComparacionResumen> Diferencias);

public sealed record DiferenciaCalculada(
    int Linea,
    string? TextoAnterior,
    string? TextoNuevo);
