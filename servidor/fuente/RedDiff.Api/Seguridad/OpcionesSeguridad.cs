namespace RedDiff.Api.Seguridad;

public sealed class OpcionesSeguridad
{
    public const string NombreSeccion = "Seguridad";

    public int DuracionSesionMinutos { get; init; } = 30;

    public int LimiteIntentosInicioSesionPorMinuto { get; init; } = 10;

    public string? OrigenCliente { get; init; }
}
