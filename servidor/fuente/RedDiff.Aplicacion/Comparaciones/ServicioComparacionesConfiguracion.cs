using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Comun;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Comparaciones;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Comparaciones;

public sealed class ServicioComparacionesConfiguracion(
    IRepositorioComparaciones repositorioComparaciones,
    IRepositorioVersionesConfiguracion repositorioVersiones,
    IRepositorioUsuarios repositorioUsuarios,
    IRepositorioAuditorias repositorioAuditorias,
    IUnidadDeTrabajo unidadDeTrabajo,
    ProcesadorComparacionConfiguraciones procesador,
    TimeProvider reloj)
{
    public async Task<ResultadoOperacion<ComparacionDetalle>> CompararAsync(
        long usuarioId,
        CompararVersionesSolicitud solicitud,
        CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            false,
            cancellationToken);
        if (usuario is null || !usuario.Estado || !usuario.Rol.Estado)
        {
            return ResultadoOperacion<ComparacionDetalle>.Fallido(
                CodigosErrorOperacion.Prohibido,
                "La operación requiere un usuario activo.");
        }

        if (solicitud.VersionOrigenId <= 0 || solicitud.VersionDestinoId <= 0)
        {
            return Fallar("Debes seleccionar dos versiones válidas.");
        }

        if (solicitud.VersionOrigenId == solicitud.VersionDestinoId)
        {
            return Fallar("Debes seleccionar dos versiones diferentes.");
        }

        VersionConfiguracion? origen = await repositorioVersiones.ObtenerPorIdAsync(
            solicitud.VersionOrigenId,
            cancellationToken);
        VersionConfiguracion? destino = await repositorioVersiones.ObtenerPorIdAsync(
            solicitud.VersionDestinoId,
            cancellationToken);
        if (origen is null || destino is null)
        {
            return ResultadoOperacion<ComparacionDetalle>.Fallido(
                CodigosErrorOperacion.NoEncontrado,
                "Una de las versiones seleccionadas no existe.");
        }

        if (origen.DispositivoId != destino.DispositivoId)
        {
            return Fallar("Solo se pueden comparar versiones del mismo dispositivo.");
        }

        DateTimeOffset fecha = reloj.GetUtcNow();
        Comparacion comparacion = new(origen.Id, destino.Id, usuario.Id, fecha);
        comparacion.Iniciar();
        repositorioComparaciones.Agregar(comparacion);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        IReadOnlyList<DiferenciaCalculada> diferencias = procesador.Comparar(
            origen.Contenido,
            destino.Contenido);
        for (int indice = 0; indice < diferencias.Count; indice++)
        {
            DiferenciaCalculada diferencia = diferencias[indice];
            comparacion.Diferencias.Add(new DetalleDiferencia(
                comparacion.Id,
                diferencia.Linea,
                ObtenerTipo(diferencia),
                diferencia.TextoAnterior,
                diferencia.TextoNuevo));
        }

        comparacion.Completar();
        repositorioAuditorias.Agregar(new Auditoria(
            usuario.Id,
            "CompararVersionesConfiguracion",
            "Comparacion",
            comparacion.Id,
            fecha,
            EstadoAuditoria.Exitoso,
            $"Se compararon las versiones {origen.Numero} y {destino.Numero} "
                + $"del dispositivo {origen.Dispositivo.Nombre}; "
                + $"se identificaron {diferencias.Count} diferencias."));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<ComparacionDetalle>.Correcto(
            MapearDetalle(comparacion, origen, destino, usuario.NombreUsuario));
    }

    public async Task<ResultadoOperacion<IReadOnlyList<ComparacionResumen>>> ListarAsync(
        long? dispositivoId,
        CancellationToken cancellationToken = default)
    {
        if (dispositivoId.HasValue && dispositivoId.Value <= 0)
        {
            return ResultadoOperacion<IReadOnlyList<ComparacionResumen>>.Fallido(
                CodigosErrorOperacion.Validacion,
                "El identificador del dispositivo no es válido.");
        }

        IReadOnlyList<Comparacion> comparaciones = await repositorioComparaciones.ListarAsync(
            dispositivoId,
            cancellationToken);
        return ResultadoOperacion<IReadOnlyList<ComparacionResumen>>.Correcto(
            comparaciones.Select(MapearResumen).ToArray());
    }

    public async Task<ResultadoOperacion<ComparacionDetalle>> ObtenerAsync(
        long comparacionId,
        CancellationToken cancellationToken = default)
    {
        if (comparacionId <= 0)
        {
            return ResultadoOperacion<ComparacionDetalle>.Fallido(
                CodigosErrorOperacion.Validacion,
                "El identificador de la comparación no es válido.");
        }

        Comparacion? comparacion = await repositorioComparaciones.ObtenerPorIdAsync(
            comparacionId,
            cancellationToken);
        return comparacion is null
            ? ResultadoOperacion<ComparacionDetalle>.Fallido(
                CodigosErrorOperacion.NoEncontrado,
                "La comparación solicitada no existe.")
            : ResultadoOperacion<ComparacionDetalle>.Correcto(MapearDetalle(comparacion));
    }

    private static ResultadoOperacion<ComparacionDetalle> Fallar(string mensaje)
    {
        return ResultadoOperacion<ComparacionDetalle>.Fallido(
            CodigosErrorOperacion.Validacion,
            mensaje);
    }

    private static TipoDiferencia ObtenerTipo(DiferenciaCalculada diferencia)
    {
        if (diferencia.TextoAnterior is null)
        {
            return TipoDiferencia.Agregada;
        }

        return diferencia.TextoNuevo is null
            ? TipoDiferencia.Eliminada
            : TipoDiferencia.Modificada;
    }

    private static ComparacionResumen MapearResumen(Comparacion comparacion)
    {
        return new ComparacionResumen(
            comparacion.Id,
            comparacion.VersionOrigen.DispositivoId,
            comparacion.VersionOrigen.Dispositivo.Nombre,
            MapearVersion(comparacion.VersionOrigen),
            MapearVersion(comparacion.VersionDestino),
            comparacion.Usuario.NombreUsuario,
            comparacion.Fecha,
            comparacion.Estado.ToString(),
            comparacion.Diferencias.Count(d => d.Tipo == TipoDiferencia.Agregada),
            comparacion.Diferencias.Count(d => d.Tipo == TipoDiferencia.Eliminada),
            comparacion.Diferencias.Count(d => d.Tipo == TipoDiferencia.Modificada));
    }

    private static ComparacionDetalle MapearDetalle(Comparacion comparacion)
    {
        ComparacionResumen resumen = MapearResumen(comparacion);
        return new ComparacionDetalle(
            resumen.Id,
            resumen.DispositivoId,
            resumen.Dispositivo,
            resumen.VersionOrigen,
            resumen.VersionDestino,
            resumen.Usuario,
            resumen.Fecha,
            resumen.Estado,
            resumen.Agregadas,
            resumen.Eliminadas,
            resumen.Modificadas,
            comparacion.Diferencias
                .OrderBy(diferencia => diferencia.Linea)
                .Select(MapearDiferencia)
                .ToArray());
    }

    private static ComparacionDetalle MapearDetalle(
        Comparacion comparacion,
        VersionConfiguracion origen,
        VersionConfiguracion destino,
        string usuario)
    {
        IReadOnlyList<DiferenciaComparacionResumen> diferencias = comparacion.Diferencias
            .OrderBy(diferencia => diferencia.Linea)
            .Select(MapearDiferencia)
            .ToArray();
        return new ComparacionDetalle(
            comparacion.Id,
            origen.DispositivoId,
            origen.Dispositivo.Nombre,
            MapearVersion(origen),
            MapearVersion(destino),
            usuario,
            comparacion.Fecha,
            comparacion.Estado.ToString(),
            diferencias.Count(d => d.Tipo == TipoDiferencia.Agregada.ToString()),
            diferencias.Count(d => d.Tipo == TipoDiferencia.Eliminada.ToString()),
            diferencias.Count(d => d.Tipo == TipoDiferencia.Modificada.ToString()),
            diferencias);
    }

    private static VersionComparadaResumen MapearVersion(VersionConfiguracion version)
    {
        return new VersionComparadaResumen(
            version.Id,
            version.Numero,
            version.Hash,
            version.CapturadaEn,
            version.Estable);
    }

    private static DiferenciaComparacionResumen MapearDiferencia(DetalleDiferencia diferencia)
    {
        return new DiferenciaComparacionResumen(
            diferencia.Linea,
            diferencia.Tipo.ToString(),
            diferencia.TextoAnterior,
            diferencia.TextoNuevo);
    }
}
