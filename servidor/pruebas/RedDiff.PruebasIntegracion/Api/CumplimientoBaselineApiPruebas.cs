using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class CumplimientoBaselineApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task AdministradorCreaBaselineYTecnicoVerificaCumplimiento()
    {
        using HttpClient administrador = await CrearClienteAutenticadoAsync(
            FabricaApiPruebas.Administrador);
        long dispositivoId = await CrearDispositivoAsync(administrador, "Router");
        long versionId = await CargarVersionAsync(
            administrador,
            dispositivoId,
            "cumple.cfg",
            "hostname RouterSeguro\nno ip http server\ninterface FastEthernet0/0\n transport input ssh\n");
        long baselineId = await CrearBaselineDispositivoAsync(
            administrador,
            dispositivoId,
            versionId,
            [
                new { criterio = "Contiene", esperado = "no ip http server", obligatoria = true },
                new { criterio = "NoContiene", esperado = "transport input telnet", obligatoria = true },
                new
                {
                    criterio = "CoincideExpresionRegular",
                    esperado = "^interface FastEthernet0/0$",
                    obligatoria = false
                }
            ]);

        using HttpClient tecnico = await CrearClienteAutenticadoAsync(FabricaApiPruebas.Tecnico);
        using HttpResponseMessage verificacion = await VerificarAsync(
            tecnico,
            baselineId,
            versionId);

        Assert.Equal(HttpStatusCode.Created, verificacion.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(
            await verificacion.Content.ReadAsStringAsync());
        JsonElement raiz = documento.RootElement;
        Assert.Equal("Completado", raiz.GetProperty("estado").GetString());
        Assert.Equal("Cumple", raiz.GetProperty("resultadoGeneral").GetString());
        Assert.Equal(3, raiz.GetProperty("cumplidas").GetInt32());
        Assert.Equal(100m, raiz.GetProperty("porcentajeCumplimiento").GetDecimal());
        Assert.Equal(3, raiz.GetProperty("resultados").GetArrayLength());

        long verificacionId = raiz.GetProperty("id").GetInt64();
        using HttpResponseMessage detalle = await tecnico.GetAsync(
            $"/api/verificaciones/{verificacionId}");
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        Assert.Contains("no ip http server", await detalle.Content.ReadAsStringAsync());

        using HttpResponseMessage historial = await tecnico.GetAsync(
            $"/api/verificaciones?dispositivoId={dispositivoId}");
        Assert.Equal(HttpStatusCode.OK, historial.StatusCode);
        Assert.Contains(FabricaApiPruebas.Tecnico, await historial.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TecnicoNoPuedeCrearUnaBaseline()
    {
        using HttpClient tecnico = await CrearClienteAutenticadoAsync(FabricaApiPruebas.Tecnico);
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(tecnico);
        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/baselines")
        {
            Content = JsonContent.Create(new
            {
                nombre = "No autorizada",
                alcance = "TipoDispositivo",
                tipoDispositivo = "Router",
                reglas = new[]
                {
                    new { criterio = "Contiene", esperado = "hostname", obligatoria = true }
                }
            })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);

        using HttpResponseMessage respuesta = await tecnico.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task ReglaObligatoriaAusente_ProduceIncumplimiento()
    {
        using HttpClient administrador = await CrearClienteAutenticadoAsync(
            FabricaApiPruebas.Administrador);
        long dispositivoId = await CrearDispositivoAsync(administrador, "Router");
        long versionId = await CargarVersionAsync(
            administrador,
            dispositivoId,
            "incumple.cfg",
            "hostname RouterSinControl\nip http server\n");
        long baselineId = await CrearBaselineDispositivoAsync(
            administrador,
            dispositivoId,
            null,
            [new { criterio = "Contiene", esperado = "no ip http server", obligatoria = true }]);

        using HttpResponseMessage respuesta = await VerificarAsync(
            administrador,
            baselineId,
            versionId);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        string contenido = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("\"resultadoGeneral\":\"Incumple\"", contenido);
        Assert.Contains("\"incumplidas\":1", contenido);
    }

    [Fact]
    public async Task VerificacionRechazaVersionFueraDelAlcance()
    {
        using HttpClient administrador = await CrearClienteAutenticadoAsync(
            FabricaApiPruebas.Administrador);
        long primerDispositivoId = await CrearDispositivoAsync(administrador, "Router");
        long segundoDispositivoId = await CrearDispositivoAsync(administrador, "Router");
        long versionAjenaId = await CargarVersionAsync(
            administrador,
            segundoDispositivoId,
            "ajena.cfg",
            "hostname OtroRouter\n");
        long baselineId = await CrearBaselineDispositivoAsync(
            administrador,
            primerDispositivoId,
            null,
            [new { criterio = "Contiene", esperado = "hostname", obligatoria = true }]);

        using HttpResponseMessage respuesta = await VerificarAsync(
            administrador,
            baselineId,
            versionAjenaId);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("alcance", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BaselinePorTipoAceptaVersionDeOtroDispositivoDelMismoTipo()
    {
        using HttpClient administrador = await CrearClienteAutenticadoAsync(
            FabricaApiPruebas.Administrador);
        _ = await CrearDispositivoAsync(administrador, "Router");
        long dispositivoEvaluadoId = await CrearDispositivoAsync(administrador, "Router");
        long versionId = await CargarVersionAsync(
            administrador,
            dispositivoEvaluadoId,
            "router-tipo.cfg",
            "hostname RouterTipo\nip cef\n");
        long baselineId = await CrearBaselineTipoAsync(administrador, "Router");

        using HttpResponseMessage respuesta = await VerificarAsync(
            administrador,
            baselineId,
            versionId);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Contains("\"resultadoGeneral\":\"Cumple\"", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BaselineInactivaNoPuedeVerificarVersiones()
    {
        using HttpClient administrador = await CrearClienteAutenticadoAsync(
            FabricaApiPruebas.Administrador);
        long dispositivoId = await CrearDispositivoAsync(administrador, "Router");
        long versionId = await CargarVersionAsync(
            administrador,
            dispositivoId,
            "inactiva.cfg",
            "hostname Router\n");
        long baselineId = await CrearBaselineDispositivoAsync(
            administrador,
            dispositivoId,
            null,
            [new { criterio = "Contiene", esperado = "hostname", obligatoria = true }]);
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(administrador);
        using HttpRequestMessage cambioEstado = new(
            HttpMethod.Patch,
            $"/api/baselines/{baselineId}/estado")
        {
            Content = JsonContent.Create(new { activa = false })
        };
        cambioEstado.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage desactivacion = await administrador.SendAsync(cambioEstado);
        desactivacion.EnsureSuccessStatusCode();

        using HttpResponseMessage respuesta = await VerificarAsync(
            administrador,
            baselineId,
            versionId);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("inactiva", await respuesta.Content.ReadAsStringAsync());
    }

    private async Task<HttpClient> CrearClienteAutenticadoAsync(string nombreUsuario)
    {
        HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            nombreUsuario);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        return cliente;
    }

    private static async Task<long> CrearDispositivoAsync(HttpClient cliente, string tipo)
    {
        string sufijo = Guid.NewGuid().ToString("N")[..10];
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage creacion = new(HttpMethod.Post, "/api/dispositivos")
        {
            Content = JsonContent.Create(new
            {
                nombre = $"baseline-{sufijo}",
                host = $"baseline-{sufijo}.ejemplo.local",
                tipo,
                modelo = "Equipo de cumplimiento",
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

    private static async Task<long> CrearBaselineDispositivoAsync(
        HttpClient cliente,
        long dispositivoId,
        long? versionReferenciaId,
        object[] reglas)
    {
        return await CrearBaselineAsync(cliente, new
        {
            nombre = $"Baseline {Guid.NewGuid():N}",
            alcance = "Dispositivo",
            dispositivoId,
            tipoDispositivo = (string?)null,
            versionReferenciaId,
            reglas
        });
    }

    private static async Task<long> CrearBaselineTipoAsync(HttpClient cliente, string tipo)
    {
        return await CrearBaselineAsync(cliente, new
        {
            nombre = $"Baseline de {tipo} {Guid.NewGuid():N}",
            alcance = "TipoDispositivo",
            dispositivoId = (long?)null,
            tipoDispositivo = tipo,
            versionReferenciaId = (long?)null,
            reglas = new[]
            {
                new { criterio = "Contiene", esperado = "ip cef", obligatoria = true }
            }
        });
    }

    private static async Task<long> CrearBaselineAsync(HttpClient cliente, object cuerpo)
    {
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/baselines")
        {
            Content = JsonContent.Create(cuerpo)
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage respuesta = await cliente.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
        using JsonDocument documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());
        return documento.RootElement.GetProperty("id").GetInt64();
    }

    private static async Task<HttpResponseMessage> VerificarAsync(
        HttpClient cliente,
        long baselineId,
        long versionId)
    {
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/verificaciones")
        {
            Content = JsonContent.Create(new { baselineId, versionId })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(solicitud);
    }
}
