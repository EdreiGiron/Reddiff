using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Cumplimiento;

public sealed class ResultadoRegla
{
    private ResultadoRegla()
    {
    }

    public ResultadoRegla(
        long verificacionId,
        long reglaId,
        EstadoResultadoRegla estado,
        string? evidencia)
    {
        VerificacionId = ValidacionDominio.Identificador(
            verificacionId,
            nameof(verificacionId));
        ReglaId = ValidacionDominio.Identificador(reglaId, nameof(reglaId));
        Estado = estado;
        Evidencia = ValidacionDominio.ContenidoOpcional(evidencia, nameof(evidencia), 20_000);
    }

    public long Id { get; private set; }

    public long VerificacionId { get; private set; }

    public long ReglaId { get; private set; }

    public EstadoResultadoRegla Estado { get; private set; }

    public string? Evidencia { get; private set; }

    public Verificacion Verificacion { get; private set; } = null!;

    public ReglaBaseline Regla { get; private set; } = null!;
}
