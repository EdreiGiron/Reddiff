using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Comun;
using RedDiff.Aplicacion.Cumplimiento;
using RedDiff.Api.Comun;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize]
[Route("api/verificaciones")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VerificacionesController(ServicioVerificacionesConfiguracion servicio)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] long? dispositivoId,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<IReadOnlyList<VerificacionResumen>> resultado =
            await servicio.ListarAsync(dispositivoId, cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpGet("{verificacionId:long}")]
    public async Task<IActionResult> Obtener(
        long verificacionId,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<VerificacionDetalle> resultado = await servicio.ObtenerAsync(
            verificacionId,
            cancellationToken);
        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Verificar(
        [FromBody] VerificarVersionSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<VerificacionDetalle> resultado = await servicio.VerificarAsync(
            ObtenerUsuarioId(),
            solicitud,
            cancellationToken);
        return resultado.Exitoso
            ? CreatedAtAction(
                nameof(Obtener),
                new { verificacionId = resultado.Valor!.Id },
                resultado.Valor)
            : this.ComoProblema(resultado);
    }

    private long ObtenerUsuarioId()
    {
        return long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
