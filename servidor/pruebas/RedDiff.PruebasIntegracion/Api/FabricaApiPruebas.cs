using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RedDiff.Aplicacion.Abstracciones.Red;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Infraestructura.Persistencia;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class FabricaApiPruebas : WebApplicationFactory<Program>
{
    public const string ContrasenaPruebas = "Clave-Integracion-2026";
    public const string Administrador = "administrador.pruebas";
    public const string Tecnico = "tecnico.pruebas";
    public const string Inactivo = "inactivo.pruebas";

    private readonly string nombreBaseDatos = $"reddiff-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment("Development");
        constructor.ConfigureAppConfiguration((_, configuracion) =>
        {
            Dictionary<string, string?> valores = new()
            {
                ["BaseDatos:Servidor"] = "127.0.0.1",
                ["BaseDatos:Puerto"] = "5432",
                ["BaseDatos:Nombre"] = "reddiff_pruebas",
                ["BaseDatos:Usuario"] = "reddiff_pruebas",
                ["BaseDatos:Contrasena"] = "secreto-temporal-de-prueba",
                ["BaseDatos:ModoSsl"] = "Disable",
                ["Seguridad:DuracionSesionMinutos"] = "30",
                ["Seguridad:LimiteIntentosInicioSesionPorMinuto"] = "100",
                ["Seguridad:OrigenCliente"] = "https://localhost"
            };

            configuracion.AddInMemoryCollection(valores);
        });

        constructor.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<DbContextOptions<ContextoRedDiff>>();
            servicios.RemoveAll<IDbContextOptionsConfiguration<ContextoRedDiff>>();
            servicios.RemoveAll<ContextoRedDiff>();
            servicios.AddDbContext<ContextoRedDiff>(opciones =>
                opciones.UseInMemoryDatabase(nombreBaseDatos));
            servicios.RemoveAll<IConectorCapturaRemota>();
            servicios.AddSingleton<IConectorCapturaRemota>(
                new ConectorCapturaRemotaSimulado(ProtocoloConexion.Ssh));
            servicios.AddSingleton<IConectorCapturaRemota>(
                new ConectorCapturaRemotaSimulado(ProtocoloConexion.Netconf));
        });
    }

    protected override IHost CreateHost(IHostBuilder constructor)
    {
        IHost host = base.CreateHost(constructor);

        using IServiceScope alcance = host.Services.CreateScope();
        ContextoRedDiff contexto = alcance.ServiceProvider.GetRequiredService<ContextoRedDiff>();
        contexto.Database.EnsureCreated();

        if (!contexto.Usuarios.Any())
        {
            Rol administrador = new("Administrador");
            Rol tecnico = new("Tecnico");
            contexto.Roles.AddRange(administrador, tecnico);
            contexto.SaveChanges();

            IProtectorContrasena protector = alcance.ServiceProvider
                .GetRequiredService<IProtectorContrasena>();
            string hash = protector.CrearHash(ContrasenaPruebas);

            Usuario usuarioAdministrador = new(administrador.Id, Administrador, hash);
            Usuario usuarioTecnico = new(tecnico.Id, Tecnico, hash);
            Usuario usuarioInactivo = new(tecnico.Id, Inactivo, hash);
            usuarioInactivo.Desactivar();
            contexto.Usuarios.AddRange(
                usuarioAdministrador,
                usuarioTecnico,
                usuarioInactivo);
            contexto.SaveChanges();
        }

        return host;
    }
}
