using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Comparaciones;
using RedDiff.Dominio.Entidades.Cumplimiento;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Identidad;

public sealed class Usuario
{
    private Usuario()
    {
    }

    public Usuario(long rolId, string nombreUsuario, string contrasenaHash)
    {
        RolId = ValidacionDominio.Identificador(rolId, nameof(rolId));
        NombreUsuario = NormalizarNombreUsuario(nombreUsuario);
        ContrasenaHash = ValidacionDominio.TextoObligatorio(
            contrasenaHash,
            nameof(contrasenaHash),
            512);
        Estado = true;
    }

    public long Id { get; private set; }

    public long RolId { get; private set; }

    public string NombreUsuario { get; private set; } = string.Empty;

    public string ContrasenaHash { get; private set; } = string.Empty;

    public bool Estado { get; private set; }

    public Rol Rol { get; private set; } = null!;

    public ICollection<Captura> CapturasSolicitadas { get; } = [];

    public ICollection<VersionConfiguracion> VersionesValidadas { get; } = [];

    public ICollection<Comparacion> Comparaciones { get; } = [];

    public ICollection<Verificacion> Verificaciones { get; } = [];

    public ICollection<Auditoria> Auditorias { get; } = [];

    public void CambiarRol(long rolId)
    {
        RolId = ValidacionDominio.Identificador(rolId, nameof(rolId));
    }

    public void CambiarNombreUsuario(string nombreUsuario)
    {
        NombreUsuario = NormalizarNombreUsuario(nombreUsuario);
    }

    public void ActualizarContrasenaHash(string contrasenaHash)
    {
        ContrasenaHash = ValidacionDominio.TextoObligatorio(
            contrasenaHash,
            nameof(contrasenaHash),
            512);
    }

    public void Activar()
    {
        Estado = true;
    }

    public void Desactivar()
    {
        Estado = false;
    }

    public static string NormalizarNombreUsuario(string nombreUsuario)
    {
        string nombreNormalizado = ValidacionDominio.TextoObligatorio(
            nombreUsuario,
            nameof(nombreUsuario),
            100).ToLowerInvariant();

        if (nombreNormalizado.Length < 3)
        {
            throw new ArgumentException(
                "El nombre de usuario debe contener al menos 3 caracteres.",
                nameof(nombreUsuario));
        }

        if (nombreNormalizado.Any(static caracter =>
                !char.IsLetterOrDigit(caracter)
                && caracter is not '.' and not '_' and not '-'))
        {
            throw new ArgumentException(
                "El nombre de usuario solo admite letras, números, punto, guion y guion bajo.",
                nameof(nombreUsuario));
        }

        return nombreNormalizado;
    }
}
