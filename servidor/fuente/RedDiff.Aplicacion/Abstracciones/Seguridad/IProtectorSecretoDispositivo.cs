namespace RedDiff.Aplicacion.Abstracciones.Seguridad;

public interface IProtectorSecretoDispositivo
{
    string Proteger(string secreto);

    bool IntentarDesproteger(string? secretoProtegido, out string secreto);
}
