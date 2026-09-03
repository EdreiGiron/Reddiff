using System.Net;
using System.Net.Http.Json;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class SaludApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task ComprobacionVida_DebeResponderSinConsultarDependencias()
    {
        using HttpClient cliente = fabrica.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage respuesta = await cliente.GetAsync("/salud/vivo");
        RespuestaSalud? contenido = await respuesta.Content.ReadFromJsonAsync<RespuestaSalud>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(contenido);
        Assert.Equal("saludable", contenido.Estado);
    }

    private sealed record RespuestaSalud(string Estado);
}
