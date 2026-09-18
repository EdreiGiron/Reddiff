using System.Text.RegularExpressions;
using RedDiff.Dominio.Entidades.Cumplimiento;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Cumplimiento;

public sealed class EvaluadorReglasBaseline
{
    private static readonly TimeSpan TiempoMaximoExpresion = TimeSpan.FromMilliseconds(500);

    public ResultadoEvaluacionRegla Evaluar(ReglaBaseline regla, string contenido)
    {
        ArgumentNullException.ThrowIfNull(regla);
        ArgumentNullException.ThrowIfNull(contenido);

        return regla.Criterio switch
        {
            TipoCriterioRegla.Contiene => EvaluarContiene(regla.Esperado, contenido),
            TipoCriterioRegla.NoContiene => EvaluarNoContiene(regla.Esperado, contenido),
            TipoCriterioRegla.CoincideExpresionRegular => EvaluarExpresion(
                regla.Esperado,
                contenido),
            _ => NoEvaluable("El tipo de criterio no es compatible con el evaluador.")
        };
    }

    private static ResultadoEvaluacionRegla EvaluarContiene(
        string esperado,
        string contenido)
    {
        bool encontrado = contenido.Contains(esperado, StringComparison.Ordinal);
        return encontrado
            ? Cumplida("El contenido esperado está presente en la versión.")
            : Incumplida("El contenido obligatorio no está presente en la versión.");
    }

    private static ResultadoEvaluacionRegla EvaluarNoContiene(
        string esperado,
        string contenido)
    {
        bool encontrado = contenido.Contains(esperado, StringComparison.Ordinal);
        return encontrado
            ? Incumplida("Se encontró contenido que la línea base prohíbe.")
            : Cumplida("No se encontró el contenido prohibido.");
    }

    private static ResultadoEvaluacionRegla EvaluarExpresion(
        string expresion,
        string contenido)
    {
        try
        {
            bool coincide = Regex.IsMatch(
                contenido,
                expresion,
                RegexOptions.CultureInvariant | RegexOptions.Multiline,
                TiempoMaximoExpresion);
            return coincide
                ? Cumplida("La configuración coincide con la expresión esperada.")
                : Incumplida("La configuración no coincide con la expresión esperada.");
        }
        catch (ArgumentException)
        {
            return NoEvaluable("La expresión regular configurada no es válida.");
        }
        catch (RegexMatchTimeoutException)
        {
            return NoEvaluable("La expresión regular superó el tiempo máximo de evaluación.");
        }
    }

    private static ResultadoEvaluacionRegla Cumplida(string evidencia)
    {
        return new ResultadoEvaluacionRegla(
            EstadoResultadoRegla.Cumplida.ToString(),
            evidencia);
    }

    private static ResultadoEvaluacionRegla Incumplida(string evidencia)
    {
        return new ResultadoEvaluacionRegla(
            EstadoResultadoRegla.Incumplida.ToString(),
            evidencia);
    }

    private static ResultadoEvaluacionRegla NoEvaluable(string evidencia)
    {
        return new ResultadoEvaluacionRegla(
            EstadoResultadoRegla.NoEvaluable.ToString(),
            evidencia);
    }
}
