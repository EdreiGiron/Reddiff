using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedDiff.Api.Salud;
using RedDiff.Infraestructura;
using RedDiff.Infraestructura.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AgregarInfraestructura(builder.Configuration, builder.Environment);
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ContextoRedDiff>(
        "base-datos",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["preparacion"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
