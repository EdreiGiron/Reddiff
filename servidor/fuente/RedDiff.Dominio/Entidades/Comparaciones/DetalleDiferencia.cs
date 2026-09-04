using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Comparaciones;

public sealed class DetalleDiferencia
{
    private DetalleDiferencia()
    {
    }

    public DetalleDiferencia(
        long comparacionId,
        int linea,
        TipoDiferencia tipo,
        string? textoAnterior,
        string? textoNuevo)
    {
        ComparacionId = ValidacionDominio.Identificador(comparacionId, nameof(comparacionId));
        Linea = ValidacionDominio.EnteroPositivo(linea, nameof(linea));
        Tipo = tipo;
        TextoAnterior = ValidacionDominio.ContenidoOpcional(
            textoAnterior,
            nameof(textoAnterior),
            10_000);
        TextoNuevo = ValidacionDominio.ContenidoOpcional(
            textoNuevo,
            nameof(textoNuevo),
            10_000);

        ValidarContenido(tipo, TextoAnterior, TextoNuevo);
    }

    public long Id { get; private set; }

    public long ComparacionId { get; private set; }

    public int Linea { get; private set; }

    public TipoDiferencia Tipo { get; private set; }

    public string? TextoAnterior { get; private set; }

    public string? TextoNuevo { get; private set; }

    public Comparacion Comparacion { get; private set; } = null!;

    private static void ValidarContenido(
        TipoDiferencia tipo,
        string? textoAnterior,
        string? textoNuevo)
    {
        bool invalido = tipo switch
        {
            TipoDiferencia.Agregada => textoNuevo is null,
            TipoDiferencia.Eliminada => textoAnterior is null,
            TipoDiferencia.Modificada => textoAnterior is null || textoNuevo is null,
            _ => true
        };

        if (invalido)
        {
            throw new ArgumentException("El contenido no corresponde con el tipo de diferencia.");
        }
    }
}
