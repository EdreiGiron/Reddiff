using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Comun;
using RedDiff.Aplicacion.Cumplimiento;
using RedDiff.Api.Comun;
using RedDiff.Api.Seguridad;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize]
[Route("api/baselines")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BaselinesController(ServicioBaselines servicio) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        IReadOnlyList<BaselineResumen> baselines = await servicio.ListarAsync(cancellationToken);
        return Ok(baselines);
    }

    [HttpGet("{baselineId:long}")]
    public async Task<IActionResult> Obtener(
        long baselineId,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<BaselineResumen> resultado = await servicio.ObtenerAsync(
            baselineId,
            cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasSeguridad.Administrador)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearBaselineSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<BaselineResumen> resultado = await servicio.CrearAsync(
            ObtenerUsuarioId(),
            solicitud,
            cancellationToken);
        return resultado.Exitoso
            ? CreatedAtAction(
                nameof(Obtener),
                new { baselineId = resultado.Valor!.Id },
                resultado.Valor)
            : this.ComoProblema(resultado);
    }

    [HttpPatch("{baselineId:long}/estado")]
    [Authorize(Policy = PoliticasSeguridad.Administrador)]
    public async Task<IActionResult> CambiarEstado(
        long baselineId,
        [FromBody] CambiarEstadoBaselineSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<BaselineResumen> resultado = await servicio.CambiarEstadoAsync(
            ObtenerUsuarioId(),
            baselineId,
            solicitud.Activa,
            cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    private long ObtenerUsuarioId()
    {
        return long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
