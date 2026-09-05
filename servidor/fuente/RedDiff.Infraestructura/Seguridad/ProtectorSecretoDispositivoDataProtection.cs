using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using RedDiff.Aplicacion.Abstracciones.Seguridad;

namespace RedDiff.Infraestructura.Seguridad;

internal sealed class ProtectorSecretoDispositivoDataProtection(
    IDataProtectionProvider proveedorProteccion) : IProtectorSecretoDispositivo
{
    private const int LongitudMaximaSecreto = 256;
    private const int LongitudMaximaSecretoProtegido = 4_000;
    private const string Proposito = "RedDiff.Dispositivos.Credenciales.v1";

    private static readonly UTF8Encoding CodificacionUtf8Estricta = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly IDataProtector protector = proveedorProteccion.CreateProtector(Proposito);

    public string Proteger(string secreto)
    {
        if (string.IsNullOrWhiteSpace(secreto) || secreto.Length > LongitudMaximaSecreto)
        {
            throw new ArgumentException(
                $"El secreto debe contener entre 1 y {LongitudMaximaSecreto} caracteres.",
                nameof(secreto));
        }

        byte[] textoPlano = CodificacionUtf8Estricta.GetBytes(secreto);

        try
        {
            byte[] datosProtegidos = protector.Protect(textoPlano);
            return Convert.ToBase64String(datosProtegidos);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(textoPlano);
        }
    }

    public bool IntentarDesproteger(string? secretoProtegido, out string secreto)
    {
        secreto = string.Empty;

        if (string.IsNullOrWhiteSpace(secretoProtegido)
            || secretoProtegido.Length > LongitudMaximaSecretoProtegido)
        {
            return false;
        }

        byte[]? textoPlano = null;

        try
        {
            byte[] datosProtegidos = Convert.FromBase64String(secretoProtegido);
            textoPlano = protector.Unprotect(datosProtegidos);
            secreto = CodificacionUtf8Estricta.GetString(textoPlano);

            if (string.IsNullOrWhiteSpace(secreto) || secreto.Length > LongitudMaximaSecreto)
            {
                secreto = string.Empty;
                return false;
            }

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
        finally
        {
            if (textoPlano is not null)
            {
                CryptographicOperations.ZeroMemory(textoPlano);
            }
        }
    }
}
