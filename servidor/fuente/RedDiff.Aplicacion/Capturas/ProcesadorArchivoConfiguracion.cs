using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using RedDiff.Aplicacion.Comun;

namespace RedDiff.Aplicacion.Capturas;

public sealed partial class ProcesadorArchivoConfiguracion
{
    public const int TamanoMaximoBytes = 5_000_000;

    private const int LongitudMaximaNombre = 160;
    private const int LongitudMaximaComentarioUsuario = 300;

    private static readonly HashSet<string> ExtensionesPermitidas = new(
        [".txt", ".cfg", ".conf", ".config"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly UTF8Encoding Utf8Estricto = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public ResultadoOperacion<ArchivoConfiguracionProcesado> Procesar(
        string? nombreArchivo,
        byte[]? bytes,
        string? comentario)
    {
        string nombreSeguro = ObtenerNombreSeguro(nombreArchivo);
        if (string.IsNullOrWhiteSpace(nombreSeguro)
            || nombreSeguro.Length > LongitudMaximaNombre
            || nombreSeguro.Any(char.IsControl)
            || !ExtensionesPermitidas.Contains(Path.GetExtension(nombreSeguro)))
        {
            return Fallar(
                "El archivo debe tener extensión .txt, .cfg, .conf o .config y un nombre válido.");
        }

        if (bytes is null || bytes.Length == 0)
        {
            return Fallar("El archivo de configuración está vacío.");
        }

        if (bytes.Length > TamanoMaximoBytes)
        {
            return Fallar("El archivo supera el límite de 5 MB.");
        }

        string contenido;
        try
        {
            ReadOnlySpan<byte> contenidoBytes = bytes;
            if (contenidoBytes.Length >= 3
                && contenidoBytes[0] == 0xEF
                && contenidoBytes[1] == 0xBB
                && contenidoBytes[2] == 0xBF)
            {
                contenidoBytes = contenidoBytes[3..];
            }

            contenido = Utf8Estricto.GetString(contenidoBytes);
        }
        catch (DecoderFallbackException)
        {
            return Fallar("El archivo debe estar codificado como UTF-8 válido.");
        }

        if (string.IsNullOrWhiteSpace(contenido))
        {
            return Fallar("El archivo no contiene una configuración utilizable.");
        }

        if (contenido.Any(EsCaracterNoPermitido))
        {
            return Fallar("El archivo contiene caracteres de control no permitidos.");
        }

        string contenidoNormalizado = NormalizarParaIntegridad(contenido);
        if (ContieneCredencialSinEnmascarar(contenidoNormalizado))
        {
            return Fallar(
                "El archivo parece contener credenciales o comunidades sin enmascarar. "
                + "Reemplázalas por [PROTEGIDO] antes de cargarlo.");
        }

        string comentarioValidado = comentario?.Trim() ?? string.Empty;
        if (comentarioValidado.Length > LongitudMaximaComentarioUsuario)
        {
            return Fallar("El comentario no puede superar 300 caracteres.");
        }

        string hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(contenidoNormalizado)))
            .ToLowerInvariant();
        string comentarioCompleto = string.IsNullOrEmpty(comentarioValidado)
            ? $"Archivo: {nombreSeguro}."
            : $"Archivo: {nombreSeguro}. {comentarioValidado}";

        return ResultadoOperacion<ArchivoConfiguracionProcesado>.Correcto(
            new ArchivoConfiguracionProcesado(
                nombreSeguro,
                contenidoNormalizado,
                hash,
                comentarioCompleto));
    }

    public ResultadoOperacion<ContenidoConfiguracionProcesado> ProcesarContenidoRemoto(
        string? contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return FallarContenidoRemoto(
                "El dispositivo no devolvió una configuración utilizable.");
        }

        string contenidoNormalizado;
        int tamanoBytes;

        try
        {
            contenidoNormalizado = NormalizarParaIntegridad(contenido);
            tamanoBytes = Utf8Estricto.GetByteCount(contenidoNormalizado);
        }
        catch (EncoderFallbackException)
        {
            return FallarContenidoRemoto(
                "La respuesta remota no contiene texto UTF-8 válido.");
        }
        catch (ArgumentException)
        {
            return FallarContenidoRemoto(
                "La respuesta remota no contiene texto Unicode válido.");
        }

        if (tamanoBytes > TamanoMaximoBytes)
        {
            return FallarContenidoRemoto(
                "La configuración remota supera el límite de 5 MB.");
        }

        if (contenidoNormalizado.Any(EsCaracterNoPermitido))
        {
            return FallarContenidoRemoto(
                "La respuesta remota contiene caracteres de control no permitidos.");
        }

        if (ContieneCredencialSinEnmascarar(contenidoNormalizado))
        {
            return FallarContenidoRemoto(
                "La configuración obtenida contiene credenciales o comunidades sin enmascarar y no será almacenada.");
        }

        string hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(contenidoNormalizado)))
            .ToLowerInvariant();

        return ResultadoOperacion<ContenidoConfiguracionProcesado>.Correcto(
            new ContenidoConfiguracionProcesado(contenidoNormalizado, hash));
    }

    public static string NormalizarParaIntegridad(string contenido)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        return contenido
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Normalize(NormalizationForm.FormC);
    }

    private static string ObtenerNombreSeguro(string? nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            return string.Empty;
        }

        string nombreNormalizado = nombreArchivo.Trim().Replace('\\', '/');
        return Path.GetFileName(nombreNormalizado);
    }

    private static bool EsCaracterNoPermitido(char caracter)
    {
        return char.IsControl(caracter) && caracter is not '\r' and not '\n' and not '\t';
    }

    private static bool ContieneCredencialSinEnmascarar(string contenido)
    {
        foreach (string linea in contenido.Split('\n'))
        {
            if (PatronDatoSensible().IsMatch(linea) && !PatronEnmascarado().IsMatch(linea))
            {
                return true;
            }
        }

        return false;
    }

    private static ResultadoOperacion<ArchivoConfiguracionProcesado> Fallar(string mensaje)
    {
        return ResultadoOperacion<ArchivoConfiguracionProcesado>.Fallido(
            CodigosErrorOperacion.Validacion,
            mensaje);
    }

    private static ResultadoOperacion<ContenidoConfiguracionProcesado> FallarContenidoRemoto(
        string mensaje)
    {
        return ResultadoOperacion<ContenidoConfiguracionProcesado>.Fallido(
            CodigosErrorOperacion.Validacion,
            mensaje);
    }

    [GeneratedRegex(
        @"^\s*(?:-----BEGIN\s+.*PRIVATE KEY-----|enable\s+(?:password|secret)|username\s+\S+.*\s(?:password|secret)\s+|snmp-server\s+community\s+|(?:radius-server|tacacs-server)\s+key\s+|neighbor\s+\S+\s+password\s+|crypto\s+isakmp\s+key\s+|(?:key-string|pre-shared-key|authentication-key|password)\s+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PatronDatoSensible();

    [GeneratedRegex(
        @"(?:\[PROTEGIDO\]|<PROTEGIDO>|\*{3,}|REDACTED|ENMASCARADO)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PatronEnmascarado();
}
