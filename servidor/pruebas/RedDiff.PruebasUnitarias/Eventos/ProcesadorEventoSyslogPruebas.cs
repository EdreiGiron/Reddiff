using System.Text;
using RedDiff.Aplicacion.Eventos;

namespace RedDiff.PruebasUnitarias.Eventos;

public sealed class ProcesadorEventoSyslogPruebas
{
    private readonly ProcesadorEventoSyslog procesador = new();

    [Fact]
    public void Procesar_EventoValido_NormalizaYCalculaHuella()
    {
        byte[] datos = Encoding.UTF8.GetBytes(
            "<189>Sep 22 03:00:00 RouterReal %SYS-5-CONFIG_I: Configured from console\r\n");

        var resultado = procesador.Procesar(datos);

        Assert.True(resultado.Exitoso);
        Assert.Equal(64, resultado.Valor!.Huella.Length);
        Assert.DoesNotContain('\r', resultado.Valor.Contenido);
        Assert.Contains("%SYS-5-CONFIG_I", resultado.Valor.Contenido);
    }

    [Fact]
    public void Procesar_DatoSensible_EnmascaraAntesDeCalcularHuella()
    {
        byte[] datos = Encoding.UTF8.GetBytes(
            "<189>RouterReal token=secreto-no-almacenable cambio aplicado");

        var resultado = procesador.Procesar(datos);

        Assert.True(resultado.Exitoso);
        Assert.DoesNotContain("secreto-no-almacenable", resultado.Valor!.Contenido);
        Assert.Contains("token=[PROTEGIDO]", resultado.Valor.Contenido);
    }

    [Fact]
    public void Procesar_Utf8Invalido_RechazaDatagrama()
    {
        var resultado = procesador.Procesar(new byte[] { 0xC3, 0x28 });

        Assert.False(resultado.Exitoso);
        Assert.Contains("UTF-8", resultado.Error!.Mensaje);
    }

    [Fact]
    public void Procesar_ComandoCiscoSensible_OmiteEventoCompleto()
    {
        byte[] datos = Encoding.UTF8.GetBytes(
            "%PARSER-5-CFGLOG_LOGGEDCMD: Command:username prueba secret clave-visible");

        var resultado = procesador.Procesar(datos);

        Assert.True(resultado.Exitoso);
        Assert.DoesNotContain("clave-visible", resultado.Valor!.Contenido);
        Assert.Contains("contenido sensible", resultado.Valor.Contenido);
    }

    [Fact]
    public void Procesar_DatagramaGrande_RechazaContenido()
    {
        byte[] datos = new byte[ProcesadorEventoSyslog.TamanoMaximoBytes + 1];
        Array.Fill(datos, (byte)'a');

        var resultado = procesador.Procesar(datos);

        Assert.False(resultado.Exitoso);
        Assert.Contains("8 KB", resultado.Error!.Mensaje);
    }
}
