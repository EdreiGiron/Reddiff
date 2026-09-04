using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.Aplicacion.Comun;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Seguridad.Usuarios;

public sealed class ServicioGestionUsuarios(
    IRepositorioUsuarios repositorioUsuarios,
    IRepositorioRoles repositorioRoles,
    IRepositorioAuditorias repositorioAuditorias,
    IProtectorContrasena protectorContrasena,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj)
{
    public async Task<IReadOnlyList<UsuarioResumen>> ListarUsuariosAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Usuario> usuarios = await repositorioUsuarios.ListarAsync(cancellationToken);
        return usuarios.Select(usuario => Mapear(usuario)).ToArray();
    }

    public async Task<IReadOnlyList<RolResumen>> ListarRolesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Rol> roles = await repositorioRoles.ListarAsync(cancellationToken);
        return roles.Select(rol => new RolResumen(rol.Id, rol.Nombre, rol.Estado)).ToArray();
    }

    public async Task<ResultadoOperacion<UsuarioResumen>> ObtenerUsuarioAsync(
        long usuarioId,
        CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            false,
            cancellationToken);

        return usuario is null
            ? ResultadoOperacion<UsuarioResumen>.Fallido(
                CodigosErrorOperacion.NoEncontrado,
                "El usuario solicitado no existe.")
            : ResultadoOperacion<UsuarioResumen>.Correcto(Mapear(usuario));
    }

    public async Task<ResultadoOperacion<UsuarioResumen>> CrearUsuarioAsync(
        long administradorId,
        CrearUsuarioSolicitud solicitud,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<Usuario>? autorizacion = await ValidarAdministradorAsync(
            administradorId,
            cancellationToken);
        if (autorizacion is not null)
        {
            return ResultadoOperacion<UsuarioResumen>.Fallido(
                autorizacion.Error!.Codigo,
                autorizacion.Error.Mensaje);
        }

        string nombreNormalizado;
        string hash;
        try
        {
            nombreNormalizado = Usuario.NormalizarNombreUsuario(solicitud.NombreUsuario);
            hash = protectorContrasena.CrearHash(solicitud.Contrasena);
        }
        catch (ArgumentException excepcion)
        {
            return await FallarOperacionAsync(
                administradorId,
                "CrearUsuario",
                null,
                CodigosErrorOperacion.Validacion,
                excepcion.Message,
                cancellationToken);
        }

        if (await repositorioUsuarios.ExisteNombreAsync(
                nombreNormalizado,
                null,
                cancellationToken))
        {
            return await FallarOperacionAsync(
                administradorId,
                "CrearUsuario",
                null,
                CodigosErrorOperacion.Conflicto,
                "El nombre de usuario ya está registrado.",
                cancellationToken);
        }

        Rol? rol = await repositorioRoles.ObtenerPorIdAsync(
            solicitud.RolId,
            false,
            cancellationToken);
        if (rol is null || !rol.Estado)
        {
            return await FallarOperacionAsync(
                administradorId,
                "CrearUsuario",
                null,
                CodigosErrorOperacion.Validacion,
                "El rol indicado no existe o está inactivo.",
                cancellationToken);
        }

        Usuario usuario = new(rol.Id, nombreNormalizado, hash);
        repositorioUsuarios.Agregar(usuario);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        RegistrarAuditoria(
            administradorId,
            "CrearUsuario",
            usuario.Id,
            $"Se creó el usuario {usuario.NombreUsuario} con el rol {rol.Nombre}.");
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<UsuarioResumen>.Correcto(
            Mapear(usuario, rol.Nombre));
    }

    public async Task<ResultadoOperacion<UsuarioResumen>> ActualizarUsuarioAsync(
        long administradorId,
        long usuarioId,
        ActualizarUsuarioSolicitud solicitud,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<Usuario>? autorizacion = await ValidarAdministradorAsync(
            administradorId,
            cancellationToken);
        if (autorizacion is not null)
        {
            return ResultadoOperacion<UsuarioResumen>.Fallido(
                autorizacion.Error!.Codigo,
                autorizacion.Error.Mensaje);
        }

        Usuario? usuario = await repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            true,
            cancellationToken);
        if (usuario is null)
        {
            return await FallarOperacionAsync(
                administradorId,
                "ActualizarUsuario",
                usuarioId,
                CodigosErrorOperacion.NoEncontrado,
                "El usuario solicitado no existe.",
                cancellationToken);
        }

        Rol? rol = await repositorioRoles.ObtenerPorIdAsync(
            solicitud.RolId,
            false,
            cancellationToken);
        if (rol is null || !rol.Estado)
        {
            return await FallarOperacionAsync(
                administradorId,
                "ActualizarUsuario",
                usuarioId,
                CodigosErrorOperacion.Validacion,
                "El rol indicado no existe o está inactivo.",
                cancellationToken);
        }

        if (usuarioId == administradorId
            && !string.Equals(rol.Nombre, RolesSistema.Administrador, StringComparison.Ordinal))
        {
            return await FallarOperacionAsync(
                administradorId,
                "ActualizarUsuario",
                usuarioId,
                CodigosErrorOperacion.Prohibido,
                "No puede retirar su propio rol de administrador.",
                cancellationToken);
        }

        string nombreNormalizado;
        string? nuevoHash = null;
        try
        {
            nombreNormalizado = Usuario.NormalizarNombreUsuario(solicitud.NombreUsuario);
            if (await repositorioUsuarios.ExisteNombreAsync(
                    nombreNormalizado,
                    usuarioId,
                    cancellationToken))
            {
                return await FallarOperacionAsync(
                    administradorId,
                    "ActualizarUsuario",
                    usuarioId,
                    CodigosErrorOperacion.Conflicto,
                    "El nombre de usuario ya está registrado.",
                    cancellationToken);
            }

            if (solicitud.NuevaContrasena is not null)
            {
                nuevoHash = protectorContrasena.CrearHash(solicitud.NuevaContrasena);
            }
        }
        catch (ArgumentException excepcion)
        {
            return await FallarOperacionAsync(
                administradorId,
                "ActualizarUsuario",
                usuarioId,
                CodigosErrorOperacion.Validacion,
                excepcion.Message,
                cancellationToken);
        }

        usuario.CambiarNombreUsuario(nombreNormalizado);
        usuario.CambiarRol(rol.Id);
        if (nuevoHash is not null)
        {
            usuario.ActualizarContrasenaHash(nuevoHash);
        }

        RegistrarAuditoria(
            administradorId,
            "ActualizarUsuario",
            usuario.Id,
            $"Se actualizó el usuario {usuario.NombreUsuario} con el rol {rol.Nombre}.");
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<UsuarioResumen>.Correcto(Mapear(usuario, rol.Nombre));
    }

    public async Task<ResultadoOperacion<UsuarioResumen>> CambiarEstadoAsync(
        long administradorId,
        long usuarioId,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<Usuario>? autorizacion = await ValidarAdministradorAsync(
            administradorId,
            cancellationToken);
        if (autorizacion is not null)
        {
            return ResultadoOperacion<UsuarioResumen>.Fallido(
                autorizacion.Error!.Codigo,
                autorizacion.Error.Mensaje);
        }

        Usuario? usuario = await repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            true,
            cancellationToken);
        if (usuario is null)
        {
            return await FallarOperacionAsync(
                administradorId,
                activo ? "ActivarUsuario" : "DesactivarUsuario",
                usuarioId,
                CodigosErrorOperacion.NoEncontrado,
                "El usuario solicitado no existe.",
                cancellationToken);
        }

        if (usuarioId == administradorId && !activo)
        {
            return await FallarOperacionAsync(
                administradorId,
                "DesactivarUsuario",
                usuarioId,
                CodigosErrorOperacion.Prohibido,
                "No puede desactivar su propia cuenta.",
                cancellationToken);
        }

        if (activo)
        {
            usuario.Activar();
        }
        else
        {
            usuario.Desactivar();
        }

        RegistrarAuditoria(
            administradorId,
            activo ? "ActivarUsuario" : "DesactivarUsuario",
            usuario.Id,
            activo ? "Se activó la cuenta." : "Se desactivó la cuenta.");
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<UsuarioResumen>.Correcto(Mapear(usuario));
    }

    private async Task<ResultadoOperacion<Usuario>?> ValidarAdministradorAsync(
        long administradorId,
        CancellationToken cancellationToken)
    {
        Usuario? administrador = await repositorioUsuarios.ObtenerPorIdAsync(
            administradorId,
            false,
            cancellationToken);

        if (administrador is null
            || !administrador.Estado
            || !administrador.Rol.Estado
            || !string.Equals(
                administrador.Rol.Nombre,
                RolesSistema.Administrador,
                StringComparison.Ordinal))
        {
            return ResultadoOperacion<Usuario>.Fallido(
                CodigosErrorOperacion.Prohibido,
                "La operación requiere un administrador activo.");
        }

        return null;
    }

    private void RegistrarAuditoria(
        long administradorId,
        string accion,
        long usuarioAfectadoId,
        string detalle)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            administradorId,
            accion,
            "Usuario",
            usuarioAfectadoId,
            reloj.GetUtcNow(),
            EstadoAuditoria.Exitoso,
            detalle));
    }

    private async Task<ResultadoOperacion<UsuarioResumen>> FallarOperacionAsync(
        long administradorId,
        string accion,
        long? usuarioAfectadoId,
        string codigo,
        string mensaje,
        CancellationToken cancellationToken)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            administradorId,
            accion,
            "Usuario",
            usuarioAfectadoId,
            reloj.GetUtcNow(),
            EstadoAuditoria.Fallido,
            mensaje));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<UsuarioResumen>.Fallido(codigo, mensaje);
    }

    private static UsuarioResumen Mapear(Usuario usuario, string? nombreRol = null)
    {
        return new UsuarioResumen(
            usuario.Id,
            usuario.NombreUsuario,
            usuario.RolId,
            nombreRol ?? usuario.Rol.Nombre,
            usuario.Estado);
    }
}
