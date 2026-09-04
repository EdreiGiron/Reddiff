using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RedDiff.Infraestructura.Persistencia;

namespace RedDiff.PruebasIntegracion.Infraestructura;

public sealed class ModeloPersistenciaPruebas
{
    private static readonly string[] TablasEsperadas =
    [
        "auditoria",
        "baseline",
        "captura",
        "comparacion",
        "detalle_diferencia",
        "dispositivo",
        "evento_cambio",
        "regla_baseline",
        "resultado_regla",
        "rol",
        "usuario",
        "verificacion",
        "version_config"
    ];

    [Fact]
    public void Modelo_ContieneLasTreceEntidadesDelDocumento()
    {
        using ContextoRedDiff contexto = CrearContexto();

        string[] tablas = contexto.Model
            .GetEntityTypes()
            .Select(entidad => entidad.GetTableName())
            .Where(nombre => nombre is not null)
            .Select(nombre => nombre!)
            .Order()
            .ToArray();

        Assert.Equal("reddiff", contexto.Model.GetDefaultSchema());
        Assert.Equal(TablasEsperadas, tablas);
    }

    [Fact]
    public void Modelo_NoPermiteEliminacionesEnCascada()
    {
        using ContextoRedDiff contexto = CrearContexto();

        IReadOnlyList<IForeignKey> relaciones = contexto.Model
            .GetEntityTypes()
            .SelectMany(entidad => entidad.GetForeignKeys())
            .ToArray();

        Assert.NotEmpty(relaciones);
        Assert.All(
            relaciones,
            relacion => Assert.Equal(DeleteBehavior.Restrict, relacion.DeleteBehavior));
    }

    [Fact]
    public void Modelo_UsaSnakeCaseEnLosNombresDeIndices()
    {
        using ContextoRedDiff contexto = CrearContexto();

        string[] nombres = contexto.Model
            .GetEntityTypes()
            .SelectMany(entidad => entidad.GetIndexes())
            .Select(indice => indice.GetDatabaseName())
            .Where(nombre => nombre is not null)
            .Select(nombre => nombre!)
            .ToArray();

        Assert.NotEmpty(nombres);
        Assert.All(nombres, nombre => Assert.Matches("^[a-z][a-z0-9_]*$", nombre));
    }

    [Fact]
    public void Modelo_CubreLosFiltrosRequeridosParaElHistorial()
    {
        using ContextoRedDiff contexto = CrearContexto();

        string[] nombres = contexto.Model
            .GetEntityTypes()
            .SelectMany(entidad => entidad.GetIndexes())
            .Select(indice => indice.GetDatabaseName())
            .Where(nombre => nombre is not null)
            .Select(nombre => nombre!)
            .ToArray();

        Assert.Contains("ix_version_config_dispositivo_fecha", nombres);
        Assert.Contains("ix_version_config_dispositivo_origen_fecha", nombres);
        Assert.Contains("ix_version_config_dispositivo_estado_fecha", nombres);
        Assert.Contains("ix_captura_dispositivo_disparador_fecha", nombres);
    }

    private static ContextoRedDiff CrearContexto()
    {
        DbContextOptions<ContextoRedDiff> opciones =
            new DbContextOptionsBuilder<ContextoRedDiff>()
                .UseNpgsql(
                    "Host=localhost;Port=5432;Database=reddiff_modelo_pruebas;"
                    + "Username=reddiff_app")
                .Options;

        return new ContextoRedDiff(opciones);
    }
}
