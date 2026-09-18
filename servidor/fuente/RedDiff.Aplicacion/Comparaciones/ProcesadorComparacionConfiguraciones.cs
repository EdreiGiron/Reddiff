using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace RedDiff.Aplicacion.Comparaciones;

public sealed class ProcesadorComparacionConfiguraciones
{
    public IReadOnlyList<DiferenciaCalculada> Comparar(
        string contenidoAnterior,
        string contenidoNuevo)
    {
        ArgumentNullException.ThrowIfNull(contenidoAnterior);
        ArgumentNullException.ThrowIfNull(contenidoNuevo);

        SideBySideDiffModel modelo = SideBySideDiffBuilder.Diff(
            contenidoAnterior,
            contenidoNuevo,
            ignoreWhiteSpace: false,
            ignoreCase: false);
        List<DiferenciaCalculada> diferencias = [];

        for (int indice = 0; indice < modelo.OldText.Lines.Count; indice++)
        {
            DiffPiece anterior = modelo.OldText.Lines[indice];
            DiffPiece nueva = modelo.NewText.Lines[indice];
            if (anterior.Type == ChangeType.Unchanged && nueva.Type == ChangeType.Unchanged)
            {
                continue;
            }

            int linea = nueva.Position ?? anterior.Position ?? indice + 1;
            diferencias.Add(new DiferenciaCalculada(
                linea,
                anterior.Type == ChangeType.Imaginary ? null : anterior.Text,
                nueva.Type == ChangeType.Imaginary ? null : nueva.Text));
        }

        return diferencias;
    }
}
