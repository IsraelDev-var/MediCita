using MediCita.Application;
using MediCita.Domain.Comun;
using Microsoft.AspNetCore.Mvc;

namespace MediCita.Api.Middleware;

/// <summary>
/// Traduce las excepciones del dominio a respuestas HTTP con ProblemDetails.
/// Gracias a esto ni el dominio ni la aplicación necesitan conocer códigos HTTP.
/// </summary>
public sealed class MiddlewareDeErrores
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<MiddlewareDeErrores> _log;

    public MiddlewareDeErrores(RequestDelegate siguiente, ILogger<MiddlewareDeErrores> log)
    {
        _siguiente = siguiente;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception ex)
        {
            var (estado, titulo) = Clasificar(ex);

            if (estado >= StatusCodes.Status500InternalServerError)
                _log.LogError(ex, "Error no controlado en {Ruta}", contexto.Request.Path);
            else
                _log.LogInformation("Petición rechazada en {Ruta}: {Mensaje}", contexto.Request.Path, ex.Message);

            var problema = new ProblemDetails
            {
                Status = estado,
                Title = titulo,
                Detail = estado >= StatusCodes.Status500InternalServerError
                    ? "Ocurrió un error inesperado. Intente de nuevo en unos segundos."
                    : ex.Message,
                Instance = contexto.Request.Path
            };

            contexto.Response.Clear();
            contexto.Response.StatusCode = estado;
            contexto.Response.ContentType = "application/problem+json";

            await contexto.Response.WriteAsJsonAsync(problema);
        }
    }

    private static (int Estado, string Titulo) Clasificar(Exception ex) => ex switch
    {
        NoEncontradoException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
        CupoNoDisponibleException => (StatusCodes.Status409Conflict, "El cupo ya no está disponible"),
        CredencialesInvalidasException => (StatusCodes.Status401Unauthorized, "Credenciales inválidas"),
        AccesoDenegadoException => (StatusCodes.Status403Forbidden, "Acceso denegado"),
        ExcepcionDeDominio => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
        _ => (StatusCodes.Status500InternalServerError, "Error interno")
    };
}
