namespace Jason.Web.Services;

// Error que devolvió la API (ya con su mensaje en español), para mostrárselo al usuario tal cual
public class ApiClientException : Exception
{
    public int StatusCode { get; }

    public ApiClientException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
