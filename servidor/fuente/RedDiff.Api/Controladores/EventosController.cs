using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Comun;
using RedDiff.Aplicacion.Eventos;
using RedDiff.Api.Comun;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize]
[Route("api/eventos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EventosController(ServicioEventosCambio servicio) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] long? dispositivoId,
        [FromQuery] string? estado,
        [FromQuery] int limite = 100,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<IReadOnlyList<EventoCambioResumen>> resultado =
            await servicio.ListarAsync(dispositivoId, estado, limite, cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }
}
