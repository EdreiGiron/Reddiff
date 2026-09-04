using RedDiff.Dominio.Entidades.Identidad;

namespace RedDiff.PruebasUnitarias.Dominio;

public sealed class UsuarioPruebas
{
    [Fact]
    public void Crear_NormalizaNombreUsuario()
    {
        Usuario usuario = new(1, "  Tecnico.Principal  ", "hash-seguro");

        Assert.Equal("tecnico.principal", usuario.NombreUsuario);
        Assert.True(usuario.Estado);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("usuario con espacios")]
    [InlineData("usuario@dominio")]
    public void Crear_RechazaNombreUsuarioInvalido(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Usuario(1, nombre, "hash-seguro"));
    }
}
