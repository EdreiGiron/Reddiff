using Microsoft.EntityFrameworkCore;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Comparaciones;
using RedDiff.Dominio.Entidades.Cumplimiento;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Entidades.Trazabilidad;

namespace RedDiff.Infraestructura.Persistencia;

public sealed class ContextoRedDiff(DbContextOptions<ContextoRedDiff> opciones)
    : DbContext(opciones), IUnidadDeTrabajo
{
    public DbSet<Rol> Roles => Set<Rol>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();

    public DbSet<EventoCambio> EventosCambio => Set<EventoCambio>();

    public DbSet<Captura> Capturas => Set<Captura>();

    public DbSet<VersionConfiguracion> VersionesConfiguracion => Set<VersionConfiguracion>();

    public DbSet<Baseline> Baselines => Set<Baseline>();

    public DbSet<ReglaBaseline> ReglasBaseline => Set<ReglaBaseline>();

    public DbSet<Comparacion> Comparaciones => Set<Comparacion>();

    public DbSet<DetalleDiferencia> DetallesDiferencia => Set<DetalleDiferencia>();

    public DbSet<Verificacion> Verificaciones => Set<Verificacion>();

    public DbSet<ResultadoRegla> ResultadosRegla => Set<ResultadoRegla>();

    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

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
