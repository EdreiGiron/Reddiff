using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Comun;
using RedDiff.Aplicacion.Inventario.Dispositivos;
using RedDiff.Api.Comun;
using RedDiff.Api.Seguridad;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize]
[Route("api/dispositivos")]
public sealed class DispositivosController(ServicioGestionDispositivos servicio) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        IReadOnlyList<DispositivoResumen> dispositivos = await servicio.ListarAsync(
            cancellationToken);
        return Ok(dispositivos);
    }

    [HttpGet("{dispositivoId:long}")]
    public async Task<IActionResult> Obtener(
        long dispositivoId,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<DispositivoResumen> resultado = await servicio.ObtenerAsync(
            dispositivoId,
            cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasSeguridad.Administrador)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearDispositivoSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<DispositivoResumen> resultado = await servicio.CrearAsync(
            ObtenerUsuarioId(),
            solicitud,
            cancellationToken);

        return resultado.Exitoso
            ? CreatedAtAction(
                nameof(Obtener),
                new { dispositivoId = resultado.Valor!.Id },
                resultado.Valor)
            : this.ComoProblema(resultado);
    }

    [HttpPut("{dispositivoId:long}")]
    [Authorize(Policy = PoliticasSeguridad.Administrador)]
    public async Task<IActionResult> Actualizar(
        long dispositivoId,
        [FromBody] ActualizarDispositivoSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<DispositivoResumen> resultado = await servicio.ActualizarAsync(
            ObtenerUsuarioId(),
            dispositivoId,
            solicitud,
            cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpPatch("{dispositivoId:long}/estado")]
    [Authorize(Policy = PoliticasSeguridad.Administrador)]
    public async Task<IActionResult> CambiarEstado(
        long dispositivoId,
        [FromBody] CambiarEstadoDispositivoSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<DispositivoResumen> resultado = await servicio.CambiarEstadoAsync(
            ObtenerUsuarioId(),
            dispositivoId,
            solicitud.Estado,
            cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    private long ObtenerUsuarioId()
    {
        return long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
