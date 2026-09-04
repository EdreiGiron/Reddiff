using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Dominio.Validaciones;

namespace RedDiff.Dominio.Entidades.Capturas;

public sealed class EventoCambio
{
    private EventoCambio()
    {
    }

    public EventoCambio(
        long dispositivoId,
        string fuente,
        FuenteEvento tipo,
        DateTimeOffset fecha,
        string huella,
        string contenidoEvento)
    {
        DispositivoId = ValidacionDominio.Identificador(dispositivoId, nameof(dispositivoId));
        Fuente = ValidacionDominio.TextoObligatorio(fuente, nameof(fuente), 255);
        Tipo = tipo;
        Fecha = ValidacionDominio.FechaUtc(fecha, nameof(fecha));
        Huella = ValidacionDominio.HuellaSha256(huella, nameof(huella));
        ContenidoEvento = ValidacionDominio.ContenidoObligatorio(
            contenidoEvento,
            nameof(contenidoEvento),
            65_535);
        Estado = EstadoEventoCambio.Recibido;
    }

    public long Id { get; private set; }

    public long DispositivoId { get; private set; }

    public string Fuente { get; private set; } = string.Empty;

    public FuenteEvento Tipo { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public string Huella { get; private set; } = string.Empty;

    public string ContenidoEvento { get; private set; } = string.Empty;

    public EstadoEventoCambio Estado { get; private set; }

    public Dispositivo Dispositivo { get; private set; } = null!;

    public Captura? Captura { get; private set; }

    public void MarcarValidado()
    {
        Estado = EstadoEventoCambio.Validado;
    }

    public void MarcarEncolado()
    {
        Estado = EstadoEventoCambio.Encolado;
    }

    public void MarcarProcesado()
    {
        Estado = EstadoEventoCambio.Procesado;
    }

    public void MarcarRechazado()
    {
        Estado = EstadoEventoCambio.Rechazado;
    }

    public void MarcarDuplicado()
    {
        Estado = EstadoEventoCambio.Duplicado;
    }

    public void MarcarFallido()
    {
        Estado = EstadoEventoCambio.Fallido;
    }
}
