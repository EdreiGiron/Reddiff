using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RedDiff.Aplicacion.Seguridad.Autenticacion;
using RedDiff.Api.Seguridad;

namespace RedDiff.Api.Controladores;

[ApiController]
[Route("api/autenticacion")]
public sealed class AutenticacionController(
    ServicioAutenticacion servicioAutenticacion,
    IAntiforgery antiforgery,
    IOptions<OpcionesSeguridad> opcionesSeguridad,
    TimeProvider reloj) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("proteccion-csrf")]
    public IActionResult ObtenerProteccionCsrf()
    {
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [AllowAnonymous]
    [EnableRateLimiting(PoliticasSeguridad.LimiteAutenticacion)]
    [HttpPost("iniciar-sesion")]
    public async Task<IActionResult> IniciarSesion(
        [FromBody] IniciarSesionSolicitud? solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoAutenticacion resultado = await servicioAutenticacion.IniciarSesionAsync(
            solicitud?.NombreUsuario,
            solicitud?.Contrasena,
            cancellationToken);

        if (!resultado.Exitoso)
        {
            return Unauthorized(new
            {
                codigo = "credenciales_invalidas",
                mensaje = "Las credenciales no son válidas o la cuenta está inactiva."
            });
        }

        SesionUsuario sesion = resultado.Sesion!;
        DateTimeOffset expiraEn = reloj.GetUtcNow().AddMinutes(
            opcionesSeguridad.Value.DuracionSesionMinutos);

        Claim[] reclamaciones =
        [
            new Claim(ClaimTypes.NameIdentifier, sesion.UsuarioId.ToString()),
            new Claim(ClaimTypes.Name, sesion.NombreUsuario),
            new Claim(ClaimTypes.Role, sesion.Rol),
            new Claim(
                PoliticasSeguridad.ReclamacionVersionCredencial,
                sesion.VersionCredencial)
        ];

        ClaimsIdentity identidad = new(
            reclamaciones,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties
            {
                AllowRefresh = false,
                IsPersistent = false,
                ExpiresUtc = expiraEn
            });

        return Ok(new SesionActivaRespuesta(
            sesion.UsuarioId,
            sesion.NombreUsuario,
            sesion.Rol,
            expiraEn));
    }

    [Authorize]
    [HttpGet("sesion")]
    public async Task<IActionResult> ObtenerSesion()
    {
        AuthenticateResult autenticacion = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(new SesionActivaRespuesta(
            ObtenerUsuarioId(),
            User.Identity!.Name!,
            User.FindFirstValue(ClaimTypes.Role)!,
            autenticacion.Properties?.ExpiresUtc));
    }

    [Authorize]
    [HttpPost("cerrar-sesion")]
    public async Task<IActionResult> CerrarSesion(CancellationToken cancellationToken)
    {
        long usuarioId = ObtenerUsuarioId();
        await servicioAutenticacion.RegistrarCierreSesionAsync(
            usuarioId,
            cancellationToken);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private long ObtenerUsuarioId()
    {
        return long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public sealed record IniciarSesionSolicitud(string NombreUsuario, string Contrasena);

    public sealed record SesionActivaRespuesta(
        long UsuarioId,
        string NombreUsuario,
        string Rol,
        DateTimeOffset? ExpiraEn);
}
