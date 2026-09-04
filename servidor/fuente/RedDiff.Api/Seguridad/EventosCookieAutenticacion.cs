using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Seguridad;
using RedDiff.Dominio.Entidades.Identidad;

namespace RedDiff.Api.Seguridad;

public sealed class EventosCookieAutenticacion(
    IRepositorioUsuarios repositorioUsuarios) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext contexto)
    {
        ClaimsPrincipal? principal = contexto.Principal;
        string? identificador = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (principal is null || !long.TryParse(identificador, out long usuarioId))
        {
            contexto.RejectPrincipal();
            return;
        }

        Usuario? usuario = await repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            false,
            contexto.HttpContext.RequestAborted);

        bool vigente = usuario is not null
            && usuario.Estado
            && usuario.Rol.Estado
            && string.Equals(
                principal.Identity?.Name,
                usuario.NombreUsuario,
                StringComparison.Ordinal)
            && principal.IsInRole(usuario.Rol.Nombre);

        vigente = vigente
            && string.Equals(
                principal.FindFirstValue(
                    PoliticasSeguridad.ReclamacionVersionCredencial),
                VersionCredencial.Calcular(usuario!.ContrasenaHash),
                StringComparison.Ordinal);

        if (!vigente)
        {
            contexto.RejectPrincipal();
        }
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> contexto)
    {
        contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(
        RedirectContext<CookieAuthenticationOptions> contexto)
    {
        contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
