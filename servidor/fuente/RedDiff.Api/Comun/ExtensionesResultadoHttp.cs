using Microsoft.AspNetCore.Mvc;
using RedDiff.Aplicacion.Comun;

namespace RedDiff.Api.Comun;

public static class ExtensionesResultadoHttp
{
    public static IActionResult ComoProblema<T>(
        this ControllerBase controlador,
        ResultadoOperacion<T> resultado) where T : class
    {
        ErrorOperacion error = resultado.Error
            ?? throw new InvalidOperationException("El resultado no contiene un error.");

        int estado = error.Codigo switch
        {
            CodigosErrorOperacion.NoEncontrado => StatusCodes.Status404NotFound,
            CodigosErrorOperacion.Conflicto => StatusCodes.Status409Conflict,
            CodigosErrorOperacion.Prohibido => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        ProblemDetails problema = new()
        {
            Status = estado,
            Title = "No fue posible completar la operación.",
            Detail = error.Mensaje
        };
        problema.Extensions["codigo"] = error.Codigo;

        return controlador.StatusCode(estado, problema);
    }
}
