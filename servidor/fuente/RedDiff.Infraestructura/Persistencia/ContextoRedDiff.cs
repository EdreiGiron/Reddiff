using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;

namespace RedDiff.Infraestructura.Persistencia;

public sealed class ContextoRedDiff(DbContextOptions<ContextoRedDiff> opciones)
    : DbContext(opciones), IUnidadDeTrabajo
{
    protected override void OnModelCreating(ModelBuilder constructorModelo)
    {
        ArgumentNullException.ThrowIfNull(constructorModelo);

        constructorModelo.HasDefaultSchema("reddiff");
        constructorModelo.ApplyConfigurationsFromAssembly(
            typeof(MarcadorEnsambladoInfraestructura).Assembly);

        base.OnModelCreating(constructorModelo);
    }

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(cancellationToken);
    }
}
