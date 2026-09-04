using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Seguridad.Autenticacion;

public sealed class ServicioAutenticacion(
    IRepositorioUsuarios repositorioUsuarios,
    IRepositorioAuditorias repositorioAuditorias,
    IProtectorContrasena protectorContrasena,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj)
{
    public async Task<ResultadoAutenticacion> IniciarSesionAsync(
        string? nombreUsuario,
        string? contrasena,
        CancellationToken cancellationToken = default)
    {
        Usuario? usuario = null;
        string? nombreNormalizado = NormalizarSinExcepcion(nombreUsuario);

        if (nombreNormalizado is not null)
        {
            usuario = await repositorioUsuarios.ObtenerPorNombreAsync(
                nombreNormalizado,
                false,
                cancellationToken);
        }

        string contrasenaEvaluada = contrasena is { Length: >= 1 and <= 128 }
            ? contrasena
            : string.Empty;

        bool contrasenaValida = protectorContrasena.Verificar(
            contrasenaEvaluada,
            usuario?.ContrasenaHash);

        bool accesoPermitido = usuario is not null
            && usuario.Estado
            && usuario.Rol.Estado
            && contrasenaValida;

        repositorioAuditorias.Agregar(new Auditoria(
            usuario?.Id,
            "AutenticarUsuario",
            "Usuario",
            usuario?.Id,
            reloj.GetUtcNow(),
            accesoPermitido ? EstadoAuditoria.Exitoso : EstadoAuditoria.Fallido,
            accesoPermitido ? "Acceso concedido." : "Credenciales rechazadas."));

        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        if (!accesoPermitido)
        {
            return ResultadoAutenticacion.Rechazado();
        }

        return ResultadoAutenticacion.Aceptado(new SesionUsuario(
            usuario!.Id,
            usuario.NombreUsuario,
            usuario.Rol.Nombre,
            VersionCredencial.Calcular(usuario.ContrasenaHash)));
    }

    public async Task RegistrarCierreSesionAsync(
        long usuarioId,
        CancellationToken cancellationToken = default)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            usuarioId,
            "CerrarSesion",
            "Usuario",
            usuarioId,
            reloj.GetUtcNow(),
            EstadoAuditoria.Exitoso,
            "Sesión finalizada por el usuario."));

        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
    }

    private static string? NormalizarSinExcepcion(string? nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            return null;
        }

        try
        {
            return Usuario.NormalizarNombreUsuario(nombreUsuario);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
