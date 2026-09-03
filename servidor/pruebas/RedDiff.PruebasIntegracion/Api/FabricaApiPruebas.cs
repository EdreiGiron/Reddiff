using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RedDiff.PruebasIntegracion.Api;

public sealed class FabricaApiPruebas : WebApplicationFactory<Program>
{
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
                ["BaseDatos:ModoSsl"] = "Disable"
            };

            configuracion.AddInMemoryCollection(valores);
        });
    }
}
