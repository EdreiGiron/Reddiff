using System.Text.RegularExpressions;

namespace RedDiff.Infraestructura.Red.Ssh;

internal static partial class ProcesadorSalidaCiscoIos
{
    private const string InicioConfiguracion = "Building configuration...";
    private const string FinConfiguracion = "end";

    internal static Regex CrearPatronPromptFinal(string? promptEsperado)
    {
        return string.IsNullOrWhiteSpace(promptEsperado)
            ? PatronPromptFinal()
            : new Regex(
                $@"(?:^|\r?\n){Regex.Escape(promptEsperado)}\s*\z",
                RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(1));
    }

    internal static bool TryObtenerPromptFinal(string contenido, out string prompt)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        Match coincidencia = PatronPromptFinal().Match(contenido);
        prompt = coincidencia.Success
            ? coincidencia.Groups["prompt"].Value
            : string.Empty;
        return coincidencia.Success;
    }

    internal static string? ExtraerResultadoComando(
        string respuesta,
        string comando,
        string prompt)
    {
        ArgumentNullException.ThrowIfNull(respuesta);
        ArgumentException.ThrowIfNullOrWhiteSpace(comando);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        List<string> lineas = LimpiarSecuenciasTerminal(respuesta)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .ToList();

        int inicioConfiguracion = lineas.FindIndex(
            linea => string.Equals(
                linea.Trim(),
                InicioConfiguracion,
                StringComparison.OrdinalIgnoreCase));
        int finConfiguracion = lineas.FindLastIndex(
            linea => string.Equals(
                linea.Trim(),
                FinConfiguracion,
                StringComparison.OrdinalIgnoreCase));

        if (inicioConfiguracion >= 0 && finConfiguracion >= inicioConfiguracion)
        {
            return string.Join(
                '\n',
                lineas.GetRange(
                    inicioConfiguracion,
                    finConfiguracion - inicioConfiguracion + 1));
        }

        while (lineas.Count > 0 && string.IsNullOrWhiteSpace(lineas[0]))
        {
            lineas.RemoveAt(0);
        }

        if (lineas.Count > 0
            && string.Equals(lineas[0].Trim(), comando, StringComparison.Ordinal))
        {
            lineas.RemoveAt(0);
        }

        while (lineas.Count > 0 && string.IsNullOrWhiteSpace(lineas[^1]))
        {
            lineas.RemoveAt(lineas.Count - 1);
        }

        if (lineas.Count > 0
            && string.Equals(lineas[^1].Trim(), prompt, StringComparison.Ordinal))
        {
            lineas.RemoveAt(lineas.Count - 1);
        }

        while (lineas.Count > 0 && string.IsNullOrWhiteSpace(lineas[^1]))
        {
            lineas.RemoveAt(lineas.Count - 1);
        }

        return lineas.Count == 0 ? null : string.Join('\n', lineas);
    }

    private static string LimpiarSecuenciasTerminal(string contenido)
    {
        string sinAnsi = PatronSecuenciaAnsi().Replace(contenido, string.Empty);
        if (sinAnsi.IndexOf('\b') < 0)
        {
            return sinAnsi;
        }

        Span<char> caracteres = sinAnsi.Length <= 4096
            ? stackalloc char[sinAnsi.Length]
            : new char[sinAnsi.Length];
        int longitud = 0;

        foreach (char caracter in sinAnsi)
        {
            if (caracter == '\b')
            {
                if (longitud > 0 && caracteres[longitud - 1] is not '\r' and not '\n')
                {
                    longitud--;
                }

                continue;
            }

            caracteres[longitud++] = caracter;
        }

        return new string(caracteres[..longitud]);
    }

    internal static bool ContieneErrorCli(string? contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return false;
        }

        return contenido.Contains("% Invalid input", StringComparison.OrdinalIgnoreCase)
            || contenido.Contains("% Incomplete command", StringComparison.OrdinalIgnoreCase)
            || contenido.Contains("% Ambiguous command", StringComparison.OrdinalIgnoreCase)
            || contenido.Contains("% Authorization failed", StringComparison.OrdinalIgnoreCase)
            || contenido.Contains("% Unrecognized command", StringComparison.OrdinalIgnoreCase)
            || contenido.Contains("invalid autocommand", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(
        @"(?:^|\r?\n)(?<prompt>[^\r\n]{1,128}[>#])\s*\z",
        RegexOptions.CultureInvariant)]
    private static partial Regex PatronPromptFinal();

    [GeneratedRegex(
        @"\x1B\[[0-?]*[ -/]*[@-~]",
        RegexOptions.CultureInvariant)]
    private static partial Regex PatronSecuenciaAnsi();
}
