using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedDiff.Aplicacion.Capturas;
using RedDiff.Aplicacion.Comparaciones;
using RedDiff.Aplicacion.Seguridad.Autenticacion;
using RedDiff.Aplicacion.Seguridad.Inicializacion;
using RedDiff.Aplicacion.Seguridad.Usuarios;
using RedDiff.Aplicacion.Inventario.Dispositivos;
using RedDiff.Api.Seguridad;
using RedDiff.Api.Salud;
using RedDiff.Infraestructura;
using RedDiff.Infraestructura.Persistencia;

const string ArgumentoInicializacion = "--inicializar-administrador";
bool inicializarAdministrador = args.Contains(ArgumentoInicializacion, StringComparer.Ordinal);
string[] argumentosAplicacion = args
    .Where(argumento => !string.Equals(argumento, ArgumentoInicializacion, StringComparison.Ordinal))
    .ToArray();

var builder = WebApplication.CreateBuilder(argumentosAplicacion);

builder.Services.AddControllersWithViews(opciones =>
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddOpenApi();
builder.Services.AgregarInfraestructura(builder.Configuration, builder.Environment);
builder.Services.AgregarSeguridad(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ProcesadorArchivoConfiguracion>();
builder.Services.AddSingleton<ProcesadorComparacionConfiguraciones>();
builder.Services.AddSingleton(OpcionesCapturaRemota.Predeterminadas);
builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<ServicioCapturaRemota>();
builder.Services.AddScoped<ServicioCapturasConfiguracion>();
builder.Services.AddScoped<ServicioComparacionesConfiguracion>();
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

if (inicializarAdministrador)
{
    using IServiceScope alcance = app.Services.CreateScope();
    ServicioInicializacionIdentidad inicializador = alcance.ServiceProvider
        .GetRequiredService<ServicioInicializacionIdentidad>();

    string nombreUsuario = app.Configuration["Seguridad:AdministradorInicial:NombreUsuario"]
        ?? string.Empty;
    string contrasena = app.Configuration["Seguridad:AdministradorInicial:Contrasena"]
        ?? string.Empty;

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

app.UseHttpsRedirection();

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

app.Run();

public partial class Program
{
}
