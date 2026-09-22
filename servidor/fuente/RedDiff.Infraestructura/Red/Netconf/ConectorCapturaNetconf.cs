using System.Net.Sockets;
using System.Text;
using System.Xml;
using RedDiff.Aplicacion.Abstracciones.Red;
using RedDiff.Dominio.Enumeraciones;
using RedDiff.Infraestructura.Red.Ssh;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace RedDiff.Infraestructura.Red.Netconf;

internal sealed class ConectorCapturaNetconf : IConectorCapturaRemota
{
    internal const string SolicitudConfiguracionEnEjecucion =
        "<rpc xmlns=\"urn:ietf:params:xml:ns:netconf:base:1.0\" message-id=\"1\">"
        + "<get-config><source><running/></source></get-config></rpc>";

    private const string CapacidadBase10 = "urn:ietf:params:netconf:base:1.0";
    private const string CapacidadBase11 = "urn:ietf:params:netconf:base:1.1";

    private static readonly UTF8Encoding Utf8Estricto = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public ProtocoloConexion Protocolo => ProtocoloConexion.Netconf;

    public async Task<ResultadoConexionRemota> CapturarAsync(
        SolicitudConexionRemota solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        PasswordConnectionInfo informacionConexion = new(
            solicitud.Host,
            solicitud.Puerto,
            solicitud.Usuario,
            solicitud.Secreto)
        {
            Timeout = solicitud.TiempoEspera,
            Encoding = Utf8Estricto
        };

        using NetConfClient cliente = new(informacionConexion)
        {
            AutomaticMessageIdHandling = true,
            KeepAliveInterval = TimeSpan.FromSeconds(5),
            OperationTimeout = solicitud.TiempoEspera
        };

        bool identidadRecibida = false;
        bool identidadRechazada = false;
        cliente.HostKeyReceived += (_, argumentos) =>
        {
            identidadRecibida = true;
            bool coincide = ValidadorIdentidadHostSsh.Coincide(
                solicitud.AlgoritmoClaveHost,
                solicitud.HuellaClaveHostEsperada,
                argumentos.HostKeyName,
                argumentos.HostKey);
            identidadRechazada = !coincide;
            argumentos.CanTrust = coincide;
        };

        Task<XmlDocument>? consultaPendiente = null;
        try
        {
            await cliente.ConnectAsync(cancellationToken);

            if (!identidadRecibida || identidadRechazada || !cliente.IsConnected)
            {
                return ResultadoConexionRemota.Fallido(
                    TipoFalloConexionRemota.IdentidadHostNoCoincide);
            }

            if (!ServidorAdmiteNetconfBase(cliente.ServerCapabilities))
            {
                return ResultadoConexionRemota.Fallido(
                    TipoFalloConexionRemota.RespuestaInvalida);
            }

            consultaPendiente = Task.Run(
                () => cliente.SendReceiveRpc(SolicitudConfiguracionEnEjecucion),
                CancellationToken.None);
            XmlDocument respuesta = await consultaPendiente.WaitAsync(
                solicitud.TiempoEspera,
                cancellationToken);

            return ProcesadorRespuestaNetconf.IntentarExtraerConfiguracion(
                respuesta,
                out string configuracion)
                    ? ResultadoConexionRemota.Correcto(configuracion)
                    : ResultadoConexionRemota.Fallido(
                        TipoFalloConexionRemota.RespuestaInvalida);
        }
        catch (OperationCanceledException)
        {
            Desconectar(cliente);
            ObservarTareaPendiente(consultaPendiente);
            throw;
        }
        catch (SshAuthenticationException)
        {
            return ResultadoConexionRemota.Fallido(
                TipoFalloConexionRemota.AutenticacionRechazada);
        }
        catch (SshOperationTimeoutException)
        {
            return ResultadoConexionRemota.Fallido(TipoFalloConexionRemota.TiempoAgotado);
        }
        catch (TimeoutException)
        {
            Desconectar(cliente);
            ObservarTareaPendiente(consultaPendiente);
            return ResultadoConexionRemota.Fallido(TipoFalloConexionRemota.TiempoAgotado);
        }
        catch (SshConnectionException) when (identidadRechazada)
        {
            return ResultadoConexionRemota.Fallido(
                TipoFalloConexionRemota.IdentidadHostNoCoincide);
        }
        catch (SocketException)
        {
            return ResultadoConexionRemota.Fallido(
                TipoFalloConexionRemota.ConexionNoDisponible);
        }
        catch (SshConnectionException)
        {
            return ResultadoConexionRemota.Fallido(
                TipoFalloConexionRemota.ConexionNoDisponible);
        }
        catch (DecoderFallbackException)
        {
            return ResultadoConexionRemota.Fallido(TipoFalloConexionRemota.RespuestaInvalida);
        }
        catch (XmlException)
        {
            return ResultadoConexionRemota.Fallido(TipoFalloConexionRemota.RespuestaInvalida);
        }
        catch (SshException)
        {
            return ResultadoConexionRemota.Fallido(TipoFalloConexionRemota.RespuestaInvalida);
        }
        catch (IOException)
        {
            return ResultadoConexionRemota.Fallido(
                TipoFalloConexionRemota.ConexionNoDisponible);
        }
        finally
        {
            Desconectar(cliente);
        }
    }

    internal static bool ServidorAdmiteNetconfBase(XmlDocument capacidades)
    {
        ArgumentNullException.ThrowIfNull(capacidades);
        return capacidades
            .GetElementsByTagName("*")
            .Cast<XmlNode>()
            .Any(nodo =>
                nodo.LocalName.Equals("capability", StringComparison.OrdinalIgnoreCase)
                && (nodo.InnerText.Trim().StartsWith(CapacidadBase10, StringComparison.Ordinal)
                    || nodo.InnerText.Trim().StartsWith(CapacidadBase11, StringComparison.Ordinal)));
    }

    private static void Desconectar(NetConfClient cliente)
    {
        if (cliente.IsConnected)
        {
            cliente.Disconnect();
        }
    }

    private static void ObservarTareaPendiente(Task? tarea)
    {
        if (tarea is null || tarea.IsCompleted)
        {
            return;
        }

        _ = tarea.ContinueWith(
            tareaFinalizada => _ = tareaFinalizada.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted
                | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
