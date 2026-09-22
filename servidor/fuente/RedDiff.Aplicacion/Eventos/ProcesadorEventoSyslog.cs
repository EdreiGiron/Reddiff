using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using RedDiff.Aplicacion.Comun;

namespace RedDiff.Aplicacion.Eventos;

public sealed partial class ProcesadorEventoSyslog
{
    public const int TamanoMaximoBytes = 8_192;

    private const string MarcaProtegida = "[PROTEGIDO]";
    private const string EventoSensibleProtegido =
        "[PROTEGIDO: contenido sensible del evento omitido]";
    private static readonly UTF8Encoding Utf8Estricto = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public ResultadoOperacion<EventoSyslogProcesado> Procesar(ReadOnlyMemory<byte> datos)
    {
        if (datos.IsEmpty)
        {
            return Fallar("El datagrama Syslog está vacío.");
        }

        if (datos.Length > TamanoMaximoBytes)
        {
            return Fallar("El datagrama Syslog supera el límite de 8 KB.");
        }

        string contenido;
        try
        {
            contenido = Utf8Estricto.GetString(datos.Span);
        }
        catch (DecoderFallbackException)
        {
            return Fallar("El datagrama Syslog no contiene texto UTF-8 válido.");
        }

        string normalizado = contenido
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Normalize(NormalizationForm.FormC)
            .Trim('\0', ' ', '\t', '\n');

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            return Fallar("El datagrama Syslog no contiene información utilizable.");
        }

        if (normalizado.Any(EsCaracterNoPermitido))
        {
            return Fallar("El datagrama Syslog contiene caracteres de control no permitidos.");
        }

        string seguro = PatronDatoSensible().Replace(
            normalizado,
            coincidencia => $"{coincidencia.Groups[1].Value}{MarcaProtegida}");
        if (PatronComandoCiscoSensible().IsMatch(seguro))
        {
            seguro = EventoSensibleProtegido;
        }
        string huella = Convert.ToHexString(
            SHA256.HashData(Utf8Estricto.GetBytes(seguro)))
            .ToLowerInvariant();

        return ResultadoOperacion<EventoSyslogProcesado>.Correcto(
            new EventoSyslogProcesado(seguro, huella));
    }

    private static bool EsCaracterNoPermitido(char caracter)
    {
        return char.IsControl(caracter) && caracter is not '\n' and not '\t';
    }

    private static ResultadoOperacion<EventoSyslogProcesado> Fallar(string mensaje)
    {
        return ResultadoOperacion<EventoSyslogProcesado>.Fallido(
            CodigosErrorOperacion.Validacion,
            mensaje);
    }

    [GeneratedRegex(
        @"(?i)\b((?:password|secret|community|token|key)\s*(?:=|:)\s*)[^\s,;]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex PatronDatoSensible();

    [GeneratedRegex(
        @"(?i)\b(?:enable\s+(?:password|secret)|username\s+\S+.*\s(?:password|secret)|snmp-server\s+community|(?:radius-server|tacacs-server)\s+key|neighbor\s+\S+\s+password|crypto\s+isakmp\s+key|pre-shared-key|authentication-key)\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex PatronComandoCiscoSensible();
}
