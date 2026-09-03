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
        Assert.Equal(SslMode.Disable, resultado.SslMode);
        Assert.Equal(7, resultado.Timeout);
        Assert.Equal(40, resultado.CommandTimeout);
        Assert.Equal("reddiff,public", resultado.SearchPath);
        Assert.False(resultado.IncludeErrorDetail);
        Assert.True(resultado.Pooling);
    }
}
