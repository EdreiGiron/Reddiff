namespace RedDiff.Aplicacion.Abstracciones.Persistencia;

/// <summary>
/// Confirma de forma atómica los cambios realizados por un caso de uso.
/// </summary>
public interface IUnidadDeTrabajo
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
