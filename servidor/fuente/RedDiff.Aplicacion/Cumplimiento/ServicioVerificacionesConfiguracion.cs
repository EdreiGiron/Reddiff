using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Comun;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Cumplimiento;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Cumplimiento;

public sealed class ServicioVerificacionesConfiguracion(
    IRepositorioBaselines repositorioBaselines,
    IRepositorioVerificaciones repositorioVerificaciones,
    IRepositorioVersionesConfiguracion repositorioVersiones,
    IRepositorioUsuarios repositorioUsuarios,
    IRepositorioAuditorias repositorioAuditorias,
    IUnidadDeTrabajo unidadDeTrabajo,
    EvaluadorReglasBaseline evaluador,
    TimeProvider reloj)
{
    public async Task<ResultadoOperacion<VerificacionDetalle>> VerificarAsync(
        long usuarioId,
        VerificarVersionSolicitud solicitud,
        CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await repositorioUsuarios.ObtenerPorIdAsync(
            usuarioId,
            false,
            cancellationToken);
        if (usuario is null || !usuario.Estado || !usuario.Rol.Estado)
        {
            return ResultadoOperacion<VerificacionDetalle>.Fallido(
                CodigosErrorOperacion.Prohibido,
                "La operación requiere un usuario activo.");
        }

        if (solicitud.BaselineId <= 0 || solicitud.VersionId <= 0)
        {
            return Fallar("Debes seleccionar una línea base y una versión válidas.");
        }

        Baseline? baseline = await repositorioBaselines.ObtenerPorIdAsync(
            solicitud.BaselineId,
            false,
            cancellationToken);
        VersionConfiguracion? version = await repositorioVersiones.ObtenerPorIdAsync(
            solicitud.VersionId,
            cancellationToken);
        if (baseline is null || version is null)
        {
            return ResultadoOperacion<VerificacionDetalle>.Fallido(
                CodigosErrorOperacion.NoEncontrado,
                "La línea base o la versión seleccionada no existe.");
        }

        if (!baseline.Activa)
        {
            return ResultadoOperacion<VerificacionDetalle>.Fallido(
                CodigosErrorOperacion.Conflicto,
                "No puede ejecutarse una verificación con una línea base inactiva.");
        }

        if (baseline.Reglas.Count == 0)
        {
            return ResultadoOperacion<VerificacionDetalle>.Fallido(
                CodigosErrorOperacion.Conflicto,
                "La línea base no contiene reglas para evaluar.");
        }

        if (!EsCompatible(baseline, version))
        {
            return Fallar("La versión no corresponde con el alcance de la línea base.");
        }

        DateTimeOffset fecha = reloj.GetUtcNow();
        Verificacion verificacion = new(baseline.Id, version.Id, usuario.Id, fecha);
        verificacion.Iniciar(baseline.Activa);
        repositorioVerificaciones.Agregar(verificacion);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        foreach (ReglaBaseline regla in baseline.Reglas.OrderBy(regla => regla.Id))
        {
            ResultadoEvaluacionRegla evaluacion = evaluador.Evaluar(regla, version.Contenido);
            EstadoResultadoRegla estado = Enum.Parse<EstadoResultadoRegla>(evaluacion.Estado);
            verificacion.Resultados.Add(new ResultadoRegla(
                verificacion.Id,
                regla.Id,
                estado,
                evaluacion.Evidencia));
        }

        verificacion.Completar();
        ResumenCalculado resumen = Calcular(verificacion.Resultados, baseline.Reglas);
        repositorioAuditorias.Agregar(new Auditoria(
            usuario.Id,
            "VerificarCumplimientoBaseline",
            "Verificacion",
            verificacion.Id,
            fecha,
            EstadoAuditoria.Exitoso,
            $"Se verificó la versión {version.Id} con la línea base {baseline.Id}; "
                + $"resultado {resumen.ResultadoGeneral}, {resumen.Cumplidas} cumplidas, "
                + $"{resumen.Incumplidas} incumplidas y {resumen.NoEvaluables} no evaluables."));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        Verificacion? guardada = await repositorioVerificaciones.ObtenerPorIdAsync(
            verificacion.Id,
            cancellationToken);
        return ResultadoOperacion<VerificacionDetalle>.Correcto(MapearDetalle(guardada!));
    }

    public async Task<ResultadoOperacion<IReadOnlyList<VerificacionResumen>>> ListarAsync(
        long? dispositivoId,
        CancellationToken cancellationToken = default)
    {
        if (dispositivoId.HasValue && dispositivoId.Value <= 0)
        {
            return ResultadoOperacion<IReadOnlyList<VerificacionResumen>>.Fallido(
                CodigosErrorOperacion.Validacion,
                "El identificador del dispositivo no es válido.");
        }

        IReadOnlyList<Verificacion> verificaciones = await repositorioVerificaciones.ListarAsync(
            dispositivoId,
            cancellationToken);
        return ResultadoOperacion<IReadOnlyList<VerificacionResumen>>.Correcto(
            verificaciones.Select(MapearResumen).ToArray());
    }

    public async Task<ResultadoOperacion<VerificacionDetalle>> ObtenerAsync(
        long verificacionId,
        CancellationToken cancellationToken = default)
    {
        if (verificacionId <= 0)
        {
            return Fallar("El identificador de la verificación no es válido.");
        }

        Verificacion? verificacion = await repositorioVerificaciones.ObtenerPorIdAsync(
            verificacionId,
            cancellationToken);
        return verificacion is null
            ? ResultadoOperacion<VerificacionDetalle>.Fallido(
                CodigosErrorOperacion.NoEncontrado,
                "La verificación solicitada no existe.")
            : ResultadoOperacion<VerificacionDetalle>.Correcto(MapearDetalle(verificacion));
    }

    private static bool EsCompatible(Baseline baseline, VersionConfiguracion version)
    {
        return baseline.DispositivoId.HasValue
            ? baseline.DispositivoId.Value == version.DispositivoId
            : string.Equals(
                baseline.TipoDispositivo,
                version.Dispositivo.Tipo,
                StringComparison.OrdinalIgnoreCase);
    }

    private static VerificacionResumen MapearResumen(Verificacion verificacion)
    {
        ResumenCalculado resumen = Calcular(
            verificacion.Resultados,
            verificacion.Baseline.Reglas);
        return new VerificacionResumen(
            verificacion.Id,
            verificacion.BaselineId,
            verificacion.Baseline.Nombre,
            verificacion.Version.DispositivoId,
            verificacion.Version.Dispositivo.Nombre,
            verificacion.VersionId,
            verificacion.Version.Numero,
            verificacion.Usuario.NombreUsuario,
            verificacion.Fecha,
            verificacion.Estado.ToString(),
            resumen.ResultadoGeneral,
            resumen.Total,
            resumen.Cumplidas,
            resumen.Incumplidas,
            resumen.NoEvaluables,
            resumen.Porcentaje);
    }

    private static VerificacionDetalle MapearDetalle(Verificacion verificacion)
    {
        VerificacionResumen resumen = MapearResumen(verificacion);
        return new VerificacionDetalle(
            resumen.Id,
            resumen.BaselineId,
            resumen.Baseline,
            resumen.DispositivoId,
            resumen.Dispositivo,
            resumen.VersionId,
            resumen.Version,
            resumen.Usuario,
            resumen.Fecha,
            resumen.Estado,
            resumen.ResultadoGeneral,
            resumen.TotalReglas,
            resumen.Cumplidas,
            resumen.Incumplidas,
            resumen.NoEvaluables,
            resumen.PorcentajeCumplimiento,
            verificacion.Resultados
                .OrderBy(resultado => resultado.ReglaId)
                .Select(resultado => new ResultadoReglaResumen(
                    resultado.ReglaId,
                    resultado.Regla.Criterio.ToString(),
                    resultado.Regla.Esperado,
                    resultado.Regla.Obligatoria,
                    resultado.Estado.ToString(),
                    resultado.Evidencia ?? "Sin evidencia adicional."))
                .ToArray());
    }

    private static ResumenCalculado Calcular(
        IEnumerable<ResultadoRegla> resultados,
        IEnumerable<ReglaBaseline> reglas)
    {
        ResultadoRegla[] resultadosMaterializados = resultados.ToArray();
        Dictionary<long, ReglaBaseline> reglasPorId = reglas.ToDictionary(regla => regla.Id);
        int cumplidas = resultadosMaterializados.Count(
            resultado => resultado.Estado == EstadoResultadoRegla.Cumplida);
        int incumplidas = resultadosMaterializados.Count(
            resultado => resultado.Estado == EstadoResultadoRegla.Incumplida);
        int noEvaluables = resultadosMaterializados.Count(
            resultado => resultado.Estado == EstadoResultadoRegla.NoEvaluable);
        bool obligatoriaIncumplida = resultadosMaterializados.Any(resultado =>
            resultado.Estado == EstadoResultadoRegla.Incumplida
            && reglasPorId.GetValueOrDefault(resultado.ReglaId)?.Obligatoria == true);
        bool obligatoriaNoEvaluable = resultadosMaterializados.Any(resultado =>
            resultado.Estado == EstadoResultadoRegla.NoEvaluable
            && reglasPorId.GetValueOrDefault(resultado.ReglaId)?.Obligatoria == true);
        string resultadoGeneral = obligatoriaIncumplida
            ? "Incumple"
            : obligatoriaNoEvaluable
                ? "NoEvaluable"
                : "Cumple";
        decimal porcentaje = resultadosMaterializados.Length == 0
            ? 0
            : Math.Round(
                cumplidas * 100m / resultadosMaterializados.Length,
                2,
                MidpointRounding.AwayFromZero);

        return new ResumenCalculado(
            resultadoGeneral,
            resultadosMaterializados.Length,
            cumplidas,
            incumplidas,
            noEvaluables,
            porcentaje);
    }

    private static ResultadoOperacion<VerificacionDetalle> Fallar(string mensaje)
    {
        return ResultadoOperacion<VerificacionDetalle>.Fallido(
            CodigosErrorOperacion.Validacion,
            mensaje);
    }

    private sealed record ResumenCalculado(
        string ResultadoGeneral,
        int Total,
        int Cumplidas,
        int Incumplidas,
        int NoEvaluables,
        decimal Porcentaje);
}
