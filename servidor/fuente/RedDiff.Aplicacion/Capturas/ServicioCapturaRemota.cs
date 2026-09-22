using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Abstracciones.Red;
using RedDiff.Aplicacion.Abstracciones.Seguridad;
using RedDiff.Aplicacion.Comun;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Capturas;

public sealed class ServicioCapturaRemota
{
    private const string AccionAuditoria = "CapturarConfiguracionRemota";

    private readonly IRepositorioCapturas repositorioCapturas;
    private readonly IRepositorioVersionesConfiguracion repositorioVersiones;
    private readonly IRepositorioDispositivos repositorioDispositivos;
    private readonly IRepositorioUsuarios repositorioUsuarios;
    private readonly IRepositorioAuditorias repositorioAuditorias;
    private readonly IUnidadDeTrabajo unidadDeTrabajo;
    private readonly IProtectorSecretoDispositivo protectorSecreto;
    private readonly ProcesadorArchivoConfiguracion procesadorContenido;
    private readonly OpcionesCapturaRemota opciones;
    private readonly TimeProvider reloj;
    private readonly IReadOnlyDictionary<ProtocoloConexion, IConectorCapturaRemota> conectores;

    public ServicioCapturaRemota(
        IRepositorioCapturas repositorioCapturas,
        IRepositorioVersionesConfiguracion repositorioVersiones,
        IRepositorioDispositivos repositorioDispositivos,
        IRepositorioUsuarios repositorioUsuarios,
        IRepositorioAuditorias repositorioAuditorias,
        IUnidadDeTrabajo unidadDeTrabajo,
        IProtectorSecretoDispositivo protectorSecreto,
        ProcesadorArchivoConfiguracion procesadorContenido,
        OpcionesCapturaRemota opciones,
        TimeProvider reloj,
        IEnumerable<IConectorCapturaRemota> conectores)
    {
        this.repositorioCapturas = repositorioCapturas;
        this.repositorioVersiones = repositorioVersiones;
        this.repositorioDispositivos = repositorioDispositivos;
        this.repositorioUsuarios = repositorioUsuarios;
        this.repositorioAuditorias = repositorioAuditorias;
        this.unidadDeTrabajo = unidadDeTrabajo;
        this.protectorSecreto = protectorSecreto;
        this.procesadorContenido = procesadorContenido;
        this.opciones = opciones;
        this.reloj = reloj;
        this.conectores = conectores.ToDictionary(conector => conector.Protocolo);
    }

    public async Task<ResultadoOperacion<CapturaRemotaResultado>> CapturarAsync(
        long usuarioId,
        long dispositivoId,
        CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            false,
            cancellationToken);
        if (usuario is null || !usuario.Estado || !usuario.Rol.Estado)
        {
            return ResultadoOperacion<CapturaRemotaResultado>.Fallido(
                CodigosErrorOperacion.Prohibido,
                "La operación requiere un usuario activo.");
        }

        if (dispositivoId <= 0)
        {
            return await FallarAntesDeCapturaAsync(
                usuarioId,
                null,
                CodigosErrorOperacion.Validacion,
                "Debe seleccionar un dispositivo válido.",
                cancellationToken);
        }

        Dispositivo? dispositivo = await repositorioDispositivos.ObtenerPorIdAsync(
            dispositivoId,
            false,
            cancellationToken);
        if (dispositivo is null)
        {
            return await FallarAntesDeCapturaAsync(
                usuarioId,
                dispositivoId,
                CodigosErrorOperacion.NoEncontrado,
                "El dispositivo solicitado no existe.",
                cancellationToken);
        }

        if (dispositivo.Estado != EstadoDispositivo.Autorizado)
        {
            return await FallarAntesDeCapturaAsync(
                usuarioId,
                dispositivo.Id,
                CodigosErrorOperacion.Prohibido,
                "Solo se pueden consultar dispositivos autorizados.",
                cancellationToken);
        }

        if (!dispositivo.AccesoRemotoConfigurado)
        {
            return await FallarAntesDeCapturaAsync(
                usuarioId,
                dispositivo.Id,
                CodigosErrorOperacion.Conflicto,
                "El dispositivo no tiene un acceso remoto completo y verificado.",
                cancellationToken);
        }

        ResultadoOperacion<CapturaPorEventoResultado> captura =
            await CapturarDispositivoAsync(
                usuarioId,
                usuario.NombreUsuario,
                null,
                dispositivo,
                cancellationToken);

        if (!captura.Exitoso)
        {
            return ResultadoOperacion<CapturaRemotaResultado>.Fallido(
                captura.Error!.Codigo,
                captura.Error.Mensaje);
        }

        CapturaPorEventoResultado valor = captura.Valor!;
        if (valor.Version is null)
        {
            return ResultadoOperacion<CapturaRemotaResultado>.Fallido(
                CodigosErrorOperacion.Conflicto,
                "La captura manual finalizó sin producir una versión.");
        }

        return ResultadoOperacion<CapturaRemotaResultado>.Correcto(
            new CapturaRemotaResultado(valor.Captura, valor.Version));
    }

    public Task<ResultadoOperacion<CapturaPorEventoResultado>> CapturarPorEventoAsync(
        EventoCambio evento,
        Dispositivo dispositivo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evento);
        ArgumentNullException.ThrowIfNull(dispositivo);

        if (evento.DispositivoId != dispositivo.Id)
        {
            return Task.FromResult(ResultadoOperacion<CapturaPorEventoResultado>.Fallido(
                CodigosErrorOperacion.Validacion,
                "El evento no pertenece al dispositivo indicado."));
        }

        if (evento.Estado != EstadoEventoCambio.Encolado)
        {
            return Task.FromResult(ResultadoOperacion<CapturaPorEventoResultado>.Fallido(
                CodigosErrorOperacion.Conflicto,
                "Solo un evento encolado puede originar una captura."));
        }

        if (dispositivo.Estado != EstadoDispositivo.Autorizado
            || !dispositivo.AccesoRemotoConfigurado)
        {
            return Task.FromResult(ResultadoOperacion<CapturaPorEventoResultado>.Fallido(
                CodigosErrorOperacion.Prohibido,
                "El dispositivo no está preparado para una captura automática."));
        }

        return CapturarDispositivoAsync(
            null,
            null,
            evento,
            dispositivo,
            cancellationToken);
    }

    private async Task<ResultadoOperacion<CapturaPorEventoResultado>> CapturarDispositivoAsync(
        long? usuarioId,
        string? usuarioSolicitante,
        EventoCambio? evento,
        Dispositivo dispositivo,
        CancellationToken cancellationToken)
    {

        if (!conectores.TryGetValue(dispositivo.Protocolo, out IConectorCapturaRemota? conector))
        {
            const string codigo = CodigosErrorOperacion.NoDisponible;
            string mensaje = $"El conector {dispositivo.Protocolo.ToString().ToUpperInvariant()} todavía no está habilitado.";
            if (usuarioId.HasValue)
            {
                await AuditarFalloAntesDeCapturaAsync(
                    usuarioId,
                    dispositivo.Id,
                    mensaje,
                    cancellationToken);
            }

            return ResultadoOperacion<CapturaPorEventoResultado>.Fallido(codigo, mensaje);
        }

        if (!protectorSecreto.IntentarDesproteger(
                dispositivo.SecretoAccesoProtegido,
                out string secreto))
        {
            const string codigo = CodigosErrorOperacion.Conflicto;
            const string mensaje = "El secreto protegido no pudo recuperarse. Reemplace el acceso remoto antes de intentar la captura.";
            if (usuarioId.HasValue)
            {
                await AuditarFalloAntesDeCapturaAsync(
                    usuarioId,
                    dispositivo.Id,
                    mensaje,
                    cancellationToken);
            }

            return ResultadoOperacion<CapturaPorEventoResultado>.Fallido(codigo, mensaje);
        }

        DateTimeOffset fecha = reloj.GetUtcNow();
        MedioCaptura medio = dispositivo.Protocolo == ProtocoloConexion.Ssh
            ? MedioCaptura.Ssh
            : MedioCaptura.Netconf;
        Captura captura = evento is null
            ? Captura.CrearBajoDemanda(dispositivo.Id, usuarioId!.Value, medio, fecha)
            : Captura.CrearPorEvento(dispositivo.Id, evento.Id, medio, fecha);
        captura.Iniciar();
        repositorioCapturas.Agregar(captura);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        ResultadoConexionRemota resultadoConexion;

        try
        {
            SolicitudConexionRemota solicitud = new(
                dispositivo.Host,
                dispositivo.Puerto,
                dispositivo.UsuarioAcceso!,
                secreto,
                dispositivo.AlgoritmoClaveHost!,
                dispositivo.HuellaClaveHost!,
                opciones.TiempoEspera);
            using CancellationTokenSource limite = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            limite.CancelAfter(opciones.TiempoEspera);
            resultadoConexion = await conector.CapturarAsync(solicitud, limite.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await FallarCapturaAsync(
                usuarioId,
                dispositivo,
                captura,
                CodigosErrorOperacion.NoDisponible,
                "El dispositivo no respondió dentro del tiempo permitido.",
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            captura.Cancelar();
            RegistrarAuditoria(
                usuarioId,
                captura.Id,
                EstadoAuditoria.Fallido,
                $"La captura remota de {dispositivo.Nombre} fue cancelada.");
            await unidadDeTrabajo.GuardarCambiosAsync(CancellationToken.None);
            throw;
        }
        catch (Exception)
        {
            return await FallarCapturaAsync(
                usuarioId,
                dispositivo,
                captura,
                CodigosErrorOperacion.NoDisponible,
                "No fue posible establecer una conexión segura con el dispositivo.",
                CancellationToken.None);
        }
        finally
        {
            secreto = string.Empty;
        }

        if (!resultadoConexion.Exitoso)
        {
            (string codigo, string mensaje) = MapearFalloConexion(resultadoConexion.TipoFallo);
            return await FallarCapturaAsync(
                usuarioId,
                dispositivo,
                captura,
                codigo,
                mensaje,
                CancellationToken.None);
        }

        ResultadoOperacion<ContenidoConfiguracionProcesado> procesamiento =
            procesadorContenido.ProcesarContenidoRemoto(resultadoConexion.Contenido);
        if (!procesamiento.Exitoso)
        {
            return await FallarCapturaAsync(
                usuarioId,
                dispositivo,
                captura,
                procesamiento.Error!.Codigo,
                procesamiento.Error.Mensaje,
                CancellationToken.None);
        }

        ContenidoConfiguracionProcesado contenido = procesamiento.Valor!;
        VersionConfiguracion? ultimaVersion = await repositorioVersiones.ObtenerUltimaAsync(
            dispositivo.Id,
            CancellationToken.None);
        if (evento is not null
            && string.Equals(
                ultimaVersion?.Hash,
                contenido.Hash,
                StringComparison.OrdinalIgnoreCase))
        {
            captura.Completar();
            RegistrarAuditoria(
                usuarioId,
                captura.Id,
                EstadoAuditoria.Exitoso,
                $"La captura automática de {dispositivo.Nombre} no generó una versión porque "
                    + $"su huella {contenido.Hash} coincide con la versión vigente más reciente.");
            await unidadDeTrabajo.GuardarCambiosAsync(CancellationToken.None);

            return ResultadoOperacion<CapturaPorEventoResultado>.Correcto(
                new CapturaPorEventoResultado(
                    MapearCaptura(captura, dispositivo.Nombre, usuarioSolicitante, null),
                    null,
                    CambioDetectado: false));
        }

        int numero = await repositorioVersiones.ObtenerSiguienteNumeroAsync(
            dispositivo.Id,
            CancellationToken.None);
        OrigenVersion origen = dispositivo.Protocolo == ProtocoloConexion.Ssh
            ? OrigenVersion.CapturaSsh
            : OrigenVersion.CapturaNetconf;
        VersionConfiguracion version = new(
            dispositivo.Id,
            captura.Id,
            numero,
            origen,
            contenido.Contenido,
            contenido.Hash,
            fecha,
            evento is null
                ? $"Captura remota de solo lectura mediante {dispositivo.Protocolo.ToString().ToUpperInvariant()}."
                : $"Captura automática originada por evento Syslog mediante {dispositivo.Protocolo.ToString().ToUpperInvariant()}.");
        repositorioVersiones.Agregar(version);
        captura.Completar();
        RegistrarAuditoria(
            usuarioId,
            captura.Id,
            EstadoAuditoria.Exitoso,
            $"Se registró la versión {numero} de {dispositivo.Nombre} mediante "
                + $"{dispositivo.Protocolo.ToString().ToUpperInvariant()}, con huella {contenido.Hash}.");
        await unidadDeTrabajo.GuardarCambiosAsync(CancellationToken.None);

        return ResultadoOperacion<CapturaPorEventoResultado>.Correcto(
            new CapturaPorEventoResultado(
                MapearCaptura(captura, dispositivo.Nombre, usuarioSolicitante, version.Id),
                MapearVersion(version, dispositivo.Nombre, usuarioSolicitante),
                CambioDetectado: true));
    }

    private async Task<ResultadoOperacion<CapturaRemotaResultado>> FallarAntesDeCapturaAsync(
        long? usuarioId,
        long? dispositivoId,
        string codigo,
        string mensaje,
        CancellationToken cancellationToken)
    {
        await AuditarFalloAntesDeCapturaAsync(
            usuarioId,
            dispositivoId,
            mensaje,
            cancellationToken);

        return ResultadoOperacion<CapturaRemotaResultado>.Fallido(codigo, mensaje);
    }

    private async Task<ResultadoOperacion<CapturaPorEventoResultado>> FallarCapturaAsync(
        long? usuarioId,
        Dispositivo dispositivo,
        Captura captura,
        string codigo,
        string mensaje,
        CancellationToken cancellationToken)
    {
        captura.Fallar(mensaje);
        RegistrarAuditoria(
            usuarioId,
            captura.Id,
            EstadoAuditoria.Fallido,
            $"Falló la captura remota de {dispositivo.Nombre}: {mensaje}");
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<CapturaPorEventoResultado>.Fallido(codigo, mensaje);
    }

    private async Task AuditarFalloAntesDeCapturaAsync(
        long? usuarioId,
        long? dispositivoId,
        string mensaje,
        CancellationToken cancellationToken)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            usuarioId,
            AccionAuditoria,
            "Dispositivo",
            dispositivoId,
            reloj.GetUtcNow(),
            EstadoAuditoria.Fallido,
            mensaje));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
    }

    private void RegistrarAuditoria(
        long? usuarioId,
        long capturaId,
        EstadoAuditoria estado,
        string detalle)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            usuarioId,
            AccionAuditoria,
            "Captura",
            capturaId,
            reloj.GetUtcNow(),
            estado,
            detalle));
    }

    private static (string Codigo, string Mensaje) MapearFalloConexion(
        TipoFalloConexionRemota? tipoFallo)
    {
        return tipoFallo switch
        {
            TipoFalloConexionRemota.IdentidadHostNoCoincide => (
                CodigosErrorOperacion.Conflicto,
                "La identidad criptográfica del dispositivo no coincide con la huella aprobada."),
            TipoFalloConexionRemota.AutenticacionRechazada => (
                CodigosErrorOperacion.NoDisponible,
                "El dispositivo rechazó las credenciales configuradas."),
            TipoFalloConexionRemota.TiempoAgotado => (
                CodigosErrorOperacion.NoDisponible,
                "El dispositivo no respondió dentro del tiempo permitido."),
            TipoFalloConexionRemota.RespuestaInvalida => (
                CodigosErrorOperacion.NoDisponible,
                "El dispositivo devolvió una respuesta que no puede utilizarse como configuración."),
            _ => (
                CodigosErrorOperacion.NoDisponible,
                "No fue posible establecer una conexión segura con el dispositivo.")
        };
    }

    private static CapturaResumen MapearCaptura(
        Captura captura,
        string dispositivo,
        string? usuarioSolicitante,
        long? versionId)
    {
        return new CapturaResumen(
            captura.Id,
            captura.DispositivoId,
            dispositivo,
            usuarioSolicitante,
            captura.Disparador.ToString(),
            captura.Medio.ToString(),
            captura.Estado.ToString(),
            captura.Fecha,
            captura.Error,
            versionId);
    }

    private static VersionConfiguracionResumen MapearVersion(
        VersionConfiguracion version,
        string dispositivo,
        string? usuarioSolicitante)
    {
        return new VersionConfiguracionResumen(
            version.Id,
            version.DispositivoId,
            dispositivo,
            version.CapturaId,
            version.Numero,
            version.Origen.ToString(),
            version.Hash,
            version.CapturadaEn,
            version.Comentario,
            version.Estado.ToString(),
            version.Estable,
            usuarioSolicitante);
    }
}
