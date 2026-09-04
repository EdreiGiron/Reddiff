namespace RedDiff.Dominio.Validaciones;

internal static class ValidacionDominio
{
    public static long Identificador(long valor, string nombreParametro)
    {
        if (valor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                "El identificador debe ser mayor que cero.");
        }

        return valor;
    }

    public static int EnteroPositivo(int valor, string nombreParametro)
    {
        if (valor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nombreParametro,
                "El valor debe ser mayor que cero.");
        }

        return valor;
    }

    public static string TextoObligatorio(
        string valor,
        string nombreParametro,
        int longitudMaxima)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor, nombreParametro);

        string texto = valor.Trim();

        if (texto.Length > longitudMaxima)
        {
            throw new ArgumentException(
                $"El valor no puede superar {longitudMaxima} caracteres.",
                nombreParametro);
        }

        return texto;
    }

    public static string? TextoOpcional(
        string? valor,
        string nombreParametro,
        int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return TextoObligatorio(valor, nombreParametro, longitudMaxima);
    }

    public static string ContenidoObligatorio(
        string valor,
        string nombreParametro,
        int longitudMaxima)
    {
        ArgumentNullException.ThrowIfNull(valor, nombreParametro);

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException("El contenido es obligatorio.", nombreParametro);
        }

        ValidarLongitud(valor, nombreParametro, longitudMaxima);
        return valor;
    }

    public static string? ContenidoOpcional(
        string? valor,
        string nombreParametro,
        int longitudMaxima)
    {
        if (valor is null)
        {
            return null;
        }

        ValidarLongitud(valor, nombreParametro, longitudMaxima);
        return valor;
    }

    public static DateTimeOffset FechaUtc(
        DateTimeOffset valor,
        string nombreParametro)
    {
        if (valor == default)
        {
            throw new ArgumentException("La fecha es obligatoria.", nombreParametro);
        }

        return valor.ToUniversalTime();
    }

    public static string HuellaSha256(string valor, string nombreParametro)
    {
        string huella = TextoObligatorio(valor, nombreParametro, 64).ToLowerInvariant();

        if (huella.Length != 64 || huella.Any(static caracter => !Uri.IsHexDigit(caracter)))
        {
            throw new ArgumentException(
                "La huella debe ser un valor SHA-256 hexadecimal de 64 caracteres.",
                nombreParametro);
        }

        return huella;
    }

    private static void ValidarLongitud(
        string valor,
        string nombreParametro,
        int longitudMaxima)
    {
        if (valor.Length > longitudMaxima)
        {
            throw new ArgumentException(
                $"El valor no puede superar {longitudMaxima} caracteres.",
                nombreParametro);
        }
    }
}
