using RedDiff.Aplicacion.Abstracciones.Persistencia;

namespace RedDiff.PruebasUnitarias;

public sealed class UnidadDeTrabajoContratoPruebas
{
    [Fact]
    public void Contrato_DebeExponerGuardadoCancelable()
    {
        Type tipoContrato = typeof(IUnidadDeTrabajo);

        System.Reflection.MethodInfo? metodo = tipoContrato.GetMethod(
            nameof(IUnidadDeTrabajo.GuardarCambiosAsync));

        Assert.NotNull(metodo);
        Assert.Equal(typeof(Task<int>), metodo.ReturnType);
        Assert.Contains(
            metodo.GetParameters(),
            parametro => parametro.ParameterType == typeof(CancellationToken));
    }
}
