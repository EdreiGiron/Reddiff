using Npgsql;
using RedDiff.Infraestructura.Persistencia;

namespace RedDiff.PruebasIntegracion.Infraestructura;

public sealed class OpcionesBaseDatosPruebas
{
    [Fact]
    public void ConstruirCadenaConexion_DebeAplicarControlesSeguros()
    {
        OpcionesBaseDatos opciones = new()
        {
            Servidor = "127.0.0.1",
            Puerto = 5432,
            VersionMayorServidor = 15,
            Nombre = "reddiff_pruebas",
            Usuario = "reddiff_pruebas",
            Contrasena = "secreto-temporal-de-prueba",
            ModoSsl = SslMode.Disable,
            TiempoEsperaSegundos = 7,
            TiempoComandoSegundos = 40
        };

        NpgsqlConnectionStringBuilder resultado = new(opciones.ConstruirCadenaConexion());

        Assert.Equal("127.0.0.1", resultado.Host);
        Assert.Equal(5432, resultado.Port);
        Assert.Equal("reddiff_pruebas", resultado.Database);
        Assert.Equal("reddiff_pruebas", resultado.Username);
        Assert.Equal("secreto-temporal-de-prueba", resultado.Password);
        Assert.Equal(SslMode.Disable, resultado.SslMode);
        Assert.Equal(7, resultado.Timeout);
        Assert.Equal(40, resultado.CommandTimeout);
        Assert.Equal("reddiff,public", resultado.SearchPath);
        Assert.False(resultado.IncludeErrorDetail);
        Assert.True(resultado.Pooling);
        Assert.False(opciones.TieneTransporteSeguroParaProduccion());
    }

    [Fact]
    public void ConstruirCadenaConexion_DebeLeerContrasenaDesdeArchivoSeguro()
    {
        string rutaSecreto = Path.GetTempFileName();

        try
        {
            File.WriteAllText(rutaSecreto, "secreto-desde-archivo\n");
            OpcionesBaseDatos opciones = new()
            {
                Servidor = "base-datos",
                Nombre = "reddiff",
                Usuario = "reddiff_app",
                ArchivoContrasena = rutaSecreto,
                ModoSsl = SslMode.VerifyFull,
                CertificadoRaiz = "/run/secrets/postgresql_ca_certificado"
            };

            NpgsqlConnectionStringBuilder resultado = new(opciones.ConstruirCadenaConexion());

            Assert.True(opciones.TieneContrasenaDisponible());
            Assert.True(opciones.TieneTransporteSeguroParaProduccion());
            Assert.Equal("secreto-desde-archivo", resultado.Password);
            Assert.Equal("/run/secrets/postgresql_ca_certificado", resultado.RootCertificate);
        }
        finally
        {
            File.Delete(rutaSecreto);
        }
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("localhost")]
    [InlineData("::1")]
    public void TieneTransporteSeguroParaProduccion_DebeAceptarExcepcionLocalExplicita(
        string servidor)
    {
        OpcionesBaseDatos opciones = new()
        {
            Servidor = servidor,
            ModoSsl = SslMode.Disable,
            PermitirConexionLocalSinTls = true
        };

        Assert.True(opciones.TieneTransporteSeguroParaProduccion());
    }

    [Fact]
    public void TieneTransporteSeguroParaProduccion_DebeRechazarServidorRemotoSinTls()
    {
        OpcionesBaseDatos opciones = new()
        {
            Servidor = "192.168.200.10",
            ModoSsl = SslMode.Disable,
            PermitirConexionLocalSinTls = true
        };

        Assert.False(opciones.TieneTransporteSeguroParaProduccion());
    }
}
