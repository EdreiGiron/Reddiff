using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class GestionDispositivosApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task Administrador_PuedeCrearYListarDispositivo()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage creacion = await CrearDispositivoAsync(
            cliente,
            "nucleo-principal",
            "CORE-01.EJEMPLO.LOCAL",
            22,
            "Ssh",
            "Syslog");
        string contenidoCreacion = await creacion.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, creacion.StatusCode);
        Assert.Contains("core-01.ejemplo.local", contenidoCreacion);
        Assert.Contains("NoAutorizado", contenidoCreacion);

        using HttpResponseMessage listado = await cliente.GetAsync("/api/dispositivos");
        string contenidoListado = await listado.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        Assert.Contains("nucleo-principal", contenidoListado);
    }

    [Fact]
    public async Task Tecnico_PuedeConsultarPeroNoRegistrarDispositivos()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage listado = await cliente.GetAsync("/api/dispositivos");
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);

        using HttpResponseMessage creacion = await CrearDispositivoAsync(
            cliente,
            "acceso-tecnico",
            "10.20.0.2",
            830,
            "Netconf",
            null);
        Assert.Equal(HttpStatusCode.Forbidden, creacion.StatusCode);
    }

    [Fact]
    public async Task Administrador_NoPuedeUsarProtocoloFueraDelAlcance()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage creacion = await CrearDispositivoAsync(
            cliente,
            "protocolo-invalido",
            "10.20.0.3",
            23,
            "NoAdmitido",
            null);

        Assert.Equal(HttpStatusCode.BadRequest, creacion.StatusCode);
    }

    [Fact]
    public async Task Administrador_NoPuedeRepetirHostYPuerto()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage primera = await CrearDispositivoAsync(
            cliente,
            "borde-uno",
            "192.0.2.10",
            830,
            "Netconf",
            "SnmpInform");
        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);

        using HttpResponseMessage repetido = await CrearDispositivoAsync(
            cliente,
            "borde-dos",
            "192.0.2.10",
            830,
            "Netconf",
            "SnmpTrap");
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
    }

    [Fact]
    public async Task Administrador_PuedeAutorizarDispositivo()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage creacion = await CrearDispositivoAsync(
            cliente,
            "distribucion-uno",
            "198.51.100.15",
            22,
            "Ssh",
            "Syslog");
        creacion.EnsureSuccessStatusCode();
        using JsonDocument documento = JsonDocument.Parse(
            await creacion.Content.ReadAsStringAsync());
        long dispositivoId = documento.RootElement.GetProperty("id").GetInt64();

        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(
            HttpMethod.Patch,
            $"/api/dispositivos/{dispositivoId}/estado")
        {
            Content = JsonContent.Create(new { estado = "Autorizado" })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);

        using HttpResponseMessage respuesta = await cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using JsonDocument resultado = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("Autorizado", resultado.RootElement.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Administrador_PuedeActualizarDatosDeConexion()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage creacion = await CrearDispositivoAsync(
            cliente,
            "acceso-editable",
            "203.0.113.20",
            22,
            "Ssh",
            null);
        creacion.EnsureSuccessStatusCode();
        using JsonDocument documento = JsonDocument.Parse(
            await creacion.Content.ReadAsStringAsync());
        long dispositivoId = documento.RootElement.GetProperty("id").GetInt64();

        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(
            HttpMethod.Put,
            $"/api/dispositivos/{dispositivoId}")
        {
            Content = JsonContent.Create(new
            {
                nombre = "acceso-actualizado",
                host = "EDGE-20.EJEMPLO.LOCAL",
                tipo = "Switch",
                modelo = "Catalyst de prueba",
                protocolo = "Netconf",
                puerto = 830,
                fuenteEventos = "SnmpTrap"
            })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);

        using HttpResponseMessage respuesta = await cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using JsonDocument resultado = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(
            "edge-20.ejemplo.local",
            resultado.RootElement.GetProperty("host").GetString());
        Assert.Equal("Netconf", resultado.RootElement.GetProperty("protocolo").GetString());
    }

    private static async Task<HttpResponseMessage> CrearDispositivoAsync(
        HttpClient cliente,
        string nombre,
        string host,
        int puerto,
        string protocolo,
        string? fuenteEventos)
    {
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/dispositivos")
        {
            Content = JsonContent.Create(new
            {
                nombre,
                host,
                tipo = "Router",
                modelo = "Cisco de prueba",
                protocolo,
                puerto,
                fuenteEventos
            })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(solicitud);
    }
}
