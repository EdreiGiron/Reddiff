using System.Xml;
using RedDiff.Infraestructura.Red.Netconf;

namespace RedDiff.PruebasIntegracion.Infraestructura;

public sealed class ConectorCapturaNetconfPruebas
{
    [Fact]
    public void SolicitudFija_ConsultaRunningSinOperacionesDeEscritura()
    {
        string solicitud = ConectorCapturaNetconf.SolicitudConfiguracionEnEjecucion;

        Assert.Contains("<get-config>", solicitud, StringComparison.Ordinal);
        Assert.Contains("<running/>", solicitud, StringComparison.Ordinal);
        Assert.DoesNotContain("edit-config", solicitud, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("copy-config", solicitud, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delete-config", solicitud, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("commit", solicitud, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("urn:ietf:params:netconf:base:1.0")]
    [InlineData("urn:ietf:params:netconf:base:1.1")]
    [InlineData("urn:ietf:params:netconf:base:1.1?module=ietf-netconf")]
    public void ServidorAdmiteNetconfBase_AceptaVersionesEstandar(string capacidad)
    {
        XmlDocument capacidades = CargarXml(
            $"<hello><capabilities><capability>{capacidad}</capability></capabilities></hello>");

        bool resultado = ConectorCapturaNetconf.ServidorAdmiteNetconfBase(capacidades);

        Assert.True(resultado);
    }

    [Fact]
    public void ServidorAdmiteNetconfBase_RechazaServidorSinCapacidadBase()
    {
        XmlDocument capacidades = CargarXml(
            """
            <hello><capabilities><capability>
              urn:ietf:params:netconf:capability:writable-running:1.0
            </capability></capabilities></hello>
            """);

        bool resultado = ConectorCapturaNetconf.ServidorAdmiteNetconfBase(capacidades);

        Assert.False(resultado);
    }

    [Fact]
    public void Procesador_ExtraeDataYEnmascaraElementosYAtributosSensibles()
    {
        XmlDocument respuesta = CargarXml(
            """
            <rpc-reply xmlns="urn:ietf:params:xml:ns:netconf:base:1.0" message-id="1">
              <data>
                <native xmlns="http://cisco.com/ns/yang/Cisco-IOS-XE-native">
                  <hostname>router-principal</hostname>
                  <username>
                    <name>reddiff</name>
                    <secret>SecretoVisible</secret>
                  </username>
                  <snmp community="publica" />
                </native>
              </data>
            </rpc-reply>
            """);

        bool resultado = ProcesadorRespuestaNetconf.IntentarExtraerConfiguracion(
            respuesta,
            out string configuracion);

        Assert.True(resultado);
        Assert.Contains("router-principal", configuracion, StringComparison.Ordinal);
        Assert.Contains(
            $">{ProcesadorRespuestaNetconf.MarcaDatoProtegido}<",
            configuracion,
            StringComparison.Ordinal);
        Assert.Contains(
            $"community=\"{ProcesadorRespuestaNetconf.MarcaDatoProtegido}\"",
            configuracion,
            StringComparison.Ordinal);
        Assert.DoesNotContain("SecretoVisible", configuracion, StringComparison.Ordinal);
        Assert.DoesNotContain("publica", configuracion, StringComparison.Ordinal);
    }

    [Fact]
    public void Procesador_EnmascaraConfiguracionCliIncluidaEnTextoXml()
    {
        XmlDocument respuesta = CargarXml(
            """
            <rpc-reply xmlns="urn:ietf:params:xml:ns:netconf:base:1.0">
              <data><config-text>hostname borde
            username operador secret TextoVisible
            interface GigabitEthernet1</config-text></data>
            </rpc-reply>
            """);

        bool resultado = ProcesadorRespuestaNetconf.IntentarExtraerConfiguracion(
            respuesta,
            out string configuracion);

        Assert.True(resultado);
        Assert.Contains("hostname borde", configuracion, StringComparison.Ordinal);
        Assert.Contains(
            ProcesadorRespuestaNetconf.MarcaDatoProtegido,
            configuracion,
            StringComparison.Ordinal);
        Assert.DoesNotContain("TextoVisible", configuracion, StringComparison.Ordinal);
    }

    [Fact]
    public void Procesador_RechazaRpcErrorAunqueIncluyaData()
    {
        XmlDocument respuesta = CargarXml(
            """
            <rpc-reply xmlns="urn:ietf:params:xml:ns:netconf:base:1.0">
              <rpc-error><error-tag>access-denied</error-tag></rpc-error>
              <data><hostname>no-utilizable</hostname></data>
            </rpc-reply>
            """);

        bool resultado = ProcesadorRespuestaNetconf.IntentarExtraerConfiguracion(
            respuesta,
            out string configuracion);

        Assert.False(resultado);
        Assert.Empty(configuracion);
    }

    [Theory]
    [InlineData("<rpc-reply xmlns='urn:ietf:params:xml:ns:netconf:base:1.0' />")]
    [InlineData("<rpc-reply xmlns='urn:ietf:params:xml:ns:netconf:base:1.0'><data /></rpc-reply>")]
    public void Procesador_RechazaRespuestaSinConfiguracionUtilizable(string xml)
    {
        XmlDocument respuesta = CargarXml(xml);

        bool resultado = ProcesadorRespuestaNetconf.IntentarExtraerConfiguracion(
            respuesta,
            out string configuracion);

        Assert.False(resultado);
        Assert.Empty(configuracion);
    }

    private static XmlDocument CargarXml(string contenido)
    {
        XmlDocument documento = new()
        {
            PreserveWhitespace = false,
            XmlResolver = null
        };
        documento.LoadXml(contenido);
        return documento;
    }
}
