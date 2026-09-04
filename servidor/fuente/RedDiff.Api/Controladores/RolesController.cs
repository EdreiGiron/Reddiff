using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Seguridad.Usuarios;
using RedDiff.Api.Seguridad;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize(Policy = PoliticasSeguridad.Administrador)]
[Route("api/roles")]
public sealed class RolesController(ServicioGestionUsuarios servicio) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        IReadOnlyList<RolResumen> roles = await servicio.ListarRolesAsync(cancellationToken);
        return Ok(roles);
    }
}
