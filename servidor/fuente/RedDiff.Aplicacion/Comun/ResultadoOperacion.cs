namespace RedDiff.Aplicacion.Comun;

public sealed record ErrorOperacion(string Codigo, string Mensaje);

public sealed record ResultadoOperacion<T> where T : class
{
    private ResultadoOperacion(T? valor, ErrorOperacion? error)
    {
        Valor = valor;
        Error = error;
    }

    public bool Exitoso => Error is null;

    public T? Valor { get; }

    public ErrorOperacion? Error { get; }

    public static ResultadoOperacion<T> Correcto(T valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return new ResultadoOperacion<T>(valor, null);
    }

    public static ResultadoOperacion<T> Fallido(string codigo, string mensaje)
    {
        return new ResultadoOperacion<T>(null, new ErrorOperacion(codigo, mensaje));
    }
}
