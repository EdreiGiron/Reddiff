using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class CapturasConfiguracionApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task Tecnico_PuedeCargarArchivoYConsultarEvidencia()
    {
        long dispositivoId = await CrearDispositivoAutorizadoAsync();
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage carga = await CargarArchivoAsync(
            cliente,
            dispositivoId,
            "acceso.cfg",
            "hostname acceso\r\ninterface Gi0/1\r\n description enlace\r\n",
            "Evidencia de mantenimiento");
        Assert.Equal(HttpStatusCode.Created, carga.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(
            await carga.Content.ReadAsStringAsync());
        long versionId = documento.RootElement
            .GetProperty("version")
            .GetProperty("id")
            .GetInt64();
        Assert.Equal(
            "Completada",
            documento.RootElement.GetProperty("captura").GetProperty("estado").GetString());

        using HttpResponseMessage capturas = await cliente.GetAsync(
            $"/api/capturas?dispositivoId={dispositivoId}");
        string contenidoCapturas = await capturas.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, capturas.StatusCode);
        Assert.Contains("Archivo", contenidoCapturas);
        Assert.Contains(FabricaApiPruebas.Tecnico, contenidoCapturas);

        using HttpResponseMessage detalle = await cliente.GetAsync(
            $"/api/versiones-configuracion/{versionId}");
        string contenidoDetalle = await detalle.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        Assert.Contains("hostname acceso\\ninterface", contenidoDetalle);
        Assert.Contains("Evidencia de mantenimiento", contenidoDetalle);
        Assert.DoesNotContain("\r", contenidoDetalle);
    }

    [Fact]
    public async Task Usuario_NoPuedeCargarArchivoEnDispositivoSinAutorizar()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        long dispositivoId = await CrearDispositivoAsync(cliente, autorizar: false);

        using HttpResponseMessage carga = await CargarArchivoAsync(
            cliente,
            dispositivoId,
            "borde.cfg",
            "hostname borde\n",
            null);

        Assert.Equal(HttpStatusCode.Forbidden, carga.StatusCode);
    }

    [Fact]
    public async Task Carga_RechazaCredencialSinEnmascarar()
    {
        long dispositivoId = await CrearDispositivoAutorizadoAsync();
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage carga = await CargarArchivoAsync(
            cliente,
            dispositivoId,
            "sensible.conf",
            "hostname borde\nenable secret clave-real\n",
            null);

        Assert.Equal(HttpStatusCode.BadRequest, carga.StatusCode);
        Assert.Contains("enmascarar", await carga.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DosCargas_CreanVersionesConsecutivasSinSobrescribirLaPrimera()
    {
        long dispositivoId = await CrearDispositivoAutorizadoAsync();
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage primera = await CargarArchivoAsync(
            cliente,
            dispositivoId,
            "primera.cfg",
            "hostname primera\n",
            null);
        using HttpResponseMessage segunda = await CargarArchivoAsync(
            cliente,
            dispositivoId,
            "segunda.cfg",
            "hostname segunda\n",
            null);
        primera.EnsureSuccessStatusCode();
        segunda.EnsureSuccessStatusCode();

        using JsonDocument documentoPrimera = JsonDocument.Parse(
            await primera.Content.ReadAsStringAsync());
        long primeraId = documentoPrimera.RootElement
            .GetProperty("version")
            .GetProperty("id")
            .GetInt64();
        using JsonDocument documentoSegunda = JsonDocument.Parse(
            await segunda.Content.ReadAsStringAsync());
        Assert.Equal(
            2,
            documentoSegunda.RootElement.GetProperty("version").GetProperty("numero").GetInt32());

        using HttpResponseMessage detallePrimera = await cliente.GetAsync(
            $"/api/versiones-configuracion/{primeraId}");
        Assert.Equal(HttpStatusCode.OK, detallePrimera.StatusCode);
        Assert.Contains("hostname primera", await detallePrimera.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Carga_RechazaTipoDeArchivoNoPermitido()
    {
        long dispositivoId = await CrearDispositivoAutorizadoAsync();
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage carga = await CargarArchivoAsync(
            cliente,
            dispositivoId,
            "captura.bin",
            "hostname borde\n",
            null);

        Assert.Equal(HttpStatusCode.BadRequest, carga.StatusCode);
    }

    private async Task<long> CrearDispositivoAutorizadoAsync()
    {
        using HttpClient administrador = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            administrador,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        return await CrearDispositivoAsync(administrador, autorizar: true);
    }

    private static async Task<long> CrearDispositivoAsync(
        HttpClient cliente,
        bool autorizar)
    {
        string sufijo = Guid.NewGuid().ToString("N")[..10];
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage creacion = new(HttpMethod.Post, "/api/dispositivos")
        {
            Content = JsonContent.Create(new
            {
                nombre = $"equipo-{sufijo}",
                host = $"equipo-{sufijo}.ejemplo.local",
                tipo = "Router",
                modelo = "Equipo de prueba",
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

        if (!autorizar)
        {
            return dispositivoId;
        }

        token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage cambioEstado = new(
            HttpMethod.Patch,
            $"/api/dispositivos/{dispositivoId}/estado")
        {
            Content = JsonContent.Create(new { estado = "Autorizado" })
        };
        cambioEstado.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage respuestaEstado = await cliente.SendAsync(cambioEstado);
        respuestaEstado.EnsureSuccessStatusCode();
        return dispositivoId;
    }

    private static async Task<HttpResponseMessage> CargarArchivoAsync(
        HttpClient cliente,
        long dispositivoId,
        string nombreArchivo,
        string contenido,
        string? comentario)
    {
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        MultipartFormDataContent formulario = new();
        formulario.Add(new StringContent(dispositivoId.ToString()), "DispositivoId");
        formulario.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes(contenido)),
            "Archivo",
            nombreArchivo);
        if (comentario is not null)
        {
            formulario.Add(new StringContent(comentario), "Comentario");
        }

        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/capturas/archivo")
        {
            Content = formulario
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(solicitud);
    }
}
