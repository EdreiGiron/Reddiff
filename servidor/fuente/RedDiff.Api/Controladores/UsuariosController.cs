using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Comun;
using RedDiff.Aplicacion.Seguridad.Usuarios;
using RedDiff.Api.Comun;
using RedDiff.Api.Seguridad;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize(Policy = PoliticasSeguridad.Administrador)]
[Route("api/usuarios")]
public sealed class UsuariosController(ServicioGestionUsuarios servicio) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        IReadOnlyList<UsuarioResumen> usuarios = await servicio.ListarUsuariosAsync(
            cancellationToken);
        return Ok(usuarios);
    }

    [HttpGet("{usuarioId:long}")]
    public async Task<IActionResult> Obtener(
        long usuarioId,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<UsuarioResumen> resultado = await servicio.ObtenerUsuarioAsync(
            usuarioId,
            cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearUsuarioSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<UsuarioResumen> resultado = await servicio.CrearUsuarioAsync(
            ObtenerUsuarioId(),
            solicitud,
            cancellationToken);

        return resultado.Exitoso
            ? CreatedAtAction(nameof(Obtener), new { usuarioId = resultado.Valor!.Id }, resultado.Valor)
            : this.ComoProblema(resultado);
    }

    [HttpPut("{usuarioId:long}")]
    public async Task<IActionResult> Actualizar(
        long usuarioId,
        [FromBody] ActualizarUsuarioSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<UsuarioResumen> resultado = await servicio.ActualizarUsuarioAsync(
            ObtenerUsuarioId(),
            usuarioId,
            solicitud,
            cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpPatch("{usuarioId:long}/estado")]
    public async Task<IActionResult> CambiarEstado(
        long usuarioId,
        [FromBody] CambiarEstadoUsuarioSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<UsuarioResumen> resultado = await servicio.CambiarEstadoAsync(
            ObtenerUsuarioId(),
            usuarioId,
            solicitud.Activo,
            cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    private long ObtenerUsuarioId()
    {
        return long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
