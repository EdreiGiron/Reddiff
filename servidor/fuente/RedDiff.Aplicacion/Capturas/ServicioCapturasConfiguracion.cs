using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Comun;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Capturas;

public sealed class ServicioCapturasConfiguracion(
    IRepositorioCapturas repositorioCapturas,
    IRepositorioVersionesConfiguracion repositorioVersiones,
    IRepositorioDispositivos repositorioDispositivos,
    IRepositorioUsuarios repositorioUsuarios,
    IRepositorioAuditorias repositorioAuditorias,
    IUnidadDeTrabajo unidadDeTrabajo,
    ProcesadorArchivoConfiguracion procesadorArchivo,
    TimeProvider reloj)
{
    public async Task<ResultadoOperacion<CargaArchivoResultado>> CargarArchivoAsync(
        long usuarioId,
        CargarArchivoConfiguracionSolicitud solicitud,
        CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            false,
            cancellationToken);
        if (usuario is null || !usuario.Estado || !usuario.Rol.Estado)
        {
            return ResultadoOperacion<CargaArchivoResultado>.Fallido(
                CodigosErrorOperacion.Prohibido,
                "La operación requiere un usuario activo.");
        }

        ResultadoOperacion<ArchivoConfiguracionProcesado> procesamiento = procesadorArchivo.Procesar(
            solicitud.NombreArchivo,
            solicitud.Contenido,
            solicitud.Comentario);
        if (!procesamiento.Exitoso)
        {
            return await FallarCargaAsync(
                usuarioId,
                null,
                procesamiento.Error!.Codigo,
                procesamiento.Error.Mensaje,
                cancellationToken);
        }

        if (solicitud.DispositivoId <= 0)
        {
            return await FallarCargaAsync(
                usuarioId,
                null,
                CodigosErrorOperacion.Validacion,
                "Debe seleccionar un dispositivo válido.",
                cancellationToken);
        }

        Dispositivo? dispositivo = await repositorioDispositivos.ObtenerPorIdAsync(
            solicitud.DispositivoId,
            false,
            cancellationToken);
        if (dispositivo is null)
        {
            return await FallarCargaAsync(
                usuarioId,
                solicitud.DispositivoId,
                CodigosErrorOperacion.NoEncontrado,
                "El dispositivo solicitado no existe.",
                cancellationToken);
        }

        if (dispositivo.Estado != EstadoDispositivo.Autorizado)
        {
            return await FallarCargaAsync(
                usuarioId,
                dispositivo.Id,
                CodigosErrorOperacion.Prohibido,
                "Solo se pueden registrar configuraciones de dispositivos autorizados.",
                cancellationToken);
        }

        ArchivoConfiguracionProcesado archivo = procesamiento.Valor!;
        DateTimeOffset fecha = reloj.GetUtcNow();
        Captura captura = Captura.CrearBajoDemanda(
            dispositivo.Id,
            usuarioId,
            MedioCaptura.Archivo,
            fecha);
        captura.Iniciar();
        repositorioCapturas.Agregar(captura);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        int numero = await repositorioVersiones.ObtenerSiguienteNumeroAsync(
            dispositivo.Id,
            cancellationToken);
        VersionConfiguracion version = new(
            dispositivo.Id,
            captura.Id,
            numero,
            OrigenVersion.Archivo,
            archivo.Contenido,
            archivo.Hash,
            fecha,
            archivo.Comentario);
        repositorioVersiones.Agregar(version);
        captura.Completar();
        repositorioAuditorias.Agregar(new Auditoria(
            usuarioId,
            "CargarArchivoConfiguracion",
            "Captura",
            captura.Id,
            fecha,
            EstadoAuditoria.Exitoso,
            $"Se registró la versión {numero} del dispositivo {dispositivo.Nombre} "
                + $"desde el archivo {archivo.NombreArchivo}, con huella {archivo.Hash}."));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        CapturaResumen capturaResumen = MapearCaptura(
            captura,
            dispositivo.Nombre,
            usuario.NombreUsuario,
            version.Id);
        VersionConfiguracionResumen versionResumen = MapearVersion(
            version,
            dispositivo.Nombre,
            usuario.NombreUsuario);

        return ResultadoOperacion<CargaArchivoResultado>.Correcto(
            new CargaArchivoResultado(capturaResumen, versionResumen));
    }

    public async Task<ResultadoOperacion<IReadOnlyList<CapturaResumen>>> ListarCapturasAsync(
        long? dispositivoId,
        string? estado,
        CancellationToken cancellationToken = default)
    {
        if (dispositivoId <= 0)
        {
            return FallarListaCapturas("El identificador del dispositivo no es válido.");
        }

        if (!IntentarInterpretar(estado, out EstadoCaptura? estadoInterpretado))
        {
            return FallarListaCapturas(
                "El estado de captura debe ser Pendiente, EnProceso, Completada, Fallida o Cancelada.");
        }

        IReadOnlyList<Captura> capturas = await repositorioCapturas.ListarAsync(
            dispositivoId,
            estadoInterpretado,
            cancellationToken);

        return ResultadoOperacion<IReadOnlyList<CapturaResumen>>.Correcto(
            capturas.Select(MapearCaptura).ToArray());
    }

    public async Task<ResultadoOperacion<IReadOnlyList<VersionConfiguracionResumen>>> ListarVersionesAsync(
        long? dispositivoId,
        string? origen,
        string? estado,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default)
    {
        if (dispositivoId <= 0)
        {
            return FallarListaVersiones("El identificador del dispositivo no es válido.");
        }

        if (!IntentarInterpretar(origen, out OrigenVersion? origenInterpretado))
        {
            return FallarListaVersiones(
                "El origen debe ser CapturaSsh, CapturaNetconf o Archivo.");
        }

        if (!IntentarInterpretar(estado, out EstadoVersion? estadoInterpretado))
        {
            return FallarListaVersiones("El estado debe ser Activa o Retirada.");
        }

        if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
        {
            return FallarListaVersiones(
                "La fecha inicial no puede ser posterior a la fecha final.");
        }

        IReadOnlyList<VersionConfiguracion> versiones = await repositorioVersiones.ListarAsync(
            dispositivoId,
            origenInterpretado,
            estadoInterpretado,
            desde,
            hasta,
            cancellationToken);

        return ResultadoOperacion<IReadOnlyList<VersionConfiguracionResumen>>.Correcto(
            versiones.Select(MapearVersion).ToArray());
    }

    public async Task<ResultadoOperacion<VersionConfiguracionDetalle>> ObtenerVersionAsync(
        long versionId,
        CancellationToken cancellationToken = default)
    {
        if (versionId <= 0)
        {
            return ResultadoOperacion<VersionConfiguracionDetalle>.Fallido(
                CodigosErrorOperacion.Validacion,
                "El identificador de la versión no es válido.");
        }

        VersionConfiguracion? version = await repositorioVersiones.ObtenerPorIdAsync(
            versionId,
            cancellationToken);
        return version is null
            ? ResultadoOperacion<VersionConfiguracionDetalle>.Fallido(
                CodigosErrorOperacion.NoEncontrado,
                "La versión solicitada no existe.")
            : ResultadoOperacion<VersionConfiguracionDetalle>.Correcto(
                MapearDetalle(version));
    }

    private async Task<ResultadoOperacion<CargaArchivoResultado>> FallarCargaAsync(
        long usuarioId,
        long? dispositivoId,
        string codigo,
        string mensaje,
        CancellationToken cancellationToken)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            usuarioId > 0 ? usuarioId : null,
            "CargarArchivoConfiguracion",
            "Captura",
            dispositivoId,
            reloj.GetUtcNow(),
            EstadoAuditoria.Fallido,
            mensaje));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<CargaArchivoResultado>.Fallido(codigo, mensaje);
    }

    private static ResultadoOperacion<IReadOnlyList<CapturaResumen>> FallarListaCapturas(
        string mensaje)
    {
        return ResultadoOperacion<IReadOnlyList<CapturaResumen>>.Fallido(
            CodigosErrorOperacion.Validacion,
            mensaje);
    }

    private static ResultadoOperacion<IReadOnlyList<VersionConfiguracionResumen>> FallarListaVersiones(
        string mensaje)
    {
        return ResultadoOperacion<IReadOnlyList<VersionConfiguracionResumen>>.Fallido(
            CodigosErrorOperacion.Validacion,
            mensaje);
    }

    private static bool IntentarInterpretar<T>(string? valor, out T? resultado)
        where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            resultado = null;
            return true;
        }

        foreach (T candidato in Enum.GetValues<T>())
        {
            if (string.Equals(valor, candidato.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                resultado = candidato;
                return true;
            }
        }

        resultado = null;
        return false;
    }

    private static CapturaResumen MapearCaptura(Captura captura)
    {
        return MapearCaptura(
            captura,
            captura.Dispositivo.Nombre,
            captura.UsuarioSolicitante?.NombreUsuario,
            captura.VersionConfiguracion?.Id);
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

    private static VersionConfiguracionResumen MapearVersion(VersionConfiguracion version)
    {
        return MapearVersion(
            version,
            version.Dispositivo.Nombre,
            version.Captura.UsuarioSolicitante?.NombreUsuario);
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

    private static VersionConfiguracionDetalle MapearDetalle(VersionConfiguracion version)
    {
        VersionConfiguracionResumen resumen = MapearVersion(version);
        return new VersionConfiguracionDetalle(
            resumen.Id,
            resumen.DispositivoId,
            resumen.Dispositivo,
            resumen.CapturaId,
            resumen.Numero,
            resumen.Origen,
            resumen.Hash,
            resumen.CapturadaEn,
            resumen.Comentario,
            resumen.Estado,
            resumen.Estable,
            resumen.UsuarioSolicitante,
            version.Contenido);
    }
}
