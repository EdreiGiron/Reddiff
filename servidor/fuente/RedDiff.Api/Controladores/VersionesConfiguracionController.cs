using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Capturas;
using RedDiff.Aplicacion.Comun;
using RedDiff.Api.Comun;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize]
[Route("api/versiones-configuracion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VersionesConfiguracionController(ServicioCapturasConfiguracion servicio)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] long? dispositivoId,
        [FromQuery] string? origen,
        [FromQuery] string? estado,
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<IReadOnlyList<VersionConfiguracionResumen>> resultado =
            await servicio.ListarVersionesAsync(
                dispositivoId,
                origen,
                estado,
                desde,
                hasta,
                cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpGet("{versionId:long}")]
    public async Task<IActionResult> Obtener(
        long versionId,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<VersionConfiguracionDetalle> resultado =
            await servicio.ObtenerVersionAsync(versionId, cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }
}
