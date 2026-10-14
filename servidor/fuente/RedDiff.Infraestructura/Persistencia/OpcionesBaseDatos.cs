using Npgsql;

namespace RedDiff.Infraestructura.Persistencia;

public sealed class OpcionesBaseDatos
{
    public const string NombreSeccion = "BaseDatos";

    public string Servidor { get; init; } = string.Empty;

    public int Puerto { get; init; } = 5432;

    public int VersionMayorServidor { get; init; } = 18;

    public string Nombre { get; init; } = string.Empty;

    public string Usuario { get; init; } = string.Empty;

    public string Contrasena { get; init; } = string.Empty;

    public string? ArchivoContrasena { get; init; }

    public SslMode ModoSsl { get; init; } = SslMode.VerifyFull;

    public bool PermitirConexionLocalSinTls { get; init; }

    public string? CertificadoRaiz { get; init; }

    public int TiempoEsperaSegundos { get; init; } = 5;

    public int TiempoComandoSegundos { get; init; } = 30;

    public bool TieneContrasenaDisponible()
    {
        return !string.IsNullOrWhiteSpace(Contrasena)
            || (!string.IsNullOrWhiteSpace(ArchivoContrasena)
                && File.Exists(ArchivoContrasena));
    }

    public bool TieneTransporteSeguroParaProduccion()
    {
        if (ModoSsl == SslMode.VerifyFull)
        {
            return true;
        }

        return PermitirConexionLocalSinTls
            && ModoSsl == SslMode.Disable
            && EsServidorLocal(Servidor);
    }

    public string ConstruirCadenaConexion()
    {
        NpgsqlConnectionStringBuilder constructor = new()
        {
            Host = Servidor,
            Port = Puerto,
            Database = Nombre,
            Username = Usuario,
            Password = ObtenerContrasena(),
            SslMode = ModoSsl,
            Timeout = TiempoEsperaSegundos,
            CommandTimeout = TiempoComandoSegundos,
            ApplicationName = "RedDiff.Api",
            SearchPath = "reddiff,public",
            IncludeErrorDetail = false,
            Pooling = true
        };

        if (!string.IsNullOrWhiteSpace(CertificadoRaiz))
        {
            constructor.RootCertificate = CertificadoRaiz;
        }

        return constructor.ConnectionString;
    }

    private string ObtenerContrasena()
    {
        if (!string.IsNullOrWhiteSpace(Contrasena))
        {
            return Contrasena;
        }

        if (string.IsNullOrWhiteSpace(ArchivoContrasena))
        {
            throw new InvalidOperationException(
                "No se configuró una contraseña ni un archivo de contraseña para PostgreSQL.");
        }

        string valor = File.ReadAllText(ArchivoContrasena).TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException(
                "El archivo de contraseña de PostgreSQL está vacío.");
        }

        return valor;
    }

    private static bool EsServidorLocal(string servidor)
    {
        return string.Equals(servidor, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(servidor, "127.0.0.1", StringComparison.Ordinal)
            || string.Equals(servidor, "::1", StringComparison.Ordinal);
    }
}
