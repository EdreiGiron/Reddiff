using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Capturas;
using RedDiff.Aplicacion.Comun;
using RedDiff.Api.Comun;

namespace RedDiff.Api.Controladores;

[ApiController]
[Authorize]
[Route("api/capturas")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CapturasController(
    ServicioCapturasConfiguracion servicio,
    ServicioCapturaRemota servicioCapturaRemota) : ControllerBase
{
    private const long LimiteSolicitudBytes = ProcesadorArchivoConfiguracion.TamanoMaximoBytes
        + 250_000;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] long? dispositivoId,
        [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<IReadOnlyList<CapturaResumen>> resultado =
            await servicio.ListarCapturasAsync(dispositivoId, estado, cancellationToken);

        return resultado.Exitoso ? Ok(resultado.Valor) : this.ComoProblema(resultado);
    }

    [HttpPost("archivo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteSolicitudBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteSolicitudBytes)]
    public async Task<IActionResult> CargarArchivo(
        [FromForm] CargarArchivoFormulario formulario,
        CancellationToken cancellationToken)
    {
        if (formulario.Archivo is null)
        {
            ResultadoOperacion<CargaArchivoResultado> faltante =
                ResultadoOperacion<CargaArchivoResultado>.Fallido(
                    CodigosErrorOperacion.Validacion,
                    "Debe seleccionar un archivo de configuración.");
            return this.ComoProblema(faltante);
        }

        if (formulario.Archivo.Length > ProcesadorArchivoConfiguracion.TamanoMaximoBytes)
        {
            ResultadoOperacion<CargaArchivoResultado> demasiadoGrande =
                ResultadoOperacion<CargaArchivoResultado>.Fallido(
                    CodigosErrorOperacion.Validacion,
                    "El archivo supera el límite de 5 MB.");
            return this.ComoProblema(demasiadoGrande);
        }

        await using MemoryStream memoria = new();
        await formulario.Archivo.CopyToAsync(memoria, cancellationToken);

        CargarArchivoConfiguracionSolicitud solicitud = new(
            formulario.DispositivoId,
            formulario.Archivo.FileName,
            memoria.ToArray(),
            formulario.Comentario);
        ResultadoOperacion<CargaArchivoResultado> resultado = await servicio.CargarArchivoAsync(
            ObtenerUsuarioId(),
            solicitud,
            cancellationToken);

        return resultado.Exitoso
            ? CreatedAtAction(
                nameof(VersionesConfiguracionController.Obtener),
                "VersionesConfiguracion",
                new { versionId = resultado.Valor!.Version.Id },
                resultado.Valor)
            : this.ComoProblema(resultado);
    }

    [HttpPost("remota")]
    public async Task<IActionResult> CapturarRemotamente(
        [FromBody] CapturarConfiguracionRemotaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        ResultadoOperacion<CapturaRemotaResultado> resultado =
            await servicioCapturaRemota.CapturarAsync(
                ObtenerUsuarioId(),
                solicitud.DispositivoId,
                cancellationToken);

        return resultado.Exitoso
            ? CreatedAtAction(
                nameof(VersionesConfiguracionController.Obtener),
                "VersionesConfiguracion",
                new { versionId = resultado.Valor!.Version.Id },
                resultado.Valor)
            : this.ComoProblema(resultado);
    }

    private long ObtenerUsuarioId()
    {
        return long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

public sealed class CargarArchivoFormulario
{
    public long DispositivoId { get; init; }

    public IFormFile? Archivo { get; init; }

    public string? Comentario { get; init; }
}
