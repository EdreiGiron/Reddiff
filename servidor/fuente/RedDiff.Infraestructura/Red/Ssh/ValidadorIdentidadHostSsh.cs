using System.Security.Cryptography;

namespace RedDiff.Infraestructura.Red.Ssh;

internal static class ValidadorIdentidadHostSsh
{
    public static bool Coincide(
        string algoritmoEsperado,
        string huellaEsperada,
        string algoritmoRecibido,
        ReadOnlySpan<byte> claveHostRecibida)
    {
        if (!AlgoritmoCoincide(algoritmoEsperado, algoritmoRecibido))
        {
            return false;
        }

        byte[] huellaEsperadaBytes;
        try
        {
            huellaEsperadaBytes = Convert.FromHexString(huellaEsperada);
        }
        catch (FormatException)
        {
            return false;
        }

        if (huellaEsperadaBytes.Length != SHA256.HashSizeInBytes)
        {
            CryptographicOperations.ZeroMemory(huellaEsperadaBytes);
            return false;
        }

        Span<byte> huellaRecibida = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(claveHostRecibida, huellaRecibida);

        bool coincide = CryptographicOperations.FixedTimeEquals(
            huellaEsperadaBytes,
            huellaRecibida);
        CryptographicOperations.ZeroMemory(huellaEsperadaBytes);
        CryptographicOperations.ZeroMemory(huellaRecibida);
        return coincide;
    }

    private static bool AlgoritmoCoincide(string esperado, string recibido)
    {
        if (string.Equals(esperado, recibido, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(esperado, "ssh-rsa", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(recibido, "rsa-sha2-256", StringComparison.OrdinalIgnoreCase)
                || string.Equals(recibido, "rsa-sha2-512", StringComparison.OrdinalIgnoreCase));
    }
}
