using Microsoft.Extensions.DependencyInjection;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.PruebasIntegracion.Api;

namespace RedDiff.PruebasIntegracion.Infraestructura;

public sealed class ProtectorSecretoDispositivoPruebas(FabricaApiPruebas fabrica)
    : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public void Secreto_DebeCifrarseConAleatoriedadYDetectarAlteraciones()
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        IProtectorSecretoDispositivo protector = alcance.ServiceProvider
            .GetRequiredService<IProtectorSecretoDispositivo>();

        const string secreto = "Clave-Dispositivo-Pruebas-2026";
        string primero = protector.Proteger(secreto);
        string segundo = protector.Proteger(secreto);

        Assert.NotEqual(primero, segundo);
        Assert.DoesNotContain(secreto, primero);
        Assert.True(protector.IntentarDesproteger(primero, out string recuperado));
        Assert.Equal(secreto, recuperado);

        char reemplazo = primero[^1] == 'A' ? 'B' : 'A';
        string alterado = primero[..^1] + reemplazo;

        Assert.False(protector.IntentarDesproteger(alterado, out string rechazado));
        Assert.Empty(rechazado);
        Assert.False(protector.IntentarDesproteger("no-es-base64", out _));
    }
}
