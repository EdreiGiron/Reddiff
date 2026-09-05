using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Comun;
using RedDiff.Aplicacion.Seguridad;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Inventario.Dispositivos;

public sealed class ServicioGestionDispositivos(
    IRepositorioDispositivos repositorioDispositivos,
    IRepositorioUsuarios repositorioUsuarios,
    IRepositorioAuditorias repositorioAuditorias,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj)
{
    public async Task<IReadOnlyList<DispositivoResumen>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Dispositivo> dispositivos = await repositorioDispositivos.ListarAsync(
            cancellationToken);
        return dispositivos.Select(dispositivo => Mapear(dispositivo)).ToArray();
    }

    public async Task<ResultadoOperacion<DispositivoResumen>> ObtenerAsync(
        long dispositivoId,
        CancellationToken cancellationToken = default)
    {
        Dispositivo? dispositivo = await repositorioDispositivos.ObtenerPorIdAsync(
            dispositivoId,
            false,
            cancellationToken);

        return dispositivo is null
            ? ResultadoOperacion<DispositivoResumen>.Fallido(
                CodigosErrorOperacion.NoEncontrado,
                "El dispositivo solicitado no existe.")
            : ResultadoOperacion<DispositivoResumen>.Correcto(Mapear(dispositivo));
    }

    public async Task<ResultadoOperacion<DispositivoResumen>> CrearAsync(
        long administradorId,
        CrearDispositivoSolicitud solicitud,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<Usuario>? autorizacion = await ValidarAdministradorAsync(
            administradorId,
            cancellationToken);
        if (autorizacion is not null)
        {
            return ResultadoOperacion<DispositivoResumen>.Fallido(
                autorizacion.Error!.Codigo,
                autorizacion.Error.Mensaje);
        }

        Dispositivo dispositivo;
        try
        {
            dispositivo = ConstruirDispositivo(
                solicitud.Nombre,
                solicitud.Host,
                solicitud.Tipo,
                solicitud.Modelo,
                solicitud.Protocolo,
                solicitud.Puerto,
                solicitud.FuenteEventos);
        }
        catch (ArgumentException excepcion)
        {
            return await FallarAsync(
                administradorId,
                "CrearDispositivo",
                null,
                CodigosErrorOperacion.Validacion,
                excepcion.Message,
                cancellationToken);
        }

        ResultadoOperacion<DispositivoResumen>? conflicto = await ValidarUnicidadAsync(
            administradorId,
            "CrearDispositivo",
            dispositivo,
            null,
            cancellationToken);
        if (conflicto is not null)
        {
            return conflicto;
        }

        repositorioDispositivos.Agregar(dispositivo);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        RegistrarAuditoria(
            administradorId,
            "CrearDispositivo",
            dispositivo.Id,
            $"Se registró {dispositivo.Nombre} en "
                + $"{dispositivo.Host}:{dispositivo.Puerto}.");
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<DispositivoResumen>.Correcto(Mapear(dispositivo));
    }

    public async Task<ResultadoOperacion<DispositivoResumen>> ActualizarAsync(
        long administradorId,
        long dispositivoId,
        ActualizarDispositivoSolicitud solicitud,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<Usuario>? autorizacion = await ValidarAdministradorAsync(
            administradorId,
            cancellationToken);
        if (autorizacion is not null)
        {
            return ResultadoOperacion<DispositivoResumen>.Fallido(
                autorizacion.Error!.Codigo,
                autorizacion.Error.Mensaje);
        }

        Dispositivo? dispositivo = await repositorioDispositivos.ObtenerPorIdAsync(
            dispositivoId,
            true,
            cancellationToken);
        if (dispositivo is null)
        {
            return await FallarAsync(
                administradorId,
                "ActualizarDispositivo",
                dispositivoId,
                CodigosErrorOperacion.NoEncontrado,
                "El dispositivo solicitado no existe.",
                cancellationToken);
        }

        Dispositivo datosValidados;
        try
        {
            datosValidados = ConstruirDispositivo(
                solicitud.Nombre,
                solicitud.Host,
                solicitud.Tipo,
                solicitud.Modelo,
                solicitud.Protocolo,
                solicitud.Puerto,
                solicitud.FuenteEventos);
        }
        catch (ArgumentException excepcion)
        {
            return await FallarAsync(
                administradorId,
                "ActualizarDispositivo",
                dispositivoId,
                CodigosErrorOperacion.Validacion,
                excepcion.Message,
                cancellationToken);
        }

        ResultadoOperacion<DispositivoResumen>? conflicto = await ValidarUnicidadAsync(
            administradorId,
            "ActualizarDispositivo",
            datosValidados,
            dispositivoId,
            cancellationToken);
        if (conflicto is not null)
        {
            return conflicto;
        }

        dispositivo.ActualizarDatos(
            datosValidados.Nombre,
            datosValidados.Host,
            datosValidados.Tipo,
            datosValidados.Modelo,
            datosValidados.Protocolo,
            datosValidados.Puerto,
            datosValidados.FuenteEventos);

        RegistrarAuditoria(
            administradorId,
            "ActualizarDispositivo",
            dispositivo.Id,
            $"Se actualizaron los datos de conexión de {dispositivo.Nombre}.");
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<DispositivoResumen>.Correcto(Mapear(dispositivo));
    }

    public async Task<ResultadoOperacion<DispositivoResumen>> CambiarEstadoAsync(
        long administradorId,
        long dispositivoId,
        string estadoSolicitado,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<Usuario>? autorizacion = await ValidarAdministradorAsync(
            administradorId,
            cancellationToken);
        if (autorizacion is not null)
        {
            return ResultadoOperacion<DispositivoResumen>.Fallido(
                autorizacion.Error!.Codigo,
                autorizacion.Error.Mensaje);
        }

        if (!IntentarInterpretarEstado(estadoSolicitado, out EstadoDispositivo estado))
        {
            return await FallarAsync(
                administradorId,
                "CambiarEstadoDispositivo",
                dispositivoId,
                CodigosErrorOperacion.Validacion,
                "El estado debe ser NoAutorizado, Autorizado o Inactivo.",
                cancellationToken);
        }

        Dispositivo? dispositivo = await repositorioDispositivos.ObtenerPorIdAsync(
            dispositivoId,
            true,
            cancellationToken);
        if (dispositivo is null)
        {
            return await FallarAsync(
                administradorId,
                "CambiarEstadoDispositivo",
                dispositivoId,
                CodigosErrorOperacion.NoEncontrado,
                "El dispositivo solicitado no existe.",
                cancellationToken);
        }

        dispositivo.CambiarEstado(estado);
        RegistrarAuditoria(
            administradorId,
            "CambiarEstadoDispositivo",
            dispositivo.Id,
            $"El dispositivo cambió al estado {estado}.");
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<DispositivoResumen>.Correcto(Mapear(dispositivo));
    }

    private async Task<ResultadoOperacion<DispositivoResumen>?> ValidarUnicidadAsync(
        long administradorId,
        string accion,
        Dispositivo dispositivo,
        long? dispositivoIdExcluido,
        CancellationToken cancellationToken)
    {
        if (await repositorioDispositivos.ExisteNombreAsync(
                dispositivo.Nombre,
                dispositivoIdExcluido,
                cancellationToken))
        {
            return await FallarAsync(
                administradorId,
                accion,
                dispositivoIdExcluido,
                CodigosErrorOperacion.Conflicto,
                "Ya existe un dispositivo con el mismo nombre.",
                cancellationToken);
        }

        if (await repositorioDispositivos.ExisteConexionAsync(
                dispositivo.Host,
                dispositivo.Puerto,
                dispositivoIdExcluido,
                cancellationToken))
        {
            return await FallarAsync(
                administradorId,
                accion,
                dispositivoIdExcluido,
                CodigosErrorOperacion.Conflicto,
                "Ya existe un dispositivo con el mismo host y puerto.",
                cancellationToken);
        }

        return null;
    }

    private async Task<ResultadoOperacion<Usuario>?> ValidarAdministradorAsync(
        long administradorId,
        CancellationToken cancellationToken)
    {
        Usuario? administrador = await repositorioUsuarios.ObtenerPorIdAsync(
            administradorId,
            false,
            cancellationToken);

        if (administrador is null
            || !administrador.Estado
            || !administrador.Rol.Estado
            || !string.Equals(
                administrador.Rol.Nombre,
                RolesSistema.Administrador,
                StringComparison.Ordinal))
        {
            return ResultadoOperacion<Usuario>.Fallido(
                CodigosErrorOperacion.Prohibido,
                "La operación requiere un administrador activo.");
        }

        return null;
    }

    private void RegistrarAuditoria(
        long administradorId,
        string accion,
        long dispositivoId,
        string detalle)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            administradorId,
            accion,
            "Dispositivo",
            dispositivoId,
            reloj.GetUtcNow(),
            EstadoAuditoria.Exitoso,
            detalle));
    }

    private async Task<ResultadoOperacion<DispositivoResumen>> FallarAsync(
        long administradorId,
        string accion,
        long? dispositivoId,
        string codigo,
        string mensaje,
        CancellationToken cancellationToken)
    {
        repositorioAuditorias.Agregar(new Auditoria(
            administradorId,
            accion,
            "Dispositivo",
            dispositivoId,
            reloj.GetUtcNow(),
            EstadoAuditoria.Fallido,
            mensaje));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<DispositivoResumen>.Fallido(codigo, mensaje);
    }

    private static Dispositivo ConstruirDispositivo(
        string nombre,
        string host,
        string tipo,
        string? modelo,
        string protocolo,
        int puerto,
        string? fuenteEventos)
    {
        if (!IntentarInterpretarProtocolo(protocolo, out ProtocoloConexion valorProtocolo))
        {
            throw new ArgumentException(
                "El protocolo debe ser Ssh o Netconf.",
                nameof(protocolo));
        }

        if (!IntentarInterpretarFuente(fuenteEventos, out FuenteEvento? valorFuente))
        {
            throw new ArgumentException(
                "La fuente de eventos debe ser SnmpTrap, SnmpInform, Syslog o quedar vacía.",
                nameof(fuenteEventos));
        }

        return new Dispositivo(
            nombre,
            host,
            tipo,
            modelo,
            valorProtocolo,
            puerto,
            valorFuente);
    }

    private static bool IntentarInterpretarProtocolo(
        string? valor,
        out ProtocoloConexion protocolo)
    {
        if (string.Equals(valor, nameof(ProtocoloConexion.Ssh), StringComparison.OrdinalIgnoreCase))
        {
            protocolo = ProtocoloConexion.Ssh;
            return true;
        }

        if (string.Equals(
                valor,
                nameof(ProtocoloConexion.Netconf),
                StringComparison.OrdinalIgnoreCase))
        {
            protocolo = ProtocoloConexion.Netconf;
            return true;
        }

        protocolo = default;
        return false;
    }

    private static bool IntentarInterpretarFuente(
        string? valor,
        out FuenteEvento? fuente)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            fuente = null;
            return true;
        }

        foreach (FuenteEvento candidata in Enum.GetValues<FuenteEvento>())
        {
            if (string.Equals(valor, candidata.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                fuente = candidata;
                return true;
            }
        }

        fuente = null;
        return false;
    }

    private static bool IntentarInterpretarEstado(
        string? valor,
        out EstadoDispositivo estado)
    {
        foreach (EstadoDispositivo candidato in Enum.GetValues<EstadoDispositivo>())
        {
            if (string.Equals(valor, candidato.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                estado = candidato;
                return true;
            }
        }

        estado = default;
        return false;
    }

    private static DispositivoResumen Mapear(Dispositivo dispositivo)
    {
        return new DispositivoResumen(
            dispositivo.Id,
            dispositivo.Nombre,
            dispositivo.Host,
            dispositivo.Tipo,
            dispositivo.Modelo,
            dispositivo.Protocolo.ToString(),
            dispositivo.Puerto,
            dispositivo.FuenteEventos?.ToString(),
            dispositivo.Estado.ToString());
    }
}
