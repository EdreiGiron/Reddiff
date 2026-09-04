using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Capturas;

public sealed class Captura
{
    private Captura()
    {
    }

    private Captura(
        long dispositivoId,
        long? eventoCambioId,
        long? usuarioSolicitanteId,
        DisparadorCaptura disparador,
        MedioCaptura medio,
        DateTimeOffset fecha)
    {
        DispositivoId = ValidacionDominio.Identificador(dispositivoId, nameof(dispositivoId));
        EventoCambioId = eventoCambioId;
        UsuarioSolicitanteId = usuarioSolicitanteId;
        Disparador = disparador;
        Medio = medio;
        Fecha = ValidacionDominio.FechaUtc(fecha, nameof(fecha));
        Estado = EstadoCaptura.Pendiente;
    }

    public long Id { get; private set; }

    public long DispositivoId { get; private set; }

    public long? EventoCambioId { get; private set; }

    public long? UsuarioSolicitanteId { get; private set; }

    public DisparadorCaptura Disparador { get; private set; }

    public MedioCaptura Medio { get; private set; }

    public EstadoCaptura Estado { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public string? Error { get; private set; }

    public Dispositivo Dispositivo { get; private set; } = null!;

    public EventoCambio? EventoCambio { get; private set; }

    public Usuario? UsuarioSolicitante { get; private set; }

    public VersionConfiguracion? VersionConfiguracion { get; private set; }

    public static Captura CrearBajoDemanda(
        long dispositivoId,
        long usuarioSolicitanteId,
        MedioCaptura medio,
        DateTimeOffset fecha)
    {
        ValidacionDominio.Identificador(usuarioSolicitanteId, nameof(usuarioSolicitanteId));

        return new Captura(
            dispositivoId,
            null,
            usuarioSolicitanteId,
            DisparadorCaptura.BajoDemanda,
            medio,
            fecha);
    }

    public static Captura CrearPorEvento(
        long dispositivoId,
        long eventoCambioId,
        MedioCaptura medio,
        DateTimeOffset fecha)
    {
        ValidacionDominio.Identificador(eventoCambioId, nameof(eventoCambioId));

        if (medio == MedioCaptura.Archivo)
        {
            throw new ArgumentException(
                "Una captura iniciada por evento debe utilizar SSH o NETCONF.",
                nameof(medio));
        }

        return new Captura(
            dispositivoId,
            eventoCambioId,
            null,
            DisparadorCaptura.Evento,
            medio,
            fecha);
    }

    public void Iniciar()
    {
        if (Estado != EstadoCaptura.Pendiente)
        {
            throw new InvalidOperationException("Solo una captura pendiente puede iniciarse.");
        }

        Estado = EstadoCaptura.EnProceso;
    }

    public void Completar()
    {
        if (Estado != EstadoCaptura.EnProceso)
        {
            throw new InvalidOperationException("Solo una captura en proceso puede completarse.");
        }

        Estado = EstadoCaptura.Completada;
        Error = null;
    }

    public void Fallar(string error)
    {
        if (Estado is EstadoCaptura.Completada or EstadoCaptura.Cancelada)
        {
            throw new InvalidOperationException("La captura ya se encuentra finalizada.");
        }

        Error = ValidacionDominio.TextoObligatorio(error, nameof(error), 2_000);
        Estado = EstadoCaptura.Fallida;
    }

    public void Cancelar()
    {
        if (Estado == EstadoCaptura.Completada)
        {
            throw new InvalidOperationException("Una captura completada no puede cancelarse.");
        }

        Estado = EstadoCaptura.Cancelada;
    }
}
