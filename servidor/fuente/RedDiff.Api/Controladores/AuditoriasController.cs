using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Auditorias;
using RedDiff.Aplicacion.Comun;
using RedDiff.Api.Comun;
using RedDiff.Api.Seguridad;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize(Policy = PoliticasSeguridad.Administrador)]
[Route("api/auditorias")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuditoriasController(ServicioAuditorias servicio) : ControllerBase
{
    [HttpGet("catalogo-filtros")]
    public async Task<ActionResult<CatalogoFiltrosAuditoria>> ObtenerCatalogoFiltros(
        CancellationToken cancellationToken = default)
    {
        CatalogoFiltrosAuditoria catalogo =
            await servicio.ObtenerCatalogoFiltrosAsync(cancellationToken);
        return Ok(catalogo);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] long? usuarioId,
        [FromQuery] string? accion,
        [FromQuery] string? entidad,
        [FromQuery] string? estado,
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 25,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<PaginaAuditorias> resultado = await servicio.ListarAsync(
            new FiltrosAuditorias(
                usuarioId,
                accion,
                entidad,
                estado,
                desde,
                hasta,
                pagina,
                tamanoPagina),
            cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }
}
