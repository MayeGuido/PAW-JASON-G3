using System.Text.Json;
using Jason.Models.Dtos;
using Jason.Models.Exchange;
using Jason.Web.Models;
using Jason.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jason.Web.Controllers;

// Pantalla de contenido: fichas del feed (desde las fuentes) y fichas guardadas.
public class ContentController : Controller
{
    // El feed puede traer cientos de items; se muestran los más recientes
    private const int MaxCards = 60;

    private readonly IJasonApiClient _api;
    private readonly ILogger<ContentController> _logger;

    public ContentController(IJasonApiClient api, ILogger<ContentController> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? tab, string? type)
    {
        ContentViewModel model = new();
        model.Type = NormalizeType(type);

        try
        {
            IReadOnlyList<SourceItemDto> savedItems = await _api.GetItemsAsync();
            List<ContentCardViewModel> savedCards = ToSavedCards(savedItems);
            model.SavedCount = savedCards.Count;

            // Sin pestaña elegida: guardados si existen; si no, lo que traen las fuentes.
            // Si piden guardados y no hay ninguno, también se muestran las fuentes.
            string activeTab = tab == ContentViewModel.TabFeed || tab == ContentViewModel.TabSaved
                ? tab
                : (savedCards.Count > 0 ? ContentViewModel.TabSaved : ContentViewModel.TabFeed);

            if (activeTab == ContentViewModel.TabSaved && savedCards.Count == 0)
            {
                activeTab = ContentViewModel.TabFeed;
                model.FellBackToFeed = true;
            }

            model.Tab = activeTab;

            List<ContentCardViewModel> cards = activeTab == ContentViewModel.TabSaved
                ? savedCards
                : await LoadFeedCards(savedCards);

            if (model.Type != null)
                cards = cards.Where(c => string.Equals(c.Item.MediaType, model.Type, StringComparison.OrdinalIgnoreCase)).ToList();

            model.TotalCards = cards.Count;
            model.Cards = cards.Take(MaxCards).ToList();
        }
        catch (ApiClientException exception)
        {
            model.Error = exception.Message;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "No se pudo cargar el contenido");
            model.Error = "No se pudo conectar con la API. Verifica que Jason.API esté corriendo y que la base de datos exista.";
        }

        return View(model);
    }

    // Guarda un item del feed. La Web vuelve a buscarlo en el feed (la API lo tiene en cache),
    // así lo que se guarda es lo que la API trajo y no lo que mande el navegador.
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Save(int sourceId, string itemId, string? type)
    {
        try
        {
            DashboardExport feed = await _api.GetFeedAsync();
            ExportItem? item = feed.Sources
                .Where(s => ParseSourceId(s.Id) == sourceId)
                .SelectMany(s => s.Items)
                .FirstOrDefault(i => i.Id == itemId);

            if (item == null)
            {
                TempData["Error"] = "Ese item ya no está en el feed. Actualiza la página e intenta de nuevo.";
            }
            else
            {
                await _api.SaveItemAsync(new SaveItemDto { SourceId = sourceId, Item = item });
                TempData["Success"] = "La ficha se guardó correctamente.";
            }
        }
        catch (ApiClientException exception)
        {
            TempData["Error"] = exception.Message;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(exception, "No se pudo guardar el item {ItemId}", itemId);
            TempData["Error"] = "No se pudo conectar con la API.";
        }

        return RedirectToAction(nameof(Index), new { tab = ContentViewModel.TabFeed, type = NormalizeType(type) });
    }

    private async Task<List<ContentCardViewModel>> LoadFeedCards(List<ContentCardViewModel> savedCards)
    {
        DashboardExport feed = await _api.GetFeedAsync();

        // Un item guardado se reconoce por su fuente y su id dentro del formato común
        HashSet<string> savedKeys = savedCards
            .Select(c => CreateKey(c.SourceId, c.Item.Id))
            .ToHashSet();

        List<ContentCardViewModel> cards = new();
        foreach (ExportSource source in feed.Sources)
        {
            int sourceId = ParseSourceId(source.Id);
            foreach (ExportItem item in source.Items)
            {
                ContentCardViewModel card = new();
                card.SourceId = sourceId;
                card.SourceName = source.Name;
                card.Item = item;
                card.IsSaved = savedKeys.Contains(CreateKey(sourceId, item.Id));
                cards.Add(card);
            }
        }

        return cards.OrderByDescending(c => c.Item.CreatedAt).ToList();
    }

    private List<ContentCardViewModel> ToSavedCards(IReadOnlyList<SourceItemDto> savedItems)
    {
        List<ContentCardViewModel> cards = new();
        foreach (SourceItemDto saved in savedItems)
        {
            ExportItem? item;
            try
            {
                item = JsonSerializer.Deserialize<ExportItem>(saved.Json, DashboardExport.JsonOptions);
            }
            catch (JsonException exception)
            {
                // Una fila con JSON dañado no debe romper toda la pantalla
                _logger.LogWarning(exception, "El item guardado {ItemId} no tiene un JSON válido", saved.Id);
                continue;
            }

            if (item == null)
                continue;

            ContentCardViewModel card = new();
            card.SourceId = saved.SourceId;
            card.SourceName = saved.SourceName;
            card.Item = item;
            card.IsSaved = true;
            cards.Add(card);
        }

        return cards;
    }

    // El id de la fuente en el feed es un slug que termina en "-{id de la tabla Sources}" (ej. "nasa-apod-3")
    private static int ParseSourceId(string slug)
    {
        int dash = slug.LastIndexOf('-');
        if (dash >= 0 && int.TryParse(slug.AsSpan(dash + 1), out int id))
            return id;

        return 0;
    }

    private static string CreateKey(int sourceId, string itemId) => sourceId + "|" + itemId;

    private static string? NormalizeType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return null;

        return MediaTypes.All.FirstOrDefault(t => string.Equals(t, type, StringComparison.OrdinalIgnoreCase));
    }
}
