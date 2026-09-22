namespace RedDiff.Infraestructura.Red.Syslog;

public sealed class OpcionesReceptorSyslog
{
    public const string NombreSeccion = "Eventos:Syslog";

    public bool Habilitado { get; init; }

    public string DireccionEscucha { get; init; } = "0.0.0.0";

    public int Puerto { get; init; } = 5514;
}
