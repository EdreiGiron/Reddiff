using System.Security.Cryptography;
using System.Text;

namespace RedDiff.Aplicacion.Seguridad;

public static class VersionCredencial
{
    public static string Calcular(string contrasenaHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contrasenaHash);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contrasenaHash));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
