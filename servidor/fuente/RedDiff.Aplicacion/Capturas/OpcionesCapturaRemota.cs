namespace RedDiff.Aplicacion.Capturas;

public sealed class OpcionesCapturaRemota
{
    public static OpcionesCapturaRemota Predeterminadas { get; } = new(TimeSpan.FromSeconds(15));

    public OpcionesCapturaRemota(TimeSpan tiempoEspera)
    {
        if (tiempoEspera < TimeSpan.FromSeconds(1)
            || tiempoEspera > TimeSpan.FromSeconds(60))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tiempoEspera),
                "El tiempo de espera debe estar entre 1 y 60 segundos.");
        }

        TiempoEspera = tiempoEspera;
    }

    public TimeSpan TiempoEspera { get; }
}
