namespace RedDiff.Aplicacion.Abstracciones.Seguridad;

public interface IProtectorContrasena
{
    string CrearHash(string contrasena);

    bool Verificar(string contrasena, string? hashAlmacenado);
}
