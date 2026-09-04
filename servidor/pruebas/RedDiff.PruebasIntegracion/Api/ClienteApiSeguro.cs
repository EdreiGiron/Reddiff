using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RedDiff.PruebasIntegracion.Api;

internal static class ClienteApiSeguro
{
    public static async Task<string> ObtenerTokenCsrfAsync(HttpClient cliente)
    {
        ProteccionCsrf? contenido = await cliente.GetFromJsonAsync<ProteccionCsrf>(
            "/api/autenticacion/proteccion-csrf");

        Assert.NotNull(contenido);
        Assert.False(string.IsNullOrWhiteSpace(contenido.Token));
        return contenido.Token;
    }

    public static async Task<HttpResponseMessage> IniciarSesionAsync(
        HttpClient cliente,
        string nombreUsuario,
        string contrasena = FabricaApiPruebas.ContrasenaPruebas)
    {
        string token = await ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/autenticacion/iniciar-sesion")
        {
            Content = JsonContent.Create(new { nombreUsuario, contrasena })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(solicitud);
    }

    public static HttpClient CrearCliente(FabricaApiPruebas fabrica)
    {
        return fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
    }

    private sealed record ProteccionCsrf(string Token);
}
