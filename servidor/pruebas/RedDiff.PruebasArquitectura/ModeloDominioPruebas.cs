using System.Reflection;
using RedDiff.Dominio.Entidades.Capturas;

namespace RedDiff.PruebasArquitectura;

public sealed class ModeloDominioPruebas
{
    [Fact]
    public void Entidades_DebenSerSelladasYNoExponerSettersPublicos()
    {
        Type[] entidades = typeof(Captura).Assembly
            .GetTypes()
            .Where(tipo => tipo.Namespace?.StartsWith(
                "RedDiff.Dominio.Entidades.",
                StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Equal(13, entidades.Length);
        Assert.All(entidades, entidad => Assert.True(entidad.IsSealed));

        PropertyInfo[] propiedadesConSetterPublico = entidades
            .SelectMany(entidad => entidad.GetProperties())
            .Where(propiedad => propiedad.SetMethod?.IsPublic == true)
            .ToArray();

        Assert.Empty(propiedadesConSetterPublico);
    }
}
