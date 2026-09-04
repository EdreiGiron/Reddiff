using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Cumplimiento;

public sealed class ReglaBaseline
{
    private ReglaBaseline()
    {
    }

    public ReglaBaseline(
        long baselineId,
        TipoCriterioRegla criterio,
        string esperado,
        bool obligatoria)
    {
        BaselineId = ValidacionDominio.Identificador(baselineId, nameof(baselineId));
        Criterio = criterio;
        Esperado = ValidacionDominio.ContenidoObligatorio(esperado, nameof(esperado), 10_000);
        Obligatoria = obligatoria;
    }

    public long Id { get; private set; }

    public long BaselineId { get; private set; }

    public TipoCriterioRegla Criterio { get; private set; }

    public string Esperado { get; private set; } = string.Empty;

    public bool Obligatoria { get; private set; }

    public Baseline Baseline { get; private set; } = null!;

    public ICollection<ResultadoRegla> Resultados { get; } = [];

    public void Actualizar(
        TipoCriterioRegla criterio,
        string esperado,
        bool obligatoria)
    {
        Criterio = criterio;
        Esperado = ValidacionDominio.ContenidoObligatorio(esperado, nameof(esperado), 10_000);
        Obligatoria = obligatoria;
    }
}
