using System.Net.Http.Headers;
using System.Text.Json;
using Jason.Models.Dtos;
using Jason.Models.Exchange;

namespace Jason.Web.Services;

// Servicio de la Web que llama a nuestra API (como el servicio del tipo de cambio visto en clase).
// Si la API responde con un error, lanza ApiClientException con el mensaje que ella mandó.
public interface IJasonApiClient
{
    Task<IReadOnlyList<SourceDto>> GetSourcesAsync();
    Task<SourceDto> CreateSourceAsync(CreateSourceDto source);
    Task DeleteSourceAsync(int id);

    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

    Task<IReadOnlyList<UserDto>> GetUsersAsync();
    Task ChangeRoleAsync(string userId, string role);

    Task<IReadOnlyList<SecretDto>> GetSecretsAsync();
    Task SaveSecretAsync(SaveSecretDto dto);
    Task DeleteSecretAsync(string key);
    Task<DashboardExport> GetFeedAsync();
    Task<IReadOnlyList<SourceItemDto>> GetItemsAsync();
    Task<SourceItemDto> SaveItemAsync(SaveItemDto dto);

    // Descargar e importar archivos JSON del formato común
    Task<ExportFileDto> ExportItemAsync(int itemId);
    Task<ExportFileDto> ExportAllAsync();

    // preview = true: la API solo revisa el archivo y dice qué pasaría, sin guardar nada
    Task<ImportResultDto> ImportFileAsync(Stream content, string fileName, bool preview);
}

public class JasonApiClient : IJasonApiClient
{
    private readonly HttpClient _http;

    public JasonApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<SourceDto>> GetSourcesAsync() =>
        await GetAsync<List<SourceDto>>("api/sources");

    public async Task<SourceDto> CreateSourceAsync(CreateSourceDto source) =>
        await SendAsync<SourceDto>(await _http.PostAsJsonAsync("api/sources", source));

    public async Task DeleteSourceAsync(int id) =>
        await SendAsync(await _http.DeleteAsync($"api/sources/{id}"));

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto) =>
        await SendAsync<AuthResponseDto>(await _http.PostAsJsonAsync("api/auth/login", dto));

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto) =>
        await SendAsync<AuthResponseDto>(await _http.PostAsJsonAsync("api/auth/register", dto));

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync() =>
        await GetAsync<List<UserDto>>("api/users");

    public async Task ChangeRoleAsync(string userId, string role) =>
        await SendAsync(await _http.PutAsJsonAsync($"api/users/{Uri.EscapeDataString(userId)}/role", new ChangeRoleDto { Role = role }));

    public async Task<IReadOnlyList<SecretDto>> GetSecretsAsync() =>
        await GetAsync<List<SecretDto>>("api/settings/secrets");

    public async Task SaveSecretAsync(SaveSecretDto dto) =>
        await SendAsync(await _http.PutAsJsonAsync("api/settings/secrets", dto));

    public async Task DeleteSecretAsync(string key) =>
        await SendAsync(await _http.DeleteAsync($"api/settings/secrets/{Uri.EscapeDataString(key)}"));

    public async Task<DashboardExport> GetFeedAsync() =>
     await GetAsync<DashboardExport>("api/feed");

    public async Task<IReadOnlyList<SourceItemDto>> GetItemsAsync() =>
        await GetAsync<List<SourceItemDto>>("api/items");

    public async Task<SourceItemDto> SaveItemAsync(SaveItemDto dto) =>
        await SendAsync<SourceItemDto>(await _http.PostAsJsonAsync("api/items", dto));

    public async Task<ExportFileDto> ExportItemAsync(int itemId) =>
        await DownloadAsync($"api/items/{itemId}/export", $"ficha-{itemId}.json");

    public async Task<ExportFileDto> ExportAllAsync() =>
        await DownloadAsync("api/items/export", "jason-fichas.json");

    public async Task<ImportResultDto> ImportFileAsync(Stream content, string fileName, bool preview)
    {
        using var form = new MultipartFormDataContent();
        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        form.Add(file, "file", fileName);
        return await SendAsync<ImportResultDto>(await _http.PostAsync($"api/items/import?preview={(preview ? "true" : "false")}", form));
    }

    // Baja un archivo JSON de la API y toma su nombre del encabezado Content-Disposition
    private async Task<ExportFileDto> DownloadAsync(string url, string fallbackName)
    {
        using var response = await _http.GetAsync(url);
        await EnsureSuccessAsync(response);

        var disposition = response.Content.Headers.ContentDisposition;
        var fileName = disposition?.FileNameStar ?? disposition?.FileName?.Trim('"') ?? fallbackName;
        return new ExportFileDto { FileName = fileName, Json = await response.Content.ReadAsStringAsync() };
    }

    private async Task<T> GetAsync<T>(string url) => await SendAsync<T>(await _http.GetAsync(url));

    // Respuestas sin cuerpo: solo importa si fue exitosa
    private static async Task SendAsync(HttpResponseMessage response)
    {
        using (response)
        {
            await EnsureSuccessAsync(response);
        }
    }

    private static async Task<T> SendAsync<T>(HttpResponseMessage response)
    {
        using (response)
        {
            await EnsureSuccessAsync(response);
            var result = await response.Content.ReadFromJsonAsync<T>();
            return result ?? throw new ApiClientException((int)response.StatusCode, "La API devolvió una respuesta vacía.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = $"La API respondió con el error {(int)response.StatusCode}.";
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
            if (!string.IsNullOrWhiteSpace(error?.Message))
                message = error.Message;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // La respuesta no traía el JSON de error: se queda el mensaje genérico
        }

        throw new ApiClientException((int)response.StatusCode, message);
    }
}
