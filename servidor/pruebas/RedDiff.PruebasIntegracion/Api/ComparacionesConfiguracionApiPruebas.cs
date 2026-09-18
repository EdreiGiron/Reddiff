using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class ComparacionesConfiguracionApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task Tecnico_PuedeCompararDosVersionesDelMismoDispositivo()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        long dispositivoId = await CrearDispositivoAutorizadoAsync();
        long versionOrigenId = await CargarVersionAsync(
            cliente,
            dispositivoId,
            "origen.cfg",
            "hostname nucleo\ninterface Gi0/1\n description anterior\n shutdown\n");
        long versionDestinoId = await CargarVersionAsync(
            cliente,
            dispositivoId,
            "destino.cfg",
            "hostname nucleo\ninterface Gi0/1\n description nueva\n no shutdown\nip cef\n");

        using HttpResponseMessage comparacion = await CompararAsync(
            cliente,
            versionOrigenId,
            versionDestinoId);

        Assert.Equal(HttpStatusCode.Created, comparacion.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(
            await comparacion.Content.ReadAsStringAsync());
        JsonElement raiz = documento.RootElement;
        Assert.Equal("Completado", raiz.GetProperty("estado").GetString());
        Assert.Equal(1, raiz.GetProperty("agregadas").GetInt32());
        Assert.Equal(0, raiz.GetProperty("eliminadas").GetInt32());
        Assert.Equal(2, raiz.GetProperty("modificadas").GetInt32());
        Assert.Equal(3, raiz.GetProperty("diferencias").GetArrayLength());
        Assert.Contains(
            raiz.GetProperty("diferencias").EnumerateArray(),
            diferencia => diferencia.GetProperty("tipo").GetString() == "Agregada"
                && diferencia.GetProperty("textoNuevo").GetString() == "ip cef");

        long comparacionId = raiz.GetProperty("id").GetInt64();
        using HttpResponseMessage detalle = await cliente.GetAsync(
            $"/api/comparaciones/{comparacionId}");
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        Assert.Contains("description anterior", await detalle.Content.ReadAsStringAsync());

        using HttpResponseMessage historial = await cliente.GetAsync(
            $"/api/comparaciones?dispositivoId={dispositivoId}");
        Assert.Equal(HttpStatusCode.OK, historial.StatusCode);
        Assert.Contains(FabricaApiPruebas.Tecnico, await historial.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Comparacion_RechazaVersionesDeDispositivosDiferentes()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        long primerDispositivoId = await CrearDispositivoAsync(cliente);
        long segundoDispositivoId = await CrearDispositivoAsync(cliente);
        long primeraVersionId = await CargarVersionAsync(
            cliente,
            primerDispositivoId,
            "primera.cfg",
            "hostname primero\n");
        long segundaVersionId = await CargarVersionAsync(
            cliente,
            segundoDispositivoId,
            "segunda.cfg",
            "hostname segundo\n");

        using HttpResponseMessage comparacion = await CompararAsync(
            cliente,
            primeraVersionId,
            segundaVersionId);

        Assert.Equal(HttpStatusCode.BadRequest, comparacion.StatusCode);
        Assert.Contains("mismo dispositivo", await comparacion.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Comparacion_RechazaLaMismaVersionComoOrigenYDestino()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        long dispositivoId = await CrearDispositivoAutorizadoAsync();
        long versionId = await CargarVersionAsync(
            cliente,
            dispositivoId,
            "unica.cfg",
            "hostname unico\n");

        using HttpResponseMessage comparacion = await CompararAsync(
            cliente,
            versionId,
            versionId);

        Assert.Equal(HttpStatusCode.BadRequest, comparacion.StatusCode);
        Assert.Contains("diferentes", await comparacion.Content.ReadAsStringAsync());
    }

    private async Task<long> CrearDispositivoAutorizadoAsync()
    {
        using HttpClient administrador = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            administrador,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        return await CrearDispositivoAsync(administrador);
    }

    private static async Task<long> CrearDispositivoAsync(HttpClient cliente)
    {
        string sufijo = Guid.NewGuid().ToString("N")[..10];
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage creacion = new(HttpMethod.Post, "/api/dispositivos")
        {
            Content = JsonContent.Create(new
            {
                nombre = $"comparacion-{sufijo}",
                host = $"comparacion-{sufijo}.ejemplo.local",
                tipo = "Router",
                modelo = "Equipo de comparación",
                protocolo = "Ssh",
                puerto = 22,
                fuenteEventos = "Syslog"
            })
        };
        creacion.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage respuestaCreacion = await cliente.SendAsync(creacion);
        respuestaCreacion.EnsureSuccessStatusCode();
        using JsonDocument documento = JsonDocument.Parse(
            await respuestaCreacion.Content.ReadAsStringAsync());
        long dispositivoId = documento.RootElement.GetProperty("id").GetInt64();

        token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage autorizacion = new(
            HttpMethod.Patch,
            $"/api/dispositivos/{dispositivoId}/estado")
        {
            Content = JsonContent.Create(new { estado = "Autorizado" })
        };
        autorizacion.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage respuestaAutorizacion = await cliente.SendAsync(autorizacion);
        respuestaAutorizacion.EnsureSuccessStatusCode();
        return dispositivoId;
    }

    private static async Task<long> CargarVersionAsync(
        HttpClient cliente,
        long dispositivoId,
        string nombreArchivo,
        string contenido)
    {
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        MultipartFormDataContent formulario = new();
        formulario.Add(new StringContent(dispositivoId.ToString()), "DispositivoId");
        formulario.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes(contenido)),
            "Archivo",
            nombreArchivo);
        using HttpRequestMessage carga = new(HttpMethod.Post, "/api/capturas/archivo")
        {
            Content = formulario
        };
        carga.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage respuesta = await cliente.SendAsync(carga);
        respuesta.EnsureSuccessStatusCode();
        using JsonDocument documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        return documento.RootElement.GetProperty("version").GetProperty("id").GetInt64();
    }

    private static async Task<HttpResponseMessage> CompararAsync(
        HttpClient cliente,
        long versionOrigenId,
        long versionDestinoId)
    {
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/comparaciones")
        {
            Content = JsonContent.Create(new { versionOrigenId, versionDestinoId })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(solicitud);
    }
}
