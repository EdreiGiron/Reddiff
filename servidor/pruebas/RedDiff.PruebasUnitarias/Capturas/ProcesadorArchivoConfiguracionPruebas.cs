using System.Security.Cryptography;
using System.Text;
using RedDiff.Aplicacion.Capturas;

namespace RedDiff.PruebasUnitarias.Capturas;

public sealed class ProcesadorArchivoConfiguracionPruebas
{
    private readonly ProcesadorArchivoConfiguracion procesador = new();

    [Fact]
    public void Procesar_NormalizaFinDeLineaYCalculaHuellaDelContenidoGuardado()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("hostname borde\r\ninterface Gi0/1\r\n");

        var resultado = procesador.Procesar("borde.cfg", bytes, "Captura inicial");

        Assert.True(resultado.Exitoso);
        Assert.Equal("hostname borde\ninterface Gi0/1\n", resultado.Valor!.Contenido);
        string hashEsperado = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(resultado.Valor.Contenido)))
            .ToLowerInvariant();
        Assert.Equal(hashEsperado, resultado.Valor.Hash);
        Assert.Contains("borde.cfg", resultado.Valor.Comentario);
    }

    [Fact]
    public void Procesar_RechazaExtensionNoPermitida()
    {
        var resultado = procesador.Procesar(
            "captura.exe",
            Encoding.UTF8.GetBytes("hostname borde"),
            null);

        Assert.False(resultado.Exitoso);
        Assert.Contains("extensión", resultado.Error!.Mensaje);
    }

    [Fact]
    public void Procesar_RechazaUtf8Invalido()
    {
        var resultado = procesador.Procesar("captura.txt", [0xC3, 0x28], null);

        Assert.False(resultado.Exitoso);
        Assert.Contains("UTF-8", resultado.Error!.Mensaje);
    }

    [Theory]
    [InlineData("enable secret clave-real")]
    [InlineData("username operador privilege 15 secret clave-real")]
    [InlineData("snmp-server community comunidad-real RO")]
    public void Procesar_RechazaCredencialesSinEnmascarar(string linea)
    {
        var resultado = procesador.Procesar(
            "captura.conf",
            Encoding.UTF8.GetBytes(linea),
            null);

        Assert.False(resultado.Exitoso);
        Assert.Contains("enmascarar", resultado.Error!.Mensaje);
    }

    [Fact]
    public void Procesar_AceptaDatoSensibleEnmascarado()
    {
        var resultado = procesador.Procesar(
            "captura.config",
            Encoding.UTF8.GetBytes("snmp-server community [PROTEGIDO] RO"),
            null);

        Assert.True(resultado.Exitoso);
    }

    [Fact]
    public void ProcesarContenidoRemoto_NormalizaYCalculaHuella()
    {
        var resultado = procesador.ProcesarContenidoRemoto(
            "hostname remoto\r\ninterface Gi0/1\r\n");

        Assert.True(resultado.Exitoso);
        Assert.Equal("hostname remoto\ninterface Gi0/1\n", resultado.Valor!.Contenido);
        string hashEsperado = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(resultado.Valor.Contenido)))
            .ToLowerInvariant();
        Assert.Equal(hashEsperado, resultado.Valor.Hash);
    }

    [Theory]
    [InlineData("enable secret clave-visible", "clave-visible")]
    [InlineData(
        "username operador privilege 15 secret 9 hash-visible",
        "hash-visible")]
    [InlineData("snmp-server community comunidad-visible RO", "comunidad-visible")]
    [InlineData("radius-server key clave-radius", "clave-radius")]
    [InlineData("neighbor 192.0.2.1 password clave-bgp", "clave-bgp")]
    [InlineData("parser view REDDIFF\n secret 5 hash-vista", "hash-vista")]
    public void ProcesarContenidoRemoto_EnmascaraDatosSensibles(
        string contenido,
        string valorSensible)
    {
        var resultado = procesador.ProcesarContenidoRemoto(contenido);

        Assert.True(resultado.Exitoso);
        Assert.DoesNotContain(
            valorSensible,
            resultado.Valor!.Contenido,
            StringComparison.Ordinal);
        Assert.Contains("[PROTEGIDO", resultado.Valor.Contenido, StringComparison.Ordinal);
        string hashEsperado = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(resultado.Valor.Contenido)))
            .ToLowerInvariant();
        Assert.Equal(hashEsperado, resultado.Valor.Hash);
    }

    [Fact]
    public void ProcesarContenidoRemoto_EliminaBloqueCompletoDeClavePrivada()
    {
        const string contenido = """
            hostname laboratorio
            -----BEGIN RSA PRIVATE KEY-----
            material-privado-linea-1
            material-privado-linea-2
            -----END RSA PRIVATE KEY-----
            interface GigabitEthernet0/1
            """;

        var resultado = procesador.ProcesarContenidoRemoto(contenido);

        Assert.True(resultado.Exitoso);
        Assert.DoesNotContain("material-privado", resultado.Valor!.Contenido);
        Assert.Contains("bloque de clave privada omitido", resultado.Valor.Contenido);
        Assert.Contains("interface GigabitEthernet0/1", resultado.Valor.Contenido);
    }

    [Theory]
    [InlineData("Line has invalid autocommand \"show running-config view full\"")]
    [InlineData("% Invalid input detected at '^' marker.")]
    [InlineData("% Authorization failed.")]
    public void ProcesarContenidoRemoto_RechazaErroresCliComoConfiguracion(string respuesta)
    {
        var resultado = procesador.ProcesarContenidoRemoto(respuesta);

        Assert.False(resultado.Exitoso);
        Assert.Contains("devolvió un error", resultado.Error!.Mensaje);
    }
}
