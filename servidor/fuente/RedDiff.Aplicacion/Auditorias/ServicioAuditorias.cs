using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Comun;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Auditorias;

public sealed class ServicioAuditorias(
    IRepositorioAuditorias repositorioAuditorias,
    IRepositorioUsuarios repositorioUsuarios)
{
    private const int TamanoPaginaMaximo = 100;

    public async Task<CatalogoFiltrosAuditoria> ObtenerCatalogoFiltrosAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarios = await repositorioUsuarios.ListarAsync(cancellationToken);
        IReadOnlyList<string> acciones =
            await repositorioAuditorias.ListarAccionesAsync(cancellationToken);
        IReadOnlyList<string> entidades =
            await repositorioAuditorias.ListarEntidadesAsync(cancellationToken);

        UsuarioFiltroAuditoria[] usuariosFiltro = usuarios
            .Select(usuario => new UsuarioFiltroAuditoria(usuario.Id, usuario.NombreUsuario))
            .ToArray();

        return new CatalogoFiltrosAuditoria(usuariosFiltro, acciones, entidades);
    }

    public async Task<ResultadoOperacion<PaginaAuditorias>> ListarAsync(
        FiltrosAuditorias filtros,
        CancellationToken cancellationToken = default)
    {
        if (filtros.Pagina < 1)
        {
            return ResultadoOperacion<PaginaAuditorias>.Fallido(
                CodigosErrorOperacion.Validacion,
                "La página debe ser mayor o igual que uno.");
        }

        if (filtros.TamanoPagina is < 1 or > TamanoPaginaMaximo)
        {
            return ResultadoOperacion<PaginaAuditorias>.Fallido(
                CodigosErrorOperacion.Validacion,
                $"El tamaño de página debe estar entre 1 y {TamanoPaginaMaximo}.");
        }

        if (filtros.UsuarioId is <= 0)
        {
            return ResultadoOperacion<PaginaAuditorias>.Fallido(
                CodigosErrorOperacion.Validacion,
                "El identificador de usuario no es válido.");
        }

        if (filtros.Desde.HasValue
            && filtros.Hasta.HasValue
            && filtros.Desde.Value > filtros.Hasta.Value)
        {
            return ResultadoOperacion<PaginaAuditorias>.Fallido(
                CodigosErrorOperacion.Validacion,
                "La fecha inicial no puede ser posterior a la fecha final.");
        }

        if (filtros.Accion?.Trim().Length > 120 || filtros.Entidad?.Trim().Length > 120)
        {
            return ResultadoOperacion<PaginaAuditorias>.Fallido(
                CodigosErrorOperacion.Validacion,
                "Los filtros de acción y entidad admiten un máximo de 120 caracteres.");
        }

        string? accion = NormalizarFiltro(filtros.Accion);
        string? entidad = NormalizarFiltro(filtros.Entidad);
        EstadoAuditoria? estado = null;
        if (!string.IsNullOrWhiteSpace(filtros.Estado))
        {
            if (!Enum.TryParse(filtros.Estado.Trim(), true, out EstadoAuditoria estadoConvertido)
                || !Enum.IsDefined(estadoConvertido))
            {
                return ResultadoOperacion<PaginaAuditorias>.Fallido(
                    CodigosErrorOperacion.Validacion,
                    "El estado de auditoría no es válido.");
            }

            estado = estadoConvertido;
        }

        long omitirCalculado = (long)(filtros.Pagina - 1) * filtros.TamanoPagina;
        if (omitirCalculado > int.MaxValue)
        {
            return ResultadoOperacion<PaginaAuditorias>.Fallido(
                CodigosErrorOperacion.Validacion,
                "La página solicitada excede el límite admitido.");
        }

        int omitir = (int)omitirCalculado;
        (IReadOnlyList<Auditoria> registros, int total) =
            await repositorioAuditorias.ListarAsync(
                filtros.UsuarioId,
                accion,
                entidad,
                estado,
                filtros.Desde,
                filtros.Hasta,
                omitir,
                filtros.TamanoPagina,
                cancellationToken);

        AuditoriaResumen[] resumenes = registros.Select(Mapear).ToArray();
        int totalPaginas = total == 0
            ? 0
            : (int)Math.Ceiling(total / (double)filtros.TamanoPagina);

        return ResultadoOperacion<PaginaAuditorias>.Correcto(new PaginaAuditorias(
            resumenes,
            filtros.Pagina,
            filtros.TamanoPagina,
            total,
            totalPaginas));
    }

    private static string? NormalizarFiltro(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return valor.Trim();
    }

    private static AuditoriaResumen Mapear(Auditoria auditoria)
    {
        return new AuditoriaResumen(
            auditoria.Id,
            auditoria.UsuarioId,
            auditoria.Usuario?.NombreUsuario,
            auditoria.Accion,
            auditoria.Entidad,
            auditoria.EntidadId,
            auditoria.Fecha,
            auditoria.Estado.ToString(),
            auditoria.Detalle);
    }
}
