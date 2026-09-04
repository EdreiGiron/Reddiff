namespace RedDiff.Aplicacion.Seguridad.Usuarios;

public sealed record CrearUsuarioSolicitud(
    string NombreUsuario,
    long RolId,
    string Contrasena);

public sealed record ActualizarUsuarioSolicitud(
    string NombreUsuario,
    long RolId,
    string? NuevaContrasena);

public sealed record CambiarEstadoUsuarioSolicitud(bool Activo);

public sealed record UsuarioResumen(
    long Id,
    string NombreUsuario,
    long RolId,
    string Rol,
    bool Activo);

public sealed record RolResumen(long Id, string Nombre, bool Activo);
