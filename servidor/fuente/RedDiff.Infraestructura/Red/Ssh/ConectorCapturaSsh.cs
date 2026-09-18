using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using RedDiff.Aplicacion.Abstracciones.Red;
using RedDiff.Dominio.Enumeraciones;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace RedDiff.Infraestructura.Red.Ssh;

internal sealed class ConectorCapturaSsh : IConectorCapturaRemota
{
    internal const string ComandoPreparacion = "terminal length 0";
    internal const string ComandoConsulta = "show running-config view full";
    internal const string TerminadorComandoTerminal = "\r";

    private const int TamanoBufferSesion = 64 * 1024;
    private const int TamanoMaximoSesionCaracteres = 5_100_000;

    private static readonly UTF8Encoding Utf8Estricto = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    public ProtocoloConexion Protocolo => ProtocoloConexion.Ssh;

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

        using SshClient cliente = new(informacionConexion)
        {
            KeepAliveInterval = TimeSpan.FromSeconds(5)
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

        try
        {
            await cliente.ConnectAsync(cancellationToken);

            if (!identidadRecibida || identidadRechazada || !cliente.IsConnected)
            {
                return ResultadoConexionRemota.Fallido(
                    TipoFalloConexionRemota.IdentidadHostNoCoincide);
            }

            using ShellStream sesion = cliente.CreateShellStream(
                "vt100",
                columns: 511,
                rows: 24,
                width: 0,
                height: 0,
                bufferSize: TamanoBufferSesion);

            string bienvenida = await LeerHastaPromptAsync(
                sesion,
                promptEsperado: null,
                solicitud.TiempoEspera,
                cancellationToken);
            if (!ProcesadorSalidaCiscoIos.TryObtenerPromptFinal(bienvenida, out string prompt))
            {
                return ResultadoConexionRemota.Fallido(
                    TipoFalloConexionRemota.RespuestaInvalida);
            }

            string respuestaPreparacion = await EjecutarEnSesionAsync(
                sesion,
                ComandoPreparacion,
                prompt,
                solicitud.TiempoEspera,
                cancellationToken);
            if (ProcesadorSalidaCiscoIos.ContieneErrorCli(respuestaPreparacion))
            {
                return ResultadoConexionRemota.Fallido(
                    TipoFalloConexionRemota.RespuestaInvalida);
            }

            string respuestaConsulta = await EjecutarEnSesionAsync(
                sesion,
                ComandoConsulta,
                prompt,
                solicitud.TiempoEspera,
                cancellationToken);
            string? configuracion = ProcesadorSalidaCiscoIos.ExtraerResultadoComando(
                respuestaConsulta,
                ComandoConsulta,
                prompt);

            return string.IsNullOrWhiteSpace(configuracion)
                || ProcesadorSalidaCiscoIos.ContieneErrorCli(configuracion)
                    ? ResultadoConexionRemota.Fallido(
                        TipoFalloConexionRemota.RespuestaInvalida)
                    : ResultadoConexionRemota.Correcto(configuracion);
        }
        catch (OperationCanceledException)
        {
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
            if (cliente.IsConnected)
            {
                cliente.Disconnect();
            }
        }
    }

    private static async Task<string> EjecutarEnSesionAsync(
        ShellStream sesion,
        string comando,
        string prompt,
        TimeSpan tiempoEspera,
        CancellationToken cancellationToken)
    {
        // Una PTY interpreta CR como Enter. Enviar CRLF provoca dos entradas en IOS:
        // el segundo prompt puede quedar pendiente y confundirse con la respuesta siguiente.
        byte[] bytes = Utf8Estricto.GetBytes(comando + TerminadorComandoTerminal);
        await sesion.WriteAsync(bytes.AsMemory(), cancellationToken);
        await sesion.FlushAsync(cancellationToken);

        return await LeerHastaPromptAsync(
            sesion,
            prompt,
            tiempoEspera,
            cancellationToken);
    }

    private static async Task<string> LeerHastaPromptAsync(
        ShellStream sesion,
        string? promptEsperado,
        TimeSpan tiempoEspera,
        CancellationToken cancellationToken)
    {
        Regex patronPrompt = ProcesadorSalidaCiscoIos.CrearPatronPromptFinal(promptEsperado);
        Task<string?> espera = Task.Factory.FromAsync(
            (callback, estado) => sesion.BeginExpect(
                tiempoEspera,
                callback,
                estado,
                new ExpectAction(patronPrompt, _ => { })),
            sesion.EndExpect,
            state: null);

        try
        {
            string? contenido = await espera.WaitAsync(cancellationToken);
            if (contenido is null)
            {
                throw new TimeoutException("La sesión SSH no respondió dentro del límite.");
            }

            if (contenido.Length > TamanoMaximoSesionCaracteres)
            {
                throw new DecoderFallbackException(
                    "La respuesta SSH supera el límite admitido.");
            }

            return contenido;
        }
        catch (OperationCanceledException)
        {
            sesion.Dispose();
            _ = espera.ContinueWith(
                tarea => _ = tarea.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted
                    | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            throw;
        }
    }
}
