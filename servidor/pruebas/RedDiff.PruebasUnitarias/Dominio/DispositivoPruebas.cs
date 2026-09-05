using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.PruebasUnitarias.Dominio;

public sealed class DispositivoPruebas
{
    [Fact]
    public void Crear_NormalizaHostYComienzaSinAutorizacion()
    {
        Dispositivo dispositivo = new(
            "Núcleo principal",
            " CORE-01.EJEMPLO.LOCAL ",
            "Router",
            "ISR 4331",
            ProtocoloConexion.Ssh,
            22,
            FuenteEvento.Syslog);

        Assert.Equal("core-01.ejemplo.local", dispositivo.Host);
        Assert.Equal(EstadoDispositivo.NoAutorizado, dispositivo.Estado);
    }

    [Fact]
    public void Crear_RechazaHostConEsquemaORuta()
    {
        Assert.Throws<ArgumentException>(() => new Dispositivo(
            "Borde",
            "ssh://10.0.0.1/config",
            "Router",
            null,
            ProtocoloConexion.Ssh,
            22,
            null));
    }

    [Fact]
    public void ActualizarDatos_RechazaPuertoFueraDeRango()
    {
        Dispositivo dispositivo = CrearDispositivo();

        Assert.Throws<ArgumentOutOfRangeException>(() => dispositivo.ActualizarDatos(
            "Acceso",
            "10.0.0.2",
            "Switch",
            "Catalyst",
            ProtocoloConexion.Netconf,
            0,
            FuenteEvento.SnmpInform));

        Assert.Equal("Acceso", dispositivo.Nombre);
        Assert.Equal("10.0.0.1", dispositivo.Host);
        Assert.Equal(ProtocoloConexion.Ssh, dispositivo.Protocolo);
    }

    [Fact]
    public void CambiarEstado_PermiteCicloOperativoDelInventario()
    {
        Dispositivo dispositivo = CrearDispositivo();

        dispositivo.Autorizar();
        Assert.Equal(EstadoDispositivo.Autorizado, dispositivo.Estado);

        dispositivo.RevocarAutorizacion();
        Assert.Equal(EstadoDispositivo.NoAutorizado, dispositivo.Estado);

        dispositivo.Desactivar();
        Assert.Equal(EstadoDispositivo.Inactivo, dispositivo.Estado);

        dispositivo.CambiarEstado(EstadoDispositivo.NoAutorizado);
        Assert.Equal(EstadoDispositivo.NoAutorizado, dispositivo.Estado);
    }

    [Fact]
    public void ConfigurarAccesoRemoto_RequiereDispositivoAutorizado()
    {
        Dispositivo dispositivo = CrearDispositivo();

        Assert.Throws<InvalidOperationException>(() => ConfigurarAcceso(dispositivo));
        Assert.False(dispositivo.AccesoRemotoConfigurado);
    }

    [Fact]
    public void ConfigurarAccesoRemoto_NormalizaMetadatosSinGuardarTextoPlano()
    {
        Dispositivo dispositivo = CrearDispositivo();
        dispositivo.Autorizar();

        dispositivo.ConfigurarAccesoRemoto(
            " operador.lectura ",
            "AQAAANCM-protegido",
            " SSH-ED25519 ",
            new string('A', 64),
            new DateTimeOffset(2026, 9, 5, 8, 30, 0, TimeSpan.FromHours(-6)));

        Assert.True(dispositivo.AccesoRemotoConfigurado);
        Assert.Equal("operador.lectura", dispositivo.UsuarioAcceso);
        Assert.Equal("AQAAANCM-protegido", dispositivo.SecretoAccesoProtegido);
        Assert.Equal("ssh-ed25519", dispositivo.AlgoritmoClaveHost);
        Assert.Equal(new string('a', 64), dispositivo.HuellaClaveHost);
        Assert.Equal(
            new DateTimeOffset(2026, 9, 5, 14, 30, 0, TimeSpan.Zero),
            dispositivo.AccesoConfiguradoEn);
    }

    [Fact]
    public void ConfigurarAccesoRemoto_RechazaHuellaInvalidaSinAlterarConfiguracion()
    {
        Dispositivo dispositivo = CrearDispositivo();
        dispositivo.Autorizar();

        Assert.Throws<ArgumentException>(() => dispositivo.ConfigurarAccesoRemoto(
            "operador",
            "secreto-protegido",
            "ssh-ed25519",
            "huella-invalida",
            DateTimeOffset.UtcNow));

        Assert.False(dispositivo.AccesoRemotoConfigurado);
        Assert.Null(dispositivo.UsuarioAcceso);
    }

    [Fact]
    public void ActualizarPuntoConexion_InvalidaAccesoRemotoAnterior()
    {
        Dispositivo dispositivo = CrearDispositivo();
        dispositivo.Autorizar();
        ConfigurarAcceso(dispositivo);

        dispositivo.ActualizarDatos(
            dispositivo.Nombre,
            "10.0.0.2",
            dispositivo.Tipo,
            dispositivo.Modelo,
            ProtocoloConexion.Netconf,
            830,
            dispositivo.FuenteEventos);

        Assert.False(dispositivo.AccesoRemotoConfigurado);
        Assert.Null(dispositivo.SecretoAccesoProtegido);
        Assert.Null(dispositivo.HuellaClaveHost);
    }

    [Fact]
    public void RevocarAutorizacion_EliminaAccesoRemotoAnterior()
    {
        Dispositivo dispositivo = CrearDispositivo();
        dispositivo.Autorizar();
        ConfigurarAcceso(dispositivo);

        dispositivo.RevocarAutorizacion();

        Assert.Equal(EstadoDispositivo.NoAutorizado, dispositivo.Estado);
        Assert.False(dispositivo.AccesoRemotoConfigurado);
        Assert.Null(dispositivo.SecretoAccesoProtegido);
    }

    private static void ConfigurarAcceso(Dispositivo dispositivo)
    {
        dispositivo.ConfigurarAccesoRemoto(
            "operador.lectura",
            "secreto-protegido",
            "ssh-ed25519",
            new string('b', 64),
            DateTimeOffset.UtcNow);
    }

    private static Dispositivo CrearDispositivo()
    {
        return new Dispositivo(
            "Acceso",
            "10.0.0.1",
            "Switch",
            null,
            ProtocoloConexion.Ssh,
            22,
            FuenteEvento.SnmpTrap);
    }
}
