using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Infraestructura.Persistencia;

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

    [Fact]
    public async Task Administrador_PuedeConfigurarAccesoSinExponerElSecreto()
    {
        const string nombre = "acceso-seguro-configurado";
        const string secreto = "Clave-remota-de-prueba-2026";
        const string huella = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        long dispositivoId = await CrearYAutorizarDispositivoAsync(
            cliente,
            nombre,
            "192.0.2.41");
        using HttpResponseMessage configuracion = await ConfigurarAccesoRemotoAsync(
            cliente,
            dispositivoId,
            secreto,
            huella,
            true);
        string contenido = await configuracion.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, configuracion.StatusCode);
        Assert.DoesNotContain(secreto, contenido, StringComparison.Ordinal);
        Assert.DoesNotContain("secretoAcceso", contenido, StringComparison.OrdinalIgnoreCase);
        using JsonDocument documento = JsonDocument.Parse(contenido);
        Assert.True(documento.RootElement.GetProperty("configurado").GetBoolean());
        Assert.Equal(
            "usuario-lectura",
            documento.RootElement.GetProperty("usuarioAcceso").GetString());
        Assert.Equal(
            huella,
            documento.RootElement.GetProperty("huellaClaveHost").GetString());

        using HttpResponseMessage consulta = await cliente.GetAsync(
            $"/api/dispositivos/{dispositivoId}/acceso-remoto");
        string contenidoConsulta = await consulta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.DoesNotContain(secreto, contenidoConsulta, StringComparison.Ordinal);
        Assert.DoesNotContain("secretoAcceso", contenidoConsulta, StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage listado = await cliente.GetAsync("/api/dispositivos");
        string contenidoListado = await listado.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        Assert.Contains("\"accesoRemotoConfigurado\":true", contenidoListado, StringComparison.Ordinal);

        using IServiceScope alcance = fabrica.Services.CreateScope();
        ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        Dispositivo dispositivo = contexto.Dispositivos.Single(
            candidato => candidato.Id == dispositivoId);
        Assert.NotEqual(secreto, dispositivo.SecretoAccesoProtegido);
        Assert.DoesNotContain(dispositivo.SecretoAccesoProtegido!, contenido, StringComparison.Ordinal);

        IProtectorSecretoDispositivo protector = alcance.ServiceProvider
            .GetRequiredService<IProtectorSecretoDispositivo>();
        Assert.True(protector.IntentarDesproteger(
            dispositivo.SecretoAccesoProtegido,
            out string secretoRecuperado));
        Assert.Equal(secreto, secretoRecuperado);
        Assert.Contains(
            contexto.Auditorias,
            auditoria => auditoria.Accion == "ConfigurarAccesoRemoto"
                && auditoria.EntidadId == dispositivoId
                && auditoria.Detalle != null
                && !auditoria.Detalle.Contains(secreto, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Administrador_PuedeReemplazarYRevocarAccesoRemoto()
    {
        const string primeraClave = "Clave-remota-inicial-2026";
        const string segundaClave = "Clave-remota-renovada-2026";
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        long dispositivoId = await CrearYAutorizarDispositivoAsync(
            cliente,
            "acceso-seguro-reemplazado",
            "192.0.2.42");
        using HttpResponseMessage primeraConfiguracion = await ConfigurarAccesoRemotoAsync(
            cliente,
            dispositivoId,
            primeraClave,
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            true);
        primeraConfiguracion.EnsureSuccessStatusCode();

        string primerSecretoProtegido;
        using (IServiceScope alcance = fabrica.Services.CreateScope())
        {
            ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
            primerSecretoProtegido = contexto.Dispositivos
                .Single(dispositivo => dispositivo.Id == dispositivoId)
                .SecretoAccesoProtegido!;
        }

        using HttpResponseMessage reemplazo = await ConfigurarAccesoRemotoAsync(
            cliente,
            dispositivoId,
            segundaClave,
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            true);
        string contenidoReemplazo = await reemplazo.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, reemplazo.StatusCode);
        Assert.DoesNotContain(segundaClave, contenidoReemplazo, StringComparison.Ordinal);

        using (IServiceScope alcance = fabrica.Services.CreateScope())
        {
            ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
            string segundoSecretoProtegido = contexto.Dispositivos
                .Single(dispositivo => dispositivo.Id == dispositivoId)
                .SecretoAccesoProtegido!;
            Assert.NotEqual(primerSecretoProtegido, segundoSecretoProtegido);

            IProtectorSecretoDispositivo protector = alcance.ServiceProvider
                .GetRequiredService<IProtectorSecretoDispositivo>();
            Assert.True(protector.IntentarDesproteger(
                segundoSecretoProtegido,
                out string secretoRecuperado));
            Assert.Equal(segundaClave, secretoRecuperado);
        }

        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitudRevocacion = new(
            HttpMethod.Delete,
            $"/api/dispositivos/{dispositivoId}/acceso-remoto");
        solicitudRevocacion.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage revocacion = await cliente.SendAsync(solicitudRevocacion);
        Assert.Equal(HttpStatusCode.OK, revocacion.StatusCode);
        using JsonDocument resultado = JsonDocument.Parse(
            await revocacion.Content.ReadAsStringAsync());
        Assert.False(resultado.RootElement.GetProperty("configurado").GetBoolean());

        using IServiceScope alcanceFinal = fabrica.Services.CreateScope();
        ContextoRedDiff contextoFinal = alcanceFinal.ServiceProvider
            .GetRequiredService<ContextoRedDiff>();
        Dispositivo dispositivoFinal = contextoFinal.Dispositivos.Single(
            dispositivo => dispositivo.Id == dispositivoId);
        Assert.Null(dispositivoFinal.UsuarioAcceso);
        Assert.Null(dispositivoFinal.SecretoAccesoProtegido);
        Assert.Null(dispositivoFinal.AlgoritmoClaveHost);
        Assert.Null(dispositivoFinal.HuellaClaveHost);
        Assert.Null(dispositivoFinal.AccesoConfiguradoEn);
        Assert.Contains(
            contextoFinal.Auditorias,
            auditoria => auditoria.Accion == "ReemplazarAccesoRemoto"
                && auditoria.EntidadId == dispositivoId);
        Assert.Contains(
            contextoFinal.Auditorias,
            auditoria => auditoria.Accion == "RevocarAccesoRemoto"
                && auditoria.EntidadId == dispositivoId);
    }

    [Fact]
    public async Task AccesoRemoto_RequiereDispositivoAutorizadoYHuellaConfirmada()
    {
        using HttpClient cliente = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            cliente,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage creacion = await CrearDispositivoAsync(
            cliente,
            "acceso-sin-autorizacion",
            "192.0.2.43",
            22,
            "Ssh",
            null);
        creacion.EnsureSuccessStatusCode();
        using JsonDocument documento = JsonDocument.Parse(
            await creacion.Content.ReadAsStringAsync());
        long dispositivoId = documento.RootElement.GetProperty("id").GetInt64();

        using HttpResponseMessage noAutorizado = await ConfigurarAccesoRemotoAsync(
            cliente,
            dispositivoId,
            "Clave-no-autorizada",
            "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
            true);
        Assert.Equal(HttpStatusCode.Conflict, noAutorizado.StatusCode);

        await AutorizarDispositivoAsync(cliente, dispositivoId);
        using HttpResponseMessage sinConfirmacion = await ConfigurarAccesoRemotoAsync(
            cliente,
            dispositivoId,
            "Clave-sin-confirmacion",
            "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
            false);
        Assert.Equal(HttpStatusCode.BadRequest, sinConfirmacion.StatusCode);
    }

    [Fact]
    public async Task Tecnico_NoPuedeConsultarNiModificarAccesoRemoto()
    {
        using HttpClient administrador = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicioAdministrador = await ClienteApiSeguro.IniciarSesionAsync(
            administrador,
            FabricaApiPruebas.Administrador);
        Assert.Equal(HttpStatusCode.OK, inicioAdministrador.StatusCode);
        long dispositivoId = await CrearYAutorizarDispositivoAsync(
            administrador,
            "acceso-restringido",
            "192.0.2.44");

        using HttpClient tecnico = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicioTecnico = await ClienteApiSeguro.IniciarSesionAsync(
            tecnico,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicioTecnico.StatusCode);

        using HttpResponseMessage consulta = await tecnico.GetAsync(
            $"/api/dispositivos/{dispositivoId}/acceso-remoto");
        Assert.Equal(HttpStatusCode.Forbidden, consulta.StatusCode);

        using HttpResponseMessage configuracion = await ConfigurarAccesoRemotoAsync(
            tecnico,
            dispositivoId,
            "Clave-tecnico-no-permitida",
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            true);
        Assert.Equal(HttpStatusCode.Forbidden, configuracion.StatusCode);

        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(tecnico);
        using HttpRequestMessage solicitudRevocacion = new(
            HttpMethod.Delete,
            $"/api/dispositivos/{dispositivoId}/acceso-remoto");
        solicitudRevocacion.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage revocacion = await tecnico.SendAsync(solicitudRevocacion);
        Assert.Equal(HttpStatusCode.Forbidden, revocacion.StatusCode);
    }

    private static async Task<long> CrearYAutorizarDispositivoAsync(
        HttpClient cliente,
        string nombre,
        string host)
    {
        using HttpResponseMessage creacion = await CrearDispositivoAsync(
            cliente,
            nombre,
            host,
            22,
            "Ssh",
            null);
        creacion.EnsureSuccessStatusCode();
        using JsonDocument documento = JsonDocument.Parse(
            await creacion.Content.ReadAsStringAsync());
        long dispositivoId = documento.RootElement.GetProperty("id").GetInt64();
        await AutorizarDispositivoAsync(cliente, dispositivoId);
        return dispositivoId;
    }

    private static async Task AutorizarDispositivoAsync(
        HttpClient cliente,
        long dispositivoId)
    {
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(
            HttpMethod.Patch,
            $"/api/dispositivos/{dispositivoId}/estado")
        {
            Content = JsonContent.Create(new { estado = "Autorizado" })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        using HttpResponseMessage respuesta = await cliente.SendAsync(solicitud);
        respuesta.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> ConfigurarAccesoRemotoAsync(
        HttpClient cliente,
        long dispositivoId,
        string secreto,
        string huella,
        bool confirmarHuella)
    {
        string token = await ClienteApiSeguro.ObtenerTokenCsrfAsync(cliente);
        using HttpRequestMessage solicitud = new(
            HttpMethod.Put,
            $"/api/dispositivos/{dispositivoId}/acceso-remoto")
        {
            Content = JsonContent.Create(new
            {
                usuarioAcceso = "usuario-lectura",
                secretoAcceso = secreto,
                algoritmoClaveHost = "ssh-ed25519",
                huellaClaveHost = huella,
                huellaClaveHostConfirmada = confirmarHuella
            })
        };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(solicitud);
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
