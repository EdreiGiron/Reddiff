using System.Security.Cryptography;
using RedDiff.Aplicacion.Abstracciones.Seguridad;

namespace RedDiff.Infraestructura.Seguridad;

internal sealed class ProtectorContrasenaPbkdf2 : IProtectorContrasena
{
    private const int Iteraciones = 600_000;
    private const int LongitudSal = 16;
    private const int LongitudHash = 32;
    private const string Prefijo = "pbkdf2-sha256";
    private const string HashSimulado =
        "pbkdf2-sha256$600000$AAAAAAAAAAAAAAAAAAAAAA==$F19IEySam/DGJyJxXidsjpwcXrcrJV/df/wHHZqBnlw=";

    public string CrearHash(string contrasena)
    {
        ValidarContrasena(contrasena);

        byte[] sal = RandomNumberGenerator.GetBytes(LongitudSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            contrasena,
            sal,
            Iteraciones,
            HashAlgorithmName.SHA256,
            LongitudHash);

        return string.Join(
            '$',
            Prefijo,
            Iteraciones,
            Convert.ToBase64String(sal),
            Convert.ToBase64String(hash));
    }

    public bool Verificar(string contrasena, string? hashAlmacenado)
    {
        string hashEvaluado = string.IsNullOrWhiteSpace(hashAlmacenado)
            ? HashSimulado
            : hashAlmacenado;

        try
        {
            string[] partes = hashEvaluado.Split('$');
            if (partes.Length != 4
                || !string.Equals(partes[0], Prefijo, StringComparison.Ordinal)
                || !int.TryParse(partes[1], out int iteraciones)
                || iteraciones is < 100_000 or > 1_000_000)
            {
                return false;
            }

            byte[] sal = Convert.FromBase64String(partes[2]);
            byte[] esperado = Convert.FromBase64String(partes[3]);
            if (sal.Length != LongitudSal || esperado.Length != LongitudHash)
            {
                return false;
            }

            byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(
                contrasena ?? string.Empty,
                sal,
                iteraciones,
                HashAlgorithmName.SHA256,
                esperado.Length);

            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void ValidarContrasena(string contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena)
            || contrasena.Length is < 12 or > 128)
        {
            throw new ArgumentException(
                "La contraseña debe contener entre 12 y 128 caracteres.",
                nameof(contrasena));
        }
    }
}
