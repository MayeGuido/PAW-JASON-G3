using Jason.API.Exceptions;
using Jason.Models.Dtos;

namespace Jason.API.Middleware;

// Si algo truena en la API se responde un JSON con el error en vez de la excepción completa
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Error controlado {Status}: {Message}", ex.StatusCode, ex.Message);
            await WriteErrorAsync(context, ex.StatusCode, ex.Title, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError,
                "Error interno", "Ocurrió un error inesperado procesando la solicitud.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int status, string title, string message)
    {
        context.Response.Clear();
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ApiErrorDto
        {
            StatusCode = status,
            Title = title,
            Message = message
        });
    }
}
