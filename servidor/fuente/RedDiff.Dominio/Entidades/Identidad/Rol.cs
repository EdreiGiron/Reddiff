using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Identidad;

public sealed class Rol
{
    private Rol()
    {
    }

    public Rol(string nombre)
    {
        Nombre = ValidacionDominio.TextoObligatorio(nombre, nameof(nombre), 80);
        Estado = true;
    }

    public long Id { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public bool Estado { get; private set; }

    public ICollection<Usuario> Usuarios { get; } = [];

    public void CambiarNombre(string nombre)
    {
        Nombre = ValidacionDominio.TextoObligatorio(nombre, nameof(nombre), 80);
    }

    public void Activar()
    {
        Estado = true;
    }

    public void Desactivar()
    {
        Estado = false;
    }
}
