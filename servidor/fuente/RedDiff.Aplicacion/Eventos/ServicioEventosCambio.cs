using System.Net;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Capturas;
using RedDiff.Aplicacion.Comun;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Eventos;

public sealed class ServicioEventosCambio(
    IRepositorioEventosCambio repositorioEventos,
    IRepositorioDispositivos repositorioDispositivos,
    IRepositorioAuditorias repositorioAuditorias,
    IUnidadDeTrabajo unidadDeTrabajo,
    ProcesadorEventoSyslog procesador,
    ServicioCapturaRemota servicioCaptura,
    TimeProvider reloj)
{
    private const string AccionRecepcion = "RecibirEventoSyslog";

    public async Task<ResultadoOperacion<RecepcionEventoResultado>> RecibirSyslogAsync(
        string direccionOrigen,
        ReadOnlyMemory<byte> datos,
        CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(direccionOrigen, out IPAddress? direccion))
        {
            return await RechazarSinEventoAsync(
                "La dirección de origen del evento no es válida.",
                cancellationToken);
        }

        string host = direccion.IsIPv4MappedToIPv6
            ? direccion.MapToIPv4().ToString()
            : direccion.ToString();
        IReadOnlyList<Dispositivo> coincidencias = await repositorioDispositivos
            .ListarPorHostAsync(host, cancellationToken);

        if (coincidencias.Count == 0)
        {
            return await RechazarSinEventoAsync(
                $"Se rechazó un evento Syslog procedente de un host no registrado: {host}.",
                cancellationToken);
        }

        if (coincidencias.Count > 1)
        {
            return await RechazarSinEventoAsync(
                $"No fue posible asociar de forma inequívoca el evento procedente de {host}.",
                cancellationToken);
        }

        Dispositivo dispositivo = coincidencias[0];
        if (dispositivo.Estado != EstadoDispositivo.Autorizado
            || dispositivo.FuenteEventos != FuenteEvento.Syslog)
        {
            return await RechazarAsync(
                dispositivo,
                "El dispositivo no está autorizado para originar eventos Syslog.",
                cancellationToken);
        }

        if (!dispositivo.AccesoRemotoConfigurado)
        {
            return await RechazarAsync(
                dispositivo,
                "El dispositivo no tiene acceso remoto configurado para generar una captura.",
                cancellationToken);
        }

        ResultadoOperacion<EventoSyslogProcesado> procesamiento = procesador.Procesar(datos);
        if (!procesamiento.Exitoso)
        {
            return await RechazarAsync(
                dispositivo,
                procesamiento.Error!.Mensaje,
                cancellationToken);
        }

        EventoSyslogProcesado contenido = procesamiento.Valor!;
        EventoCambio? existente = await repositorioEventos.ObtenerPorHuellaAsync(
            dispositivo.Id,
            contenido.Huella,
            cancellationToken);
        if (existente is not null)
        {
            RegistrarAuditoria(
                existente.Id,
                EstadoAuditoria.Exitoso,
                $"Se ignoró un evento Syslog duplicado de {dispositivo.Nombre}.");
            await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
            return ResultadoOperacion<RecepcionEventoResultado>.Correcto(
                new RecepcionEventoResultado(
                    Mapear(existente),
                    Duplicado: true,
                    CapturaRealizada: existente.Captura is not null,
                    VersionGenerada: existente.Captura?.VersionConfiguracion is not null));
        }

        EventoCambio evento = new(
            dispositivo.Id,
            host,
            FuenteEvento.Syslog,
            reloj.GetUtcNow(),
            contenido.Huella,
            contenido.Contenido);
        evento.MarcarValidado();
        evento.MarcarEncolado();
        repositorioEventos.Agregar(evento);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        ResultadoOperacion<CapturaPorEventoResultado> captura;
        try
        {
            captura = await servicioCaptura.CapturarPorEventoAsync(
                evento,
                dispositivo,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            evento.MarcarFallido();
            await unidadDeTrabajo.GuardarCambiosAsync(CancellationToken.None);
            throw;
        }

        if (!captura.Exitoso)
        {
            evento.MarcarFallido();
            RegistrarAuditoria(
                evento.Id,
                EstadoAuditoria.Fallido,
                $"El evento de {dispositivo.Nombre} no pudo generar una versión: "
                    + captura.Error!.Mensaje);
            await unidadDeTrabajo.GuardarCambiosAsync(CancellationToken.None);
            return ResultadoOperacion<RecepcionEventoResultado>.Correcto(
                new RecepcionEventoResultado(
                    Mapear(evento, dispositivo.Nombre),
                    Duplicado: false,
                    CapturaRealizada: false,
                    VersionGenerada: false));
        }

        if (!captura.Valor!.CambioDetectado)
        {
            evento.MarcarSinCambios();
            RegistrarAuditoria(
                evento.Id,
                EstadoAuditoria.Exitoso,
                $"El evento Syslog de {dispositivo.Nombre} originó una captura, pero no una "
                    + "versión porque la configuración no cambió.");
            await unidadDeTrabajo.GuardarCambiosAsync(CancellationToken.None);

            return ResultadoOperacion<RecepcionEventoResultado>.Correcto(
                new RecepcionEventoResultado(
                    Mapear(evento, dispositivo.Nombre),
                    Duplicado: false,
                    CapturaRealizada: true,
                    VersionGenerada: false));
        }

        evento.MarcarProcesado();
        RegistrarAuditoria(
            evento.Id,
            EstadoAuditoria.Exitoso,
            $"El evento Syslog de {dispositivo.Nombre} generó la captura "
                + $"{captura.Valor.Captura.Id} y la versión {captura.Valor.Version!.Numero}.");
        await unidadDeTrabajo.GuardarCambiosAsync(CancellationToken.None);

        return ResultadoOperacion<RecepcionEventoResultado>.Correcto(
            new RecepcionEventoResultado(
                Mapear(evento, dispositivo.Nombre),
                Duplicado: false,
                CapturaRealizada: true,
                VersionGenerada: true));
    }

    public async Task<ResultadoOperacion<IReadOnlyList<EventoCambioResumen>>> ListarAsync(
        long? dispositivoId,
        string? estado,
        int limite,
        CancellationToken cancellationToken = default)
    {
        if (dispositivoId is <= 0)
        {
            return ResultadoOperacion<IReadOnlyList<EventoCambioResumen>>.Fallido(
                CodigosErrorOperacion.Validacion,
                "El filtro de dispositivo no es válido.");
        }

        if (limite is < 1 or > 500)
        {
            return ResultadoOperacion<IReadOnlyList<EventoCambioResumen>>.Fallido(
                CodigosErrorOperacion.Validacion,
                "El límite debe estar entre 1 y 500 eventos.");
        }

        EstadoEventoCambio? estadoInterpretado = null;
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse(estado, true, out EstadoEventoCambio valor)
                || !Enum.IsDefined(valor))
            {
                return ResultadoOperacion<IReadOnlyList<EventoCambioResumen>>.Fallido(
                    CodigosErrorOperacion.Validacion,
                    "El estado de evento solicitado no es válido.");
            }

            estadoInterpretado = valor;
        }

        IReadOnlyList<EventoCambio> eventos = await repositorioEventos.ListarAsync(
            dispositivoId,
            estadoInterpretado,
            limite,
            cancellationToken);
        return ResultadoOperacion<IReadOnlyList<EventoCambioResumen>>.Correcto(
            eventos.Select(Mapear).ToArray());
    }

    private async Task<ResultadoOperacion<RecepcionEventoResultado>> RechazarSinEventoAsync(
        string mensaje,
        CancellationToken cancellationToken)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            null,
            AccionRecepcion,
            "EventoCambio",
            null,
            reloj.GetUtcNow(),
            EstadoAuditoria.Fallido,
            mensaje));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return ResultadoOperacion<RecepcionEventoResultado>.Fallido(
            CodigosErrorOperacion.Prohibido,
            mensaje);
    }

    private async Task<ResultadoOperacion<RecepcionEventoResultado>> RechazarAsync(
        Dispositivo dispositivo,
        string mensaje,
        CancellationToken cancellationToken)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            null,
            AccionRecepcion,
            "Dispositivo",
            dispositivo.Id,
            reloj.GetUtcNow(),
            EstadoAuditoria.Fallido,
            mensaje));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return ResultadoOperacion<RecepcionEventoResultado>.Fallido(
            CodigosErrorOperacion.Prohibido,
            mensaje);
    }

    private void RegistrarAuditoria(long eventoId, EstadoAuditoria estado, string detalle)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            null,
            AccionRecepcion,
            "EventoCambio",
            eventoId,
            reloj.GetUtcNow(),
            estado,
            detalle));
    }

    private static EventoCambioResumen Mapear(EventoCambio evento)
    {
        return Mapear(evento, evento.Dispositivo?.Nombre ?? string.Empty);
    }

    private static EventoCambioResumen Mapear(EventoCambio evento, string dispositivo)
    {
        string resumen = evento.ContenidoEvento.Replace('\n', ' ');
        if (resumen.Length > 240)
        {
            resumen = $"{resumen[..239]}…";
        }

        return new EventoCambioResumen(
            evento.Id,
            evento.DispositivoId,
            dispositivo,
            evento.Fuente,
            evento.Tipo.ToString(),
            evento.Fecha,
            evento.Huella,
            evento.Estado.ToString(),
            resumen,
            evento.Captura?.Id,
            evento.Captura?.VersionConfiguracion?.Id);
    }
}
