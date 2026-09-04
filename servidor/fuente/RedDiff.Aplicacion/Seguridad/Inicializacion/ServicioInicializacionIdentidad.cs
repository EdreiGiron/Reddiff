using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.Aplicacion.Comun;
using RedDiff.Aplicacion.Seguridad.Autenticacion;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Seguridad.Inicializacion;

public sealed class ServicioInicializacionIdentidad(
    IRepositorioUsuarios repositorioUsuarios,
    IRepositorioRoles repositorioRoles,
    IRepositorioAuditorias repositorioAuditorias,
    IProtectorContrasena protectorContrasena,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj)
{
    public async Task<ResultadoOperacion<SesionUsuario>> InicializarAsync(
        string nombreUsuario,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        if (await repositorioUsuarios.ExisteAlgunoAsync(cancellationToken))
        {
            return ResultadoOperacion<SesionUsuario>.Fallido(
                CodigosErrorOperacion.Conflicto,
                "La identidad ya fue inicializada; administre las cuentas mediante la API.");
        }

        string nombreNormalizado;
        string hash;
        try
        {
            nombreNormalizado = Usuario.NormalizarNombreUsuario(nombreUsuario);
            hash = protectorContrasena.CrearHash(contrasena);
        }
        catch (ArgumentException excepcion)
        {
            return ResultadoOperacion<SesionUsuario>.Fallido(
                CodigosErrorOperacion.Validacion,
                excepcion.Message);
        }

        Rol administrador = await ObtenerOCrearRolAsync(
            RolesSistema.Administrador,
            cancellationToken);
        _ = await ObtenerOCrearRolAsync(RolesSistema.Tecnico, cancellationToken);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        Usuario usuario = new(administrador.Id, nombreNormalizado, hash);
        repositorioUsuarios.Agregar(usuario);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        repositorioAuditorias.Agregar(new Auditoria(
            usuario.Id,
            "InicializarIdentidad",
            "Usuario",
            usuario.Id,
            reloj.GetUtcNow(),
            EstadoAuditoria.Exitoso,
            "Se creó la primera cuenta administrativa y el catálogo funcional de roles."));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<SesionUsuario>.Correcto(new SesionUsuario(
            usuario.Id,
            usuario.NombreUsuario,
            administrador.Nombre,
            VersionCredencial.Calcular(usuario.ContrasenaHash)));
    }

    private async Task<Rol> ObtenerOCrearRolAsync(
        string nombre,
        CancellationToken cancellationToken)
    {
        Rol? rol = await repositorioRoles.ObtenerPorNombreAsync(
            nombre,
            true,
            cancellationToken);

        if (rol is null)
        {
            rol = new Rol(nombre);
            repositorioRoles.Agregar(rol);
        }
        else if (!rol.Estado)
        {
            rol.Activar();
        }

        return rol;
    }
}
