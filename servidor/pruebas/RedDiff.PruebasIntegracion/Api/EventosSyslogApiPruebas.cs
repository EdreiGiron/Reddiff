using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.Aplicacion.Eventos;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Infraestructura.Persistencia;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class EventosSyslogApiPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    private const string HuellaHost =
        "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    [Fact]
    public async Task EventoSyslogAutorizado_GeneraUnaSolaCapturaYVersion()
    {
        string host = $"192.0.2.{Random.Shared.Next(10, 240)}";
        long dispositivoId = CrearDispositivoPreparado(host);
        byte[] datagrama = Encoding.UTF8.GetBytes(
            "<189>Sep 22 03:00:00 RouterReal %SYS-5-CONFIG_I: Configured from console");

        using (IServiceScope alcance = fabrica.Services.CreateScope())
        {
            ServicioEventosCambio servicio = alcance.ServiceProvider
                .GetRequiredService<ServicioEventosCambio>();

            var primera = await servicio.RecibirSyslogAsync(host, datagrama);
            var duplicada = await servicio.RecibirSyslogAsync(host, datagrama);

            Assert.True(primera.Exitoso);
            Assert.True(primera.Valor!.CapturaRealizada);
            Assert.True(primera.Valor.VersionGenerada);
            Assert.False(primera.Valor.Duplicado);
            Assert.True(duplicada.Exitoso);
            Assert.True(duplicada.Valor!.Duplicado);
        }

        using IServiceScope verificacion = fabrica.Services.CreateScope();
        ContextoRedDiff contexto = verificacion.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        Assert.Single(contexto.EventosCambio, evento => evento.DispositivoId == dispositivoId);
        Assert.Single(contexto.Capturas, captura => captura.DispositivoId == dispositivoId);
        Assert.Single(
            contexto.VersionesConfiguracion,
            version => version.DispositivoId == dispositivoId);
        Assert.Contains(
            contexto.EventosCambio,
            evento => evento.DispositivoId == dispositivoId
                && evento.Estado == EstadoEventoCambio.Procesado);
    }

    [Fact]
    public async Task EventosDistintosSinCambio_GeneranUnaSolaVersion()
    {
        string host = $"203.0.113.{Random.Shared.Next(10, 240)}";
        long dispositivoId = CrearDispositivoPreparado(host);

        using (IServiceScope alcance = fabrica.Services.CreateScope())
        {
            ServicioEventosCambio servicio = alcance.ServiceProvider
                .GetRequiredService<ServicioEventosCambio>();

            var primera = await servicio.RecibirSyslogAsync(
                host,
                Encoding.UTF8.GetBytes("<189>RouterReal cambio de prueba uno"));
            var segunda = await servicio.RecibirSyslogAsync(
                host,
                Encoding.UTF8.GetBytes("<189>RouterReal cambio de prueba dos"));

            Assert.True(primera.Exitoso);
            Assert.True(primera.Valor!.VersionGenerada);
            Assert.True(segunda.Exitoso);
            Assert.True(segunda.Valor!.CapturaRealizada);
            Assert.False(segunda.Valor.VersionGenerada);
            Assert.Equal("SinCambios", segunda.Valor.Evento.Estado);
        }

        using IServiceScope verificacion = fabrica.Services.CreateScope();
        ContextoRedDiff contexto = verificacion.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        Assert.Equal(
            2,
            contexto.EventosCambio.Count(evento => evento.DispositivoId == dispositivoId));
        Assert.Equal(
            2,
            contexto.Capturas.Count(captura => captura.DispositivoId == dispositivoId));
        Assert.Single(
            contexto.VersionesConfiguracion,
            version => version.DispositivoId == dispositivoId);
        Assert.Contains(
            contexto.EventosCambio,
            evento => evento.DispositivoId == dispositivoId
                && evento.Estado == EstadoEventoCambio.SinCambios);
    }

    [Fact]
    public async Task Tecnico_PuedeConsultarEventosSinContenidoCompleto()
    {
        string host = $"198.51.100.{Random.Shared.Next(10, 240)}";
        CrearDispositivoPreparado(host);
        using (IServiceScope alcance = fabrica.Services.CreateScope())
        {
            ServicioEventosCambio servicio = alcance.ServiceProvider
                .GetRequiredService<ServicioEventosCambio>();
            await servicio.RecibirSyslogAsync(
                host,
                Encoding.UTF8.GetBytes(
                    "<189>RouterReal %SYS-5-CONFIG_I: Configured from console token=oculto"));
        }

        using HttpClient tecnico = ClienteApiSeguro.CrearCliente(fabrica);
        using HttpResponseMessage inicio = await ClienteApiSeguro.IniciarSesionAsync(
            tecnico,
            FabricaApiPruebas.Tecnico);
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);

        using HttpResponseMessage respuesta = await tecnico.GetAsync("/api/eventos?limite=20");
        string contenido = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("oculto", contenido, StringComparison.Ordinal);
        using JsonDocument documento = JsonDocument.Parse(contenido);
        Assert.Contains(
            documento.RootElement.EnumerateArray(),
            evento => evento.GetProperty("fuente").GetString() == host
                && evento.GetProperty("tipo").GetString() == "Syslog"
                && evento.GetProperty("estado").GetString() == "Procesado");
    }

    [Fact]
    public async Task EventoDeHostDesconocido_SeRechazaSinCrearEvidencia()
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        ServicioEventosCambio servicio = alcance.ServiceProvider
            .GetRequiredService<ServicioEventosCambio>();

        var resultado = await servicio.RecibirSyslogAsync(
            "203.0.113.250",
            Encoding.UTF8.GetBytes("<189>evento no autorizado"));

        Assert.False(resultado.Exitoso);
        ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        Assert.DoesNotContain(
            contexto.EventosCambio,
            evento => evento.Fuente == "203.0.113.250");
    }

    private long CrearDispositivoPreparado(string host)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        IProtectorSecretoDispositivo protector = alcance.ServiceProvider
            .GetRequiredService<IProtectorSecretoDispositivo>();
        Dispositivo dispositivo = new(
            $"router-syslog-{Guid.NewGuid():N}",
            host,
            "Router",
            "Cisco simulado",
            ProtocoloConexion.Ssh,
            22,
            FuenteEvento.Syslog);
        dispositivo.Autorizar();
        dispositivo.ConfigurarAccesoRemoto(
            "lector-eventos",
            protector.Proteger("secreto-de-prueba"),
            "ssh-ed25519",
            HuellaHost,
            DateTimeOffset.UtcNow);
        contexto.Dispositivos.Add(dispositivo);
        contexto.SaveChanges();
        return dispositivo.Id;
    }
}
