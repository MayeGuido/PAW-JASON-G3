namespace Jason.API.Exceptions;

// Error controlado: el middleware lo convierte en una respuesta JSON con el código HTTP indicado
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }

    public ApiException(int statusCode, string title, string message) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public static ApiException BadRequest(string message) => new(StatusCodes.Status400BadRequest, "Solicitud inválida", message);
    public static ApiException Unauthorized(string message) => new(StatusCodes.Status401Unauthorized, "No autenticado", message);
    public static ApiException NotFound(string message) => new(StatusCodes.Status404NotFound, "No encontrado", message);
    public static ApiException Conflict(string message) => new(StatusCodes.Status409Conflict, "Conflicto", message);
    public static ApiException Locked(string message) => new(StatusCodes.Status423Locked, "Cuenta bloqueada", message);
}
