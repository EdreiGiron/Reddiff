using System.Text.RegularExpressions;
using RedDiff.Aplicacion.Abstracciones.Persistencia;
using RedDiff.Aplicacion.Comun;
using RedDiff.Aplicacion.Seguridad;
using RedDiff.Dominio.Entidades.Capturas;
using RedDiff.Dominio.Entidades.Cumplimiento;
using RedDiff.Dominio.Entidades.Identidad;
using RedDiff.Dominio.Entidades.Inventario;
using RedDiff.Dominio.Entidades.Trazabilidad;
using RedDiff.Dominio.Enumeraciones;

namespace RedDiff.Aplicacion.Cumplimiento;

public sealed class ServicioBaselines(
    IRepositorioBaselines repositorioBaselines,
    IRepositorioDispositivos repositorioDispositivos,
    IRepositorioVersionesConfiguracion repositorioVersiones,
    IRepositorioUsuarios repositorioUsuarios,
    IRepositorioAuditorias repositorioAuditorias,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj)
{
    private const string AlcanceDispositivo = "Dispositivo";
    private const string AlcanceTipoDispositivo = "TipoDispositivo";

    public async Task<IReadOnlyList<BaselineResumen>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Baseline> baselines = await repositorioBaselines.ListarAsync(
            cancellationToken);
        return baselines.Select(Mapear).ToArray();
    }

    public async Task<ResultadoOperacion<BaselineResumen>> ObtenerAsync(
        long baselineId,
        CancellationToken cancellationToken = default)
    {
        if (baselineId <= 0)
        {
            return Fallar("El identificador de la línea base no es válido.");
        }

        Baseline? baseline = await repositorioBaselines.ObtenerPorIdAsync(
            baselineId,
            false,
            cancellationToken);
        return baseline is null
            ? ResultadoOperacion<BaselineResumen>.Fallido(
                CodigosErrorOperacion.NoEncontrado,
                "La línea base solicitada no existe.")
            : ResultadoOperacion<BaselineResumen>.Correcto(Mapear(baseline));
    }

    public async Task<ResultadoOperacion<BaselineResumen>> CrearAsync(
        long administradorId,
        CrearBaselineSolicitud solicitud,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<Usuario>? autorizacion = await ValidarAdministradorAsync(
            administradorId,
            cancellationToken);
        if (autorizacion is not null)
        {
            return ResultadoOperacion<BaselineResumen>.Fallido(
                autorizacion.Error!.Codigo,
                autorizacion.Error.Mensaje);
        }

        ResultadoOperacion<IReadOnlyList<ReglaValidada>> reglasResultado = ValidarReglas(
            solicitud.Reglas);
        if (!reglasResultado.Exitoso)
        {
            return Fallar(reglasResultado.Error!.Mensaje);
        }

        Dispositivo? dispositivo = null;
        string? tipoDispositivo = null;
        Baseline baseline;
        try
        {
            if (string.Equals(solicitud.Alcance, AlcanceDispositivo, StringComparison.Ordinal))
            {
                if (!solicitud.DispositivoId.HasValue || solicitud.DispositivoId.Value <= 0)
                {
                    return Fallar("Debes seleccionar el dispositivo de la línea base.");
                }

                if (!string.IsNullOrWhiteSpace(solicitud.TipoDispositivo))
                {
                    return Fallar("Una línea base por dispositivo no admite un tipo adicional.");
                }

                dispositivo = await repositorioDispositivos.ObtenerPorIdAsync(
                    solicitud.DispositivoId.Value,
                    false,
                    cancellationToken);
                if (dispositivo is null)
                {
                    return ResultadoOperacion<BaselineResumen>.Fallido(
                        CodigosErrorOperacion.NoEncontrado,
                        "El dispositivo seleccionado no existe.");
                }

                baseline = Baseline.ParaDispositivo(dispositivo.Id, solicitud.Nombre);
            }
            else if (string.Equals(
                solicitud.Alcance,
                AlcanceTipoDispositivo,
                StringComparison.Ordinal))
            {
                if (solicitud.DispositivoId.HasValue)
                {
                    return Fallar("Una línea base por tipo no admite un dispositivo específico.");
                }

                tipoDispositivo = solicitud.TipoDispositivo?.Trim();
                baseline = Baseline.ParaTipoDispositivo(tipoDispositivo!, solicitud.Nombre);
                IReadOnlyList<Dispositivo> dispositivos = await repositorioDispositivos.ListarAsync(
                    cancellationToken);
                if (!dispositivos.Any(item => string.Equals(
                        item.Tipo,
                        tipoDispositivo,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    return Fallar("El tipo seleccionado no corresponde con un dispositivo registrado.");
                }
            }
            else
            {
                return Fallar("El alcance debe ser Dispositivo o TipoDispositivo.");
            }
        }
        catch (ArgumentException excepcion)
        {
            return Fallar(excepcion.Message);
        }

        VersionConfiguracion? versionReferencia = null;
        if (solicitud.VersionReferenciaId.HasValue)
        {
            if (solicitud.VersionReferenciaId.Value <= 0)
            {
                return Fallar("La versión de referencia no es válida.");
            }

            versionReferencia = await repositorioVersiones.ObtenerPorIdConSeguimientoAsync(
                solicitud.VersionReferenciaId.Value,
                cancellationToken);
            if (versionReferencia is null)
            {
                return ResultadoOperacion<BaselineResumen>.Fallido(
                    CodigosErrorOperacion.NoEncontrado,
                    "La versión de referencia seleccionada no existe.");
            }

            if (!EsCompatible(baseline, versionReferencia))
            {
                return Fallar("La versión de referencia no corresponde con el alcance de la línea base.");
            }

            if (versionReferencia.Estado != EstadoVersion.Activa)
            {
                return ResultadoOperacion<BaselineResumen>.Fallido(
                    CodigosErrorOperacion.Conflicto,
                    "Una versión retirada no puede utilizarse como referencia estable.");
            }

            baseline.CambiarVersionReferencia(versionReferencia.Id);
        }

        repositorioBaselines.Agregar(baseline);
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        foreach (ReglaValidada regla in reglasResultado.Valor!)
        {
            baseline.Reglas.Add(new ReglaBaseline(
                baseline.Id,
                regla.Criterio,
                regla.Esperado,
                regla.Obligatoria));
        }

        if (versionReferencia is not null)
        {
            versionReferencia.MarcarComoEstable(administradorId, reloj.GetUtcNow());
        }

        repositorioAuditorias.Agregar(new Auditoria(
            administradorId,
            "CrearBaseline",
            "Baseline",
            baseline.Id,
            reloj.GetUtcNow(),
            EstadoAuditoria.Exitoso,
            $"Se creó la línea base {baseline.Id} de alcance {solicitud.Alcance} con "
                + $"{baseline.Reglas.Count} reglas y versión de referencia "
                + $"{baseline.VersionReferenciaId?.ToString() ?? "no definida"}."));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<BaselineResumen>.Correcto(
            Mapear(baseline, dispositivo, tipoDispositivo, versionReferencia));
    }

    public async Task<ResultadoOperacion<BaselineResumen>> CambiarEstadoAsync(
        long administradorId,
        long baselineId,
        bool activa,
        CancellationToken cancellationToken = default)
    {
        ResultadoOperacion<Usuario>? autorizacion = await ValidarAdministradorAsync(
            administradorId,
            cancellationToken);
        if (autorizacion is not null)
        {
            return ResultadoOperacion<BaselineResumen>.Fallido(
                autorizacion.Error!.Codigo,
                autorizacion.Error.Mensaje);
        }

        Baseline? baseline = await repositorioBaselines.ObtenerPorIdAsync(
            baselineId,
            true,
            cancellationToken);
        if (baseline is null)
        {
            return ResultadoOperacion<BaselineResumen>.Fallido(
                CodigosErrorOperacion.NoEncontrado,
                "La línea base solicitada no existe.");
        }

        if (activa)
        {
            baseline.Activar();
        }
        else
        {
            baseline.Desactivar();
        }

        repositorioAuditorias.Agregar(new Auditoria(
            administradorId,
            activa ? "ActivarBaseline" : "DesactivarBaseline",
            "Baseline",
            baseline.Id,
            reloj.GetUtcNow(),
            EstadoAuditoria.Exitoso,
            activa
                ? $"Se activó la línea base {baseline.Id}."
                : $"Se desactivó la línea base {baseline.Id}."));
        await unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<BaselineResumen>.Correcto(Mapear(baseline));
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

    private static ResultadoOperacion<IReadOnlyList<ReglaValidada>> ValidarReglas(
        IReadOnlyList<CrearReglaBaselineSolicitud>? solicitudes)
    {
        if (solicitudes is null || solicitudes.Count is < 1 or > 100)
        {
            return ResultadoOperacion<IReadOnlyList<ReglaValidada>>.Fallido(
                CodigosErrorOperacion.Validacion,
                "La línea base debe contener entre 1 y 100 reglas.");
        }

        List<ReglaValidada> reglas = [];
        HashSet<string> unicas = new(StringComparer.Ordinal);
        foreach (CrearReglaBaselineSolicitud solicitud in solicitudes)
        {
            if (!Enum.TryParse(solicitud.Criterio, true, out TipoCriterioRegla criterio)
                || !Enum.IsDefined(criterio))
            {
                return ReglasInvalidas(
                    "Cada criterio debe ser Contiene, NoContiene o CoincideExpresionRegular.");
            }

            if (string.IsNullOrWhiteSpace(solicitud.Esperado)
                || solicitud.Esperado.Length > 10_000)
            {
                return ReglasInvalidas(
                    "El contenido esperado de cada regla es obligatorio y admite hasta 10000 caracteres.");
            }

            if (criterio == TipoCriterioRegla.CoincideExpresionRegular
                && !ExpresionValida(solicitud.Esperado))
            {
                return ReglasInvalidas("Una de las expresiones regulares no es válida.");
            }

            string clave = $"{criterio}\u001f{solicitud.Esperado}";
            if (!unicas.Add(clave))
            {
                return ReglasInvalidas("La línea base contiene una regla duplicada.");
            }

            reglas.Add(new ReglaValidada(criterio, solicitud.Esperado, solicitud.Obligatoria));
        }

        return ResultadoOperacion<IReadOnlyList<ReglaValidada>>.Correcto(reglas);
    }

    private static bool ExpresionValida(string expresion)
    {
        try
        {
            _ = Regex.IsMatch(
                string.Empty,
                expresion,
                RegexOptions.CultureInvariant | RegexOptions.Multiline,
                TimeSpan.FromMilliseconds(250));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static ResultadoOperacion<IReadOnlyList<ReglaValidada>> ReglasInvalidas(
        string mensaje)
    {
        return ResultadoOperacion<IReadOnlyList<ReglaValidada>>.Fallido(
            CodigosErrorOperacion.Validacion,
            mensaje);
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

    private static BaselineResumen Mapear(Baseline baseline)
    {
        return Mapear(
            baseline,
            baseline.Dispositivo,
            baseline.TipoDispositivo,
            baseline.VersionReferencia);
    }

    private static BaselineResumen Mapear(
        Baseline baseline,
        Dispositivo? dispositivo,
        string? tipoDispositivo,
        VersionConfiguracion? versionReferencia)
    {
        return new BaselineResumen(
            baseline.Id,
            baseline.Nombre,
            baseline.DispositivoId.HasValue ? AlcanceDispositivo : AlcanceTipoDispositivo,
            baseline.DispositivoId,
            dispositivo?.Nombre,
            tipoDispositivo,
            baseline.Activa,
            versionReferencia is null
                ? null
                : new VersionReferenciaBaselineResumen(
                    versionReferencia.Id,
                    versionReferencia.Numero,
                    versionReferencia.Dispositivo.Nombre,
                    versionReferencia.Hash,
                    versionReferencia.CapturadaEn,
                    versionReferencia.Estable),
            baseline.Reglas
                .OrderBy(regla => regla.Id)
                .Select(regla => new ReglaBaselineResumen(
                    regla.Id,
                    regla.Criterio.ToString(),
                    regla.Esperado,
                    regla.Obligatoria))
                .ToArray());
    }

    private static ResultadoOperacion<BaselineResumen> Fallar(string mensaje)
    {
        return ResultadoOperacion<BaselineResumen>.Fallido(
            CodigosErrorOperacion.Validacion,
            mensaje);
    }

    private sealed record ReglaValidada(
        TipoCriterioRegla Criterio,
        string Esperado,
        bool Obligatoria);
}
