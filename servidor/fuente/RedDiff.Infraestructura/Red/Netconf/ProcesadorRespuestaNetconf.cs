using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace RedDiff.Infraestructura.Red.Netconf;

internal static partial class ProcesadorRespuestaNetconf
{
    internal const string MarcaDatoProtegido = "[PROTEGIDO]";

    public static bool IntentarExtraerConfiguracion(
        XmlDocument respuesta,
        out string configuracion)
    {
        ArgumentNullException.ThrowIfNull(respuesta);

        configuracion = string.Empty;
        if (BuscarPrimerElemento(respuesta, "rpc-error") is not null)
        {
            return false;
        }

        XmlElement? datos = BuscarPrimerElemento(respuesta, "data");
        if (datos is null || !datos.HasChildNodes || string.IsNullOrWhiteSpace(datos.InnerText))
        {
            return false;
        }

        XmlDocument copia = new()
        {
            PreserveWhitespace = false,
            XmlResolver = null
        };
        copia.AppendChild(copia.ImportNode(datos, deep: true));

        EnmascararNodo(copia.DocumentElement!);
        configuracion = Serializar(copia);
        return !string.IsNullOrWhiteSpace(configuracion);
    }

    private static XmlElement? BuscarPrimerElemento(XmlDocument documento, string nombreLocal)
    {
        foreach (XmlElement elemento in documento.GetElementsByTagName("*"))
        {
            if (string.Equals(
                    elemento.LocalName,
                    nombreLocal,
                    StringComparison.OrdinalIgnoreCase))
            {
                return elemento;
            }
        }

        return null;
    }

    private static void EnmascararNodo(XmlElement elemento)
    {
        EnmascararAtributos(elemento);

        if (EsNombreSensible(elemento.LocalName))
        {
            while (elemento.HasChildNodes)
            {
                elemento.RemoveChild(elemento.FirstChild!);
            }

            elemento.InnerText = MarcaDatoProtegido;
            return;
        }

        foreach (XmlNode hijo in elemento.ChildNodes.Cast<XmlNode>().ToArray())
        {
            if (hijo is XmlElement elementoHijo)
            {
                EnmascararNodo(elementoHijo);
            }
            else if (hijo is XmlText or XmlCDataSection)
            {
                hijo.Value = EnmascararTextoNativo(hijo.Value ?? string.Empty);
            }
        }
    }

    private static void EnmascararAtributos(XmlElement elemento)
    {
        foreach (XmlAttribute atributo in elemento.Attributes.Cast<XmlAttribute>())
        {
            if (!atributo.Prefix.Equals("xmlns", StringComparison.Ordinal)
                && !atributo.Name.Equals("xmlns", StringComparison.Ordinal)
                && EsNombreSensible(atributo.LocalName))
            {
                atributo.Value = MarcaDatoProtegido;
            }
        }
    }

    private static bool EsNombreSensible(string nombre)
    {
        string normalizado = nombre.Replace('_', '-').ToLowerInvariant();
        return normalizado.Contains("password", StringComparison.Ordinal)
            || normalizado.Contains("secret", StringComparison.Ordinal)
            || normalizado.Contains("community", StringComparison.Ordinal)
            || normalizado is "key"
                or "key-string"
                or "pre-shared-key"
                or "authentication-key"
                or "private-key";
    }

    private static string EnmascararTextoNativo(string contenido)
    {
        string[] lineas = contenido
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        for (int indice = 0; indice < lineas.Length; indice++)
        {
            if (PatronDatoSensibleCisco().IsMatch(lineas[indice]))
            {
                int longitudSangria = lineas[indice]
                    .TakeWhile(caracter => caracter is ' ' or '\t')
                    .Count();
                lineas[indice] = lineas[indice][..longitudSangria] + MarcaDatoProtegido;
            }
        }

        return string.Join('\n', lineas);
    }

    private static string Serializar(XmlDocument documento)
    {
        XmlWriterSettings configuracion = new()
        {
            Encoding = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true),
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = true
        };

        StringBuilder salida = new();
        using (XmlWriter escritor = XmlWriter.Create(salida, configuracion))
        {
            documento.DocumentElement!.WriteTo(escritor);
        }

        return salida.ToString();
    }

    [GeneratedRegex(
        @"^\s*(?:-----BEGIN\s+.*PRIVATE KEY-----|enable\s+(?:password|secret)|username\s+\S+.*\s(?:password|secret)\s+|snmp-server\s+community\s+|(?:radius-server|tacacs-server)\s+key\s+|neighbor\s+\S+\s+password\s+|crypto\s+isakmp\s+key\s+|(?:key-string|pre-shared-key|authentication-key|password|secret)\s+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PatronDatoSensibleCisco();
}
