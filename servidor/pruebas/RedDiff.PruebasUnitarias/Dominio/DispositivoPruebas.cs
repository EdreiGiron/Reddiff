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
