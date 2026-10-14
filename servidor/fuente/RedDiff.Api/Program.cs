using System.Runtime.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedDiff.Aplicacion.Capturas;
using RedDiff.Aplicacion.Auditorias;
using RedDiff.Aplicacion.Comparaciones;
using RedDiff.Aplicacion.Cumplimiento;
using RedDiff.Aplicacion.Seguridad.Autenticacion;
using RedDiff.Aplicacion.Seguridad.Inicializacion;
using RedDiff.Aplicacion.Seguridad.Usuarios;
using RedDiff.Aplicacion.Inventario.Dispositivos;
using RedDiff.Aplicacion.Eventos;
using RedDiff.Api.Seguridad;
using RedDiff.Api.Salud;
using RedDiff.Infraestructura;
using RedDiff.Infraestructura.Persistencia;
using Microsoft.Extensions.Logging.EventLog;

const string ArgumentoInicializacion = "--inicializar-administrador";
const string ArgumentoMigracion = "--migrar-base-datos";
bool inicializarAdministrador = args.Contains(ArgumentoInicializacion, StringComparer.Ordinal);
bool migrarBaseDatos = args.Contains(ArgumentoMigracion, StringComparer.Ordinal);
string[] argumentosAplicacion = args
    .Where(argumento => !string.Equals(argumento, ArgumentoInicializacion, StringComparison.Ordinal)
        && !string.Equals(argumento, ArgumentoMigracion, StringComparison.Ordinal))
    .ToArray();

if (inicializarAdministrador && migrarBaseDatos)
{
    Console.Error.WriteLine("Solo puede ejecutar una operación administrativa a la vez.");
    Environment.ExitCode = 1;
    return;
}

var builder = WebApplication.CreateBuilder(argumentosAplicacion);

if (OperatingSystem.IsWindows())
{
    AgregarRegistroEventosWindows(builder.Logging);
}

builder.Services.AddControllersWithViews(opciones =>
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddOpenApi();
builder.Services.AgregarInfraestructura(builder.Configuration, builder.Environment);
builder.Services.AgregarSeguridad(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ProcesadorArchivoConfiguracion>();
builder.Services.AddSingleton<ProcesadorComparacionConfiguraciones>();
builder.Services.AddSingleton<ProcesadorEventoSyslog>();
builder.Services.AddSingleton<EvaluadorReglasBaseline>();
builder.Services.AddSingleton(OpcionesCapturaRemota.Predeterminadas);
builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<ServicioAuditorias>();
builder.Services.AddScoped<ServicioCapturaRemota>();
builder.Services.AddScoped<ServicioCapturasConfiguracion>();
builder.Services.AddScoped<ServicioEventosCambio>();
builder.Services.AddScoped<ServicioComparacionesConfiguracion>();
builder.Services.AddScoped<ServicioBaselines>();
builder.Services.AddScoped<ServicioVerificacionesConfiguracion>();
builder.Services.AddScoped<ServicioGestionDispositivos>();
builder.Services.AddScoped<ServicioGestionUsuarios>();
builder.Services.AddScoped<ServicioInicializacionIdentidad>();
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ContextoRedDiff>(
        "base-datos",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["preparacion"]);

var app = builder.Build();

if (migrarBaseDatos)
{
    using IServiceScope alcance = app.Services.CreateScope();
    ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
    await contexto.Database.MigrateAsync();
    Console.WriteLine("Las migraciones pendientes fueron aplicadas correctamente.");
    return;
}

if (inicializarAdministrador)
{
    using IServiceScope alcance = app.Services.CreateScope();
    ServicioInicializacionIdentidad inicializador = alcance.ServiceProvider
        .GetRequiredService<ServicioInicializacionIdentidad>();

    string nombreUsuario = app.Configuration["Seguridad:AdministradorInicial:NombreUsuario"]
        ?? string.Empty;
    string contrasena = ObtenerSecreto(
        app.Configuration["Seguridad:AdministradorInicial:Contrasena"],
        app.Configuration["Seguridad:AdministradorInicial:ArchivoContrasena"]);

    var resultado = await inicializador.InicializarAsync(nombreUsuario, contrasena);
    if (!resultado.Exitoso)
    {
        Console.Error.WriteLine(resultado.Error!.Mensaje);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine(
        $"Identidad inicializada para el usuario {resultado.Valor!.NombreUsuario}.");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string rutaIndiceCliente = Path.Combine(
    app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"),
    "index.html");
bool clienteWebIncorporado = File.Exists(rutaIndiceCliente);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.Use(async (contexto, siguiente) =>
    {
        contexto.Response.Headers["X-Content-Type-Options"] = "nosniff";
        contexto.Response.Headers["X-Frame-Options"] = "DENY";
        contexto.Response.Headers["Referrer-Policy"] = "no-referrer";
        contexto.Response.Headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=()";
        contexto.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; "
            + "form-action 'self'; connect-src 'self'; img-src 'self' data:; "
            + "font-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self'";
        await siguiente();
    });
}

if (app.Configuration.GetValue<bool>("Seguridad:ConfiarProxyInverso"))
{
    ForwardedHeadersOptions opcionesEncabezadosReenviados = new()
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    };
    opcionesEncabezadosReenviados.KnownIPNetworks.Clear();
    opcionesEncabezadosReenviados.KnownProxies.Clear();
    app.UseForwardedHeaders(opcionesEncabezadosReenviados);
}

app.UseHttpsRedirection();

if (clienteWebIncorporado)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseRouting();
app.UseCors(PoliticasSeguridad.CorsCliente);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/salud/vivo", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = RespuestaComprobacionSalud.EscribirAsync
});

app.MapHealthChecks("/salud/listo", new HealthCheckOptions
{
    Predicate = comprobacion => comprobacion.Tags.Contains("preparacion"),
    ResponseWriter = RespuestaComprobacionSalud.EscribirAsync
});

if (clienteWebIncorporado)
{
    app.MapFallback(async contexto =>
    {
        if (contexto.Request.Path.StartsWithSegments("/api")
            || contexto.Request.Path.StartsWithSegments("/salud"))
        {
            contexto.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        contexto.Response.ContentType = "text/html; charset=utf-8";
        await contexto.Response.SendFileAsync(rutaIndiceCliente);
    });
}

app.Run();

[SupportedOSPlatform("windows")]
static void AgregarRegistroEventosWindows(ILoggingBuilder registro)
{
    registro.AddEventLog(ConfigurarRegistroEventosWindows);
}

static void ConfigurarRegistroEventosWindows(EventLogSettings configuracion)
{
    if (OperatingSystem.IsWindows())
    {
        configuracion.SourceName = "RedDiff";
    }
}

static string ObtenerSecreto(string? valor, string? rutaArchivo)
{
    if (!string.IsNullOrWhiteSpace(valor))
    {
        return valor;
    }

    if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo))
    {
        return string.Empty;
    }

    return File.ReadAllText(rutaArchivo).TrimEnd('\r', '\n');
}

public partial class Program
{
}
