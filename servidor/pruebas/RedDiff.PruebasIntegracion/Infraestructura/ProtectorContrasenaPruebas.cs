using Microsoft.Extensions.DependencyInjection;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.PruebasIntegracion.Api;

namespace RedDiff.PruebasIntegracion.Infraestructura;

public sealed class ProtectorContrasenaPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public void Hash_DebeUsarSalAleatoriaYVerificacionConstante()
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        IProtectorContrasena protector = alcance.ServiceProvider
            .GetRequiredService<IProtectorContrasena>();

        const string contrasena = "Clave-Unitaria-Segura-2026";
        string primero = protector.CrearHash(contrasena);
        string segundo = protector.CrearHash(contrasena);

        Assert.NotEqual(primero, segundo);
        Assert.DoesNotContain(contrasena, primero);
        Assert.True(protector.Verificar(contrasena, primero));
        Assert.False(protector.Verificar("Clave-Distinta-2026", primero));
        Assert.False(protector.Verificar(contrasena, "hash-invalido"));
    }
}
