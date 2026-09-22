namespace RedDiff.Dominio.Enumeraciones;

public enum EstadoEventoCambio
{
    Recibido = 1,
    Validado = 2,
    Encolado = 3,
    Procesado = 4,
    Rechazado = 5,
    Duplicado = 6,
    Fallido = 7,
    SinCambios = 8
}
