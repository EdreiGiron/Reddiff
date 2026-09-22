using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedDiff.Aplicacion.Eventos;

namespace RedDiff.Infraestructura.Red.Syslog;

internal sealed class ReceptorSyslogUdp(
    IOptions<OpcionesReceptorSyslog> opciones,
    IServiceScopeFactory fabricaAlcances,
    ILogger<ReceptorSyslogUdp> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OpcionesReceptorSyslog configuracion = opciones.Value;
        if (!configuracion.Habilitado)
        {
            logger.LogInformation("El receptor Syslog UDP está deshabilitado.");
            return;
        }

        IPAddress direccion = IPAddress.Parse(configuracion.DireccionEscucha);
        using UdpClient receptor = new(new IPEndPoint(direccion, configuracion.Puerto));
        logger.LogInformation(
            "Receptor Syslog UDP activo en {Direccion}:{Puerto}.",
            direccion,
            configuracion.Puerto);

        while (!stoppingToken.IsCancellationRequested)
        {
            UdpReceiveResult datagrama;
            try
            {
                datagrama = await receptor.ReceiveAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (datagrama.Buffer.Length > ProcesadorEventoSyslog.TamanoMaximoBytes)
            {
                logger.LogWarning(
                    "Se rechazó un datagrama Syslog de {Origen} porque supera el límite permitido.",
                    datagrama.RemoteEndPoint.Address);
                continue;
            }

            await ProcesarAsync(datagrama, stoppingToken);
        }
    }

    private async Task ProcesarAsync(
        UdpReceiveResult datagrama,
        CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope alcance = fabricaAlcances.CreateScope();
            ServicioEventosCambio servicio = alcance.ServiceProvider
                .GetRequiredService<ServicioEventosCambio>();
            var resultado = await servicio.RecibirSyslogAsync(
                datagrama.RemoteEndPoint.Address.ToString(),
                datagrama.Buffer,
                cancellationToken);

            if (!resultado.Exitoso)
            {
                logger.LogWarning(
                    "Evento Syslog rechazado desde {Origen}: {Motivo}",
                    datagrama.RemoteEndPoint.Address,
                    resultado.Error!.Mensaje);
                return;
            }

            logger.LogInformation(
                "Evento Syslog {EventoId} procesado desde {Origen}; duplicado: {Duplicado}; "
                    + "captura: {Captura}; versión: {Version}.",
                resultado.Valor!.Evento.Id,
                datagrama.RemoteEndPoint.Address,
                resultado.Valor.Duplicado,
                resultado.Valor.CapturaRealizada,
                resultado.Valor.VersionGenerada);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            logger.LogError(
                excepcion,
                "Falló el procesamiento de un evento Syslog procedente de {Origen}.",
                datagrama.RemoteEndPoint.Address);
        }
    }
}
