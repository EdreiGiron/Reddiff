namespace RedDiff.Aplicacion.Auditorias;

public sealed record AuditoriaResumen(
    long Id,
    long? UsuarioId,
    string? NombreUsuario,
    string Accion,
    string Entidad,
    long? EntidadId,
    DateTimeOffset Fecha,
    string Estado,
    string? Detalle);

public sealed record PaginaAuditorias(
    IReadOnlyList<AuditoriaResumen> Registros,
    int Pagina,
    int TamanoPagina,
    int TotalRegistros,
    int TotalPaginas);

public sealed record UsuarioFiltroAuditoria(
    long Id,
    string NombreUsuario);

public sealed record CatalogoFiltrosAuditoria(
    IReadOnlyList<UsuarioFiltroAuditoria> Usuarios,
    IReadOnlyList<string> Acciones,
    IReadOnlyList<string> Entidades);

public sealed record FiltrosAuditorias(
    long? UsuarioId,
    string? Accion,
    string? Entidad,
    string? Estado,
    DateTimeOffset? Desde,
    DateTimeOffset? Hasta,
    int Pagina = 1,
    int TamanoPagina = 25);
