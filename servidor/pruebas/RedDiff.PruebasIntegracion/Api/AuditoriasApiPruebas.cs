using System.Net;
using System.Text.Json;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class AuditoriasApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task Administrador_ObtieneCatalogoParaFiltrosGuiados()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage respuesta = await cliente.GetAsync(
            "/api/auditorias/catalogo-filtros");
        string contenido = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        using JsonDocument documento = JsonDocument.Parse(contenido);
        JsonElement raiz = documento.RootElement;
        Assert.NotEmpty(raiz.GetProperty("usuarios").EnumerateArray());
        Assert.NotEmpty(raiz.GetProperty("acciones").EnumerateArray());
        Assert.NotEmpty(raiz.GetProperty("entidades").EnumerateArray());
        Assert.DoesNotContain("contrasena", contenido, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Administrador_PuedeConsultarAuditoriaPaginada()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage respuesta = await cliente.GetAsync(
            "/api/auditorias?pagina=1&tamanoPagina=5");
        string contenido = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        using JsonDocument documento = JsonDocument.Parse(contenido);
        JsonElement raiz = documento.RootElement;
        Assert.Equal(1, raiz.GetProperty("pagina").GetInt32());
        Assert.Equal(5, raiz.GetProperty("tamanoPagina").GetInt32());
        Assert.True(raiz.GetProperty("totalRegistros").GetInt32() >= 1);
        Assert.NotEmpty(raiz.GetProperty("registros").EnumerateArray());
        Assert.DoesNotContain("contrasena", contenido, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contenidoConfiguracion", contenido, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tecnico_NoPuedeConsultarAuditoria()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/auditorias");
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task FiltrosInvalidos_DevuelvenProblemaDeValidacion()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage respuesta = await cliente.GetAsync(
            "/api/auditorias?estado=Desconocido&tamanoPagina=101");
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
