namespace RedDiff.Aplicacion.Seguridad.Autenticacion;

public sealed record SesionUsuario(
    long UsuarioId,
    string NombreUsuario,
    string Rol,
    string VersionCredencial);

public sealed record ResultadoAutenticacion(bool Exitoso, SesionUsuario? Sesion)
{
    public static ResultadoAutenticacion Rechazado()
    {
        return new ResultadoAutenticacion(false, null);
    }

    public static ResultadoAutenticacion Aceptado(SesionUsuario sesion)
    {
        return new ResultadoAutenticacion(true, sesion);
    }
}
