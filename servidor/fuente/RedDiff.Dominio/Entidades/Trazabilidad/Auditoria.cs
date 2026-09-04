using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Trazabilidad;

public sealed class Auditoria
{
    private Auditoria()
    {
    }

    public Auditoria(
        long? usuarioId,
        string accion,
        string entidad,
        long? entidadId,
        DateTimeOffset fecha,
        EstadoAuditoria estado,
        string? detalle)
    {
        if (usuarioId.HasValue)
        {
            ValidacionDominio.Identificador(usuarioId.Value, nameof(usuarioId));
        }

        if (entidadId.HasValue)
        {
            ValidacionDominio.Identificador(entidadId.Value, nameof(entidadId));
        }

        UsuarioId = usuarioId;
        Accion = ValidacionDominio.TextoObligatorio(accion, nameof(accion), 120);
        Entidad = ValidacionDominio.TextoObligatorio(entidad, nameof(entidad), 120);
        EntidadId = entidadId;
        Fecha = ValidacionDominio.FechaUtc(fecha, nameof(fecha));
        Estado = estado;
        Detalle = ValidacionDominio.TextoOpcional(detalle, nameof(detalle), 4_000);
    }

    public long Id { get; private set; }

    public long? UsuarioId { get; private set; }

    public string Accion { get; private set; } = string.Empty;

    public string Entidad { get; private set; } = string.Empty;

    public long? EntidadId { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public EstadoAuditoria Estado { get; private set; }

    public string? Detalle { get; private set; }

    public Usuario? Usuario { get; private set; }
}
