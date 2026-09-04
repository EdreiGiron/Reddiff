using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Infraestructura.Persistencia;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class AutenticacionApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task IniciarSesion_SinTokenCsrf_DebeRechazarSolicitud()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage respuesta = await cliente.PostAsJsonAsync(
            "/api/autenticacion/iniciar-sesion",
            new
            {
                nombreUsuario = FabricaApiPruebas.Administrador,
                contrasena = FabricaApiPruebas.ContrasenaPruebas
            });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task IniciarSesion_Valida_CreaCookieProtegidaYExponeSesion()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage respuesta = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        string cookies = string.Join(';', respuesta.Headers.GetValues("Set-Cookie"));
        Assert.Contains("RedDiff.Sesion=", cookies);
        Assert.Contains("httponly", cookies, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookies, StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage respuestaSesion = await cliente.GetAsync(
            "/api/autenticacion/sesion");
        string contenido = await respuestaSesion.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuestaSesion.StatusCode);
        Assert.Contains(FabricaApiPruebas.Administrador, contenido);
        Assert.Contains("Administrador", contenido);
    }

    [Theory]
    [InlineData("usuario.inexistente")]
    [InlineData(FabricaApiPruebas.Inactivo)]
    public async Task IniciarSesion_CredencialesNoPermitidas_DebeResponderIgual(
        string nombreUsuario)
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage respuesta = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            nombreUsuario);

        string contenido = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Contains("credenciales_invalidas", contenido);
        Assert.DoesNotContain("inactivo", contenido, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inexistente", contenido, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sesion_SinAutenticacion_DebeResponderNoAutorizado()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/autenticacion/sesion");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Tecnico_NoPuedeAdministrarUsuarios()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/usuarios");
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task IntentoFallido_DebeQuedarAuditadoSinCredencial()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage respuesta = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            "usuario.inexistente",
            "Clave-Erronea-2026");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);

        using IServiceScope alcance = fabrica.Services.CreateScope();
        ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        var auditoria = contexto.Auditorias.OrderByDescending(elemento => elemento.Id).First();

        Assert.Equal(EstadoAuditoria.Fallido, auditoria.Estado);
        Assert.Equal("AutenticarUsuario", auditoria.Accion);
        Assert.DoesNotContain("Clave-Erronea-2026", auditoria.Detalle ?? string.Empty);
    }
}
