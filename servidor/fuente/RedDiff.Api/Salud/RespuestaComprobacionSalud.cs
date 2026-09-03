using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RedDiff.Api.Salud;

internal static class RespuestaComprobacionSalud
{
    public static Task EscribirAsync(HttpContext contexto, HealthReport informe)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(informe);

        RespuestaSalud respuesta = new(
            informe.Status == HealthStatus.Healthy
                ? "saludable"
                : "no_disponible");

        return contexto.Response.WriteAsJsonAsync(
            respuesta,
            cancellationToken: contexto.RequestAborted);
    }

    private sealed record RespuestaSalud(string Estado);
}
