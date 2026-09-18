using System.Security.Cryptography;
using System.Text;
using RedDiff.Infraestructura.Red.Ssh;

namespace RedDiff.PruebasIntegracion.Infraestructura;

public sealed class ValidadorIdentidadHostSshPruebas
{
    private static readonly byte[] ClaveHost = Encoding.UTF8.GetBytes(
        "clave-publica-del-host-solo-para-pruebas");

    public static TheoryData<string, string, bool> Algoritmos => new()
    {
        { "ssh-ed25519", "ssh-ed25519", true },
        { "ssh-rsa", "ssh-rsa", true },
        { "ssh-rsa", "RSA-SHA2-512", true },
        { "ssh-rsa", "rsa-sha2-256", true },
        { "ssh-ed25519", "ssh-rsa", false }
    };

    [Fact]
    public void Conector_UsaComandoFijoParaObtenerLaVistaCompleta()
    {
        Assert.Equal(
            "terminal length 0",
            ConectorCapturaSsh.ComandoPreparacion);
        Assert.Equal(
            "show running-config view full",
            ConectorCapturaSsh.ComandoConsulta);
        Assert.Equal("\r", ConectorCapturaSsh.TerminadorComandoTerminal);
    }

    [Fact]
    public void ProcesadorSalida_ExtraeConfiguracionConPromptPrefijadoYControlTerminal()
    {
        const string respuesta =
            "RouterReal>show running-config view full\r\n"
            + "\u001b[2KBuilding configuration...\r\n\r\n"
            + "Current configuration : 180 bytes\r\n"
            + "!\r\nhostname RouterReal\r\n!\r\nend\r\nRouterReal>";

        string? configuracion = ProcesadorSalidaCiscoIos.ExtraerResultadoComando(
            respuesta,
            ConectorCapturaSsh.ComandoConsulta,
            "RouterReal>");

        Assert.NotNull(configuracion);
        Assert.StartsWith("Building configuration...", configuracion);
        Assert.EndsWith("end", configuracion);
        Assert.DoesNotContain("\u001b", configuracion, StringComparison.Ordinal);
        Assert.DoesNotContain("RouterReal>", configuracion);
    }

    [Fact]
    public void ProcesadorSalida_ExtraeConfiguracionDeUnaSesionIos()
    {
        const string respuesta = """
            show running-config view full
            Building configuration...

            Current configuration : 180 bytes
            !
            hostname RouterReal
            !
            end
            RouterReal>
            """;

        string? configuracion = ProcesadorSalidaCiscoIos.ExtraerResultadoComando(
            respuesta,
            ConectorCapturaSsh.ComandoConsulta,
            "RouterReal>");

        Assert.NotNull(configuracion);
        Assert.StartsWith("Building configuration...", configuracion);
        Assert.Contains("hostname RouterReal", configuracion);
        Assert.EndsWith("end", configuracion);
        Assert.DoesNotContain(ConectorCapturaSsh.ComandoConsulta, configuracion);
        Assert.DoesNotContain("RouterReal>", configuracion);
    }

    [Fact]
    public void ProcesadorSalida_RechazaAutocomandoInvalido()
    {
        const string respuesta =
            "Line has invalid autocommand \"show running-config view full\"";

        Assert.True(ProcesadorSalidaCiscoIos.ContieneErrorCli(respuesta));
    }

    [Theory]
    [MemberData(nameof(Algoritmos))]
    public void Coincide_ValidaAlgoritmoYHuella(
        string algoritmoEsperado,
        string algoritmoRecibido,
        bool esperado)
    {
        string huella = Convert.ToHexString(SHA256.HashData(ClaveHost)).ToLowerInvariant();

        bool resultado = ValidadorIdentidadHostSsh.Coincide(
            algoritmoEsperado,
            huella,
            algoritmoRecibido,
            ClaveHost);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void Coincide_RechazaUnaClaveDistinta()
    {
        string huella = Convert.ToHexString(SHA256.HashData(ClaveHost)).ToLowerInvariant();

        bool resultado = ValidadorIdentidadHostSsh.Coincide(
            "ssh-ed25519",
            huella,
            "ssh-ed25519",
            Encoding.UTF8.GetBytes("otra-clave-publica"));

        Assert.False(resultado);
    }
}
