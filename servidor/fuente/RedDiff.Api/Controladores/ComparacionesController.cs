using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Comparaciones;
using RedDiff.Aplicacion.Comun;
using RedDiff.Api.Comun;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize]
[Route("api/comparaciones")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ComparacionesController(ServicioComparacionesConfiguracion servicio)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] long? dispositivoId,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<IReadOnlyList<ComparacionResumen>> resultado =
            await servicio.ListarAsync(dispositivoId, cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpGet("{comparacionId:long}")]
    public async Task<IActionResult> Obtener(
        long comparacionId,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<ComparacionDetalle> resultado = await servicio.ObtenerAsync(
            comparacionId,
            cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Comparar(
        [FromBody] CompararVersionesSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<ComparacionDetalle> resultado = await servicio.CompararAsync(
            ObtenerUsuarioId(),
            solicitud,
            cancellationToken);
        return resultado.Exitoso
            ? CreatedAtAction(
                nameof(Obtener),
                new { comparacionId = resultado.Valor!.Id },
                resultado.Valor)
            : this.ComoProblema(resultado);
    }

    private long ObtenerUsuarioId()
    {
        return long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
