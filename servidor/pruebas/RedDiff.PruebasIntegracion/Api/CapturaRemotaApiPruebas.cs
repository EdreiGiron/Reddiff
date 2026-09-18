using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Infraestructura.Persistencia;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class CapturaRemotaApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    private const string SecretoRemoto = "Clave-remota-solo-pruebas-2026";
    private const string UsuarioRemoto = "usuario-lectura";
    private const string Huella = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task Tecnico_PuedeCapturarPorSshSinExponerCredenciales()
    {
        long dispositivoId = await CrearDispositivoAsync(
            ProtocoloConexion.Ssh,
            "ssh-correcto",
            configurarAcceso: true);
        using HttpClient tecnico = await CrearClienteAutenticadoAsync(FabricaApiPruebas.Tecnico);

        using HttpResponseMessage respuesta = await CapturarAsync(tecnico, dispositivoId);
        string contenido = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.DoesNotContain(SecretoRemoto, contenido, StringComparison.Ordinal);
        Assert.DoesNotContain(UsuarioRemoto, contenido, StringComparison.Ordinal);
        using JsonDocument documento = JsonDocument.Parse(contenido);
        Assert.Equal(
            "Ssh",
            documento.RootElement.GetProperty("captura").GetProperty("medio").GetString());
        Assert.Equal(
            "Completada",
            documento.RootElement.GetProperty("captura").GetProperty("estado").GetString());
        Assert.Equal(
            "CapturaSsh",
            documento.RootElement.GetProperty("version").GetProperty("origen").GetString());

        using IServiceScope alcance = fabrica.Services.CreateScope();
        ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        Assert.Contains(
            contexto.Auditorias,
            auditoria => auditoria.Accion == "CapturarConfiguracionRemota"
                && auditoria.Estado == EstadoAuditoria.Exitoso
                && auditoria.Detalle != null
                && !auditoria.Detalle.Contains(SecretoRemoto, StringComparison.Ordinal));
    }

    [Fact]
    public async Task CapturaRemota_RequiereAccesoConfigurado()
    {
        long dispositivoId = await CrearDispositivoAsync(
            ProtocoloConexion.Ssh,
            "sin-acceso",
            configurarAcceso: false);
        using HttpClient tecnico = await CrearClienteAutenticadoAsync(FabricaApiPruebas.Tecnico);

        using HttpResponseMessage respuesta = await CapturarAsync(tecnico, dispositivoId);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        using IServiceScope alcance = fabrica.Services.CreateScope();
        ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        Assert.DoesNotContain(
            contexto.Capturas,
            captura => captura.DispositivoId == dispositivoId);
    }

    [Fact]
    public async Task CapturaRemota_RechazaIdentidadDeHostNoCoincidente()
    {
        long dispositivoId = await CrearDispositivoAsync(
            ProtocoloConexion.Ssh,
            "huella-no-coincide",
            configurarAcceso: true);
        using HttpClient tecnico = await CrearClienteAutenticadoAsync(FabricaApiPruebas.Tecnico);

        using HttpResponseMessage respuesta = await CapturarAsync(tecnico, dispositivoId);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("identidad criptográfica", await respuesta.Content.ReadAsStringAsync());
        ComprobarCapturaFallidaSinVersion(dispositivoId);
    }

    [Fact]
    public async Task CapturaRemota_EnmascaraContenidoSensibleAntesDeAlmacenarlo()
    {
        long dispositivoId = await CrearDispositivoAsync(
            ProtocoloConexion.Ssh,
            "contenido-sensible",
            configurarAcceso: true);
        using HttpClient tecnico = await CrearClienteAutenticadoAsync(FabricaApiPruebas.Tecnico);

        using HttpResponseMessage respuesta = await CapturarAsync(tecnico, dispositivoId);
        string contenido = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.DoesNotContain("clave-no-almacenable", contenido);

        using IServiceScope alcance = fabrica.Services.CreateScope();
        ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        var version = Assert.Single(
            contexto.VersionesConfiguracion,
            elemento => elemento.DispositivoId == dispositivoId);
        Assert.DoesNotContain("clave-no-almacenable", version.Contenido);
        Assert.Contains("[PROTEGIDO", version.Contenido);
        Assert.Contains(
            contexto.Capturas,
            captura => captura.DispositivoId == dispositivoId
                && captura.Estado == EstadoCaptura.Completada);
    }

    [Fact]
    public async Task CapturaRemota_ReportaConexionNoDisponibleSinCrearVersion()
    {
        long dispositivoId = await CrearDispositivoAsync(
            ProtocoloConexion.Ssh,
            "conexion-no-disponible",
            configurarAcceso: true);
        using HttpClient tecnico = await CrearClienteAutenticadoAsync(FabricaApiPruebas.Tecnico);

        using HttpResponseMessage respuesta = await CapturarAsync(tecnico, dispositivoId);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Contains("conexión segura", await respuesta.Content.ReadAsStringAsync());
        ComprobarCapturaFallidaSinVersion(dispositivoId);
    }

    [Fact]
    public async Task Tecnico_PuedeCapturarPorNetconfConElOrigenCorrecto()
    {
        long dispositivoId = await CrearDispositivoAsync(
            ProtocoloConexion.Netconf,
            "netconf-correcto",
            configurarAcceso: true);
        using HttpClient tecnico = await CrearClienteAutenticadoAsync(FabricaApiPruebas.Tecnico);

        using HttpResponseMessage respuesta = await CapturarAsync(tecnico, dispositivoId);
        using JsonDocument documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(
            "Netconf",
            documento.RootElement.GetProperty("captura").GetProperty("medio").GetString());
        Assert.Equal(
            "CapturaNetconf",
            documento.RootElement.GetProperty("version").GetProperty("origen").GetString());
    }

    private async Task<long> CrearDispositivoAsync(
        ProtocoloConexion protocolo,
        string prefijoHost,
        bool configurarAcceso)
    {
        using HttpClient administrador = await CrearClienteAutenticadoAsync(
            FabricaApiPruebas.Administrador);
        string sufijo = Guid.NewGuid().ToString("N")[..10];
        int puerto = protocolo == ProtocoloConexion.Ssh ? 22 : 830;
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(administrador);
        using HttpRequestMessage creacion = new(HttpMethod.Post, "/api/dispositivos")
        {
            Content = JsonContent.Create(new
            {
                nombre = $"remoto-{prefijoHost}-{sufijo}",
                host = $"{prefijoHost}-{sufijo}.ejemplo.local",
                tipo = "Router",
                modelo = "Cisco simulado",
                protocolo = protocolo.ToString(),
                puerto,
                fuenteEventos = (string?)null
            })
        };
        creacion.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage respuestaCreacion = await administrador.SendAsync(creacion);
        respuestaCreacion.EnsureSuccessStatusCode();
        using JsonDocument documento = JsonDocument.Parse(
            await respuestaCreacion.Content.ReadAsStringAsync());
        long dispositivoId = documento.RootElement.GetProperty("id").GetInt64();

        token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(administrador);
        using HttpRequestMessage autorizacion = new(
            HttpMethod.Patch,
            $"/api/dispositivos/{dispositivoId}/estado")
        {
            Content = JsonContent.Create(new { estado = "Autorizado" })
        };
        autorizacion.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage respuestaAutorizacion = await administrador.SendAsync(
            autorizacion);
        respuestaAutorizacion.EnsureSuccessStatusCode();

        if (!configurarAcceso)
        {
            return dispositivoId;
        }

        token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(administrador);
        using HttpRequestMessage configuracion = new(
            HttpMethod.Put,
            $"/api/dispositivos/{dispositivoId}/acceso-remoto")
        {
            Content = JsonContent.Create(new
            {
                usuarioAcceso = UsuarioRemoto,
                secretoAcceso = SecretoRemoto,
                algoritmoClaveHost = "ssh-ed25519",
                huellaClaveHost = Huella,
                huellaClaveHostConfirmada = true
            })
        };
        configuracion.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage respuestaConfiguracion = await administrador.SendAsync(
            configuracion);
        respuestaConfiguracion.EnsureSuccessStatusCode();

        return dispositivoId;
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

    private static async Task<HttpResponseMessage> CapturarAsync(
        HttpClient cliente,
        long dispositivoId)
    {
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/capturas/remota")
        {
            Content = JsonContent.Create(new { dispositivoId })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(solicitud);
    }

    private void ComprobarCapturaFallidaSinVersion(long dispositivoId)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        Assert.Contains(
            contexto.Capturas,
            captura => captura.DispositivoId == dispositivoId
                && captura.Estado == EstadoCaptura.Fallida);
        Assert.DoesNotContain(
            contexto.VersionesConfiguracion,
            version => version.DispositivoId == dispositivoId);
    }
}
