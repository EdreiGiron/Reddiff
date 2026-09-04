using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class GestionUsuariosApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task Administrador_PuedeCrearYListarUsuarioSinExponerHash()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        long rolTecnicoId = await ObtenerRolTecnicoIdAsync(cliente);
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(new
            {
                nombreUsuario = "nuevo.tecnico",
                rolId = rolTecnicoId,
                contrasena = "Clave-Nueva-Segura-2026"
            })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);

        using HttpResponseMessage creacion = await cliente.SendAsync(solicitud);
        string contenidoCreacion = await creacion.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, creacion.StatusCode);
        Assert.Contains("nuevo.tecnico", contenidoCreacion);
        Assert.DoesNotContain("contrasena", contenidoCreacion, StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage listado = await cliente.GetAsync("/api/usuarios");
        string contenidoListado = await listado.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        Assert.Contains("nuevo.tecnico", contenidoListado);
        Assert.DoesNotContain("contrasena", contenidoListado, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Administrador_NoPuedeCrearNombreDuplicado()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        long rolTecnicoId = await ObtenerRolTecnicoIdAsync(cliente);
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(new
            {
                nombreUsuario = FabricaApiPruebas.Tecnico.ToUpperInvariant(),
                rolId = rolTecnicoId,
                contrasena = "Otra-Clave-Segura-2026"
            })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);

        using HttpResponseMessage respuesta = await cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task Administrador_NoPuedeDesactivarSuPropiaCuenta()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using JsonDocument sesion = JsonDocument.Parse(await inicio.Content.ReadAsStringAsync());
        long administradorId = sesion.RootElement.GetProperty("usuarioId").GetInt64();
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(
            HttpMethod.Patch,
            $"/api/usuarios/{administradorId}/estado")
        {
            Content = JsonContent.Create(new { activo = false })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);

        using HttpResponseMessage respuesta = await cliente.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task DesactivarUsuario_InvalidaSuSesionEnLaSiguienteSolicitud()
    {
        using HttpClient clienteTecnico = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicioTecnico = await ClienteApiSeguro.IniciarSesionAsync(
            clienteTecnico,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicioTecnico.StatusCode);

        using HttpClient clienteAdministrador = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicioAdministrador = await ClienteApiSeguro.IniciarSesionAsync(
            clienteAdministrador,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicioAdministrador.StatusCode);

        using HttpResponseMessage listado = await clienteAdministrador.GetAsync("/api/usuarios");
        listado.EnsureSuccessStatusCode();
        using JsonDocument usuarios = JsonDocument.Parse(
            await listado.Content.ReadAsStringAsync());
        long tecnicoId = usuarios.RootElement
            .EnumerateArray()
            .Single(elemento => elemento.GetProperty("nombreUsuario").GetString()
                == FabricaApiPruebas.Tecnico)
            .GetProperty("id")
            .GetInt64();

        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(clienteAdministrador);
        using HttpRequestMessage solicitud = new(
            HttpMethod.Patch,
            $"/api/usuarios/{tecnicoId}/estado")
        {
            Content = JsonContent.Create(new { activo = false })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage desactivacion = await clienteAdministrador.SendAsync(solicitud);
        Assert.Equal(HttpStatusCode.OK, desactivacion.StatusCode);

        using HttpResponseMessage sesionTecnico = await clienteTecnico.GetAsync(
            "/api/autenticacion/sesion");
        Assert.Equal(HttpStatusCode.Unauthorized, sesionTecnico.StatusCode);
    }

    private static async Task<long> ObtenerRolTecnicoIdAsync(HttpClient cliente)
    {
        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/roles");
        respuesta.EnsureSuccessStatusCode();
        using JsonDocument documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());

        return documento.RootElement
            .EnumerateArray()
            .Single(elemento => elemento.GetProperty("nombre").GetString() == "Tecnico")
            .GetProperty("id")
            .GetInt64();
    }
}
