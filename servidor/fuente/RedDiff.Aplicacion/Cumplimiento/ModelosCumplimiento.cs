namespace RedDiff.Aplicacion.Cumplimiento;

public sealed record CrearReglaBaselineSolicitud(
    string Criterio,
    string Esperado,
    bool Obligatoria);

public sealed record CrearBaselineSolicitud(
    string Nombre,
    string Alcance,
    long? DispositivoId,
    string? TipoDispositivo,
    long? VersionReferenciaId,
    IReadOnlyList<CrearReglaBaselineSolicitud> Reglas);

public sealed record CambiarEstadoBaselineSolicitud(bool Activa);

public sealed record ReglaBaselineResumen(
    long Id,
    string Criterio,
    string Esperado,
    bool Obligatoria);

public sealed record VersionReferenciaBaselineResumen(
    long Id,
    int Numero,
    string Dispositivo,
    string Hash,
    DateTimeOffset CapturadaEn,
    bool Estable);

public sealed record BaselineResumen(
    long Id,
    string Nombre,
    string Alcance,
    long? DispositivoId,
    string? Dispositivo,
    string? TipoDispositivo,
    bool Activa,
    VersionReferenciaBaselineResumen? VersionReferencia,
    IReadOnlyList<ReglaBaselineResumen> Reglas);

public sealed record VerificarVersionSolicitud(
    long BaselineId,
    long VersionId);

public sealed record ResultadoReglaResumen(
    long ReglaId,
    string Criterio,
    string Esperado,
    bool Obligatoria,
    string Estado,
    string Evidencia);

public sealed record VerificacionResumen(
    long Id,
    long BaselineId,
    string Baseline,
    long DispositivoId,
    string Dispositivo,
    long VersionId,
    int Version,
    string Usuario,
    DateTimeOffset Fecha,
    string Estado,
    string ResultadoGeneral,
    int TotalReglas,
    int Cumplidas,
    int Incumplidas,
    int NoEvaluables,
    decimal PorcentajeCumplimiento);

public sealed record VerificacionDetalle(
    long Id,
    long BaselineId,
    string Baseline,
    long DispositivoId,
    string Dispositivo,
    long VersionId,
    int Version,
    string Usuario,
    DateTimeOffset Fecha,
    string Estado,
    string ResultadoGeneral,
    int TotalReglas,
    int Cumplidas,
    int Incumplidas,
    int NoEvaluables,
    decimal PorcentajeCumplimiento,
    IReadOnlyList<ResultadoReglaResumen> Resultados);

public sealed record ResultadoEvaluacionRegla(
    string Estado,
    string Evidencia);
