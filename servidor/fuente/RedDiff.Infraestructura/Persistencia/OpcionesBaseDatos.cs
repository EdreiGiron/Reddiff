using Npgsql;

namespace RedDiff.Infraestructura.Persistencia;

public sealed class OpcionesBaseDatos
{
    public const string NombreSeccion = "BaseDatos";

    public string Servidor { get; init; } = string.Empty;

    public int Puerto { get; init; } = 5432;

    public string Nombre { get; init; } = string.Empty;

    public string Usuario { get; init; } = string.Empty;

    public string Contrasena { get; init; } = string.Empty;

    public SslMode ModoSsl { get; init; } = SslMode.VerifyFull;

    public int TiempoEsperaSegundos { get; init; } = 5;

    public int TiempoComandoSegundos { get; init; } = 30;

    public string ConstruirCadenaConexion()
    {
        NpgsqlConnectionStringBuilder constructor = new()
        {
            Host = Servidor,
            Port = Puerto,
            Database = Nombre,
            Username = Usuario,
            Password = Contrasena,
            SslMode = ModoSsl,
            Timeout = TiempoEsperaSegundos,
            CommandTimeout = TiempoComandoSegundos,
            ApplicationName = "RedDiff.Api",
            SearchPath = "reddiff,public",
            IncludeErrorDetail = false,
            Pooling = true
        };

        return constructor.ConnectionString;
    }
}
