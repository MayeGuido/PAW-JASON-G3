using System.Text.Json;
using Jason.API.Exceptions;
using Jason.DataAccess.Repositories;
using Jason.Models.Dtos;
using Jason.Models.Entities;
using Jason.Models.Exchange;

namespace Jason.API.Services;

public interface IItemService
{
    Task<IEnumerable<SourceItemDto>> GetAllAsync();
    Task<SourceItemDto> SaveAsync(SaveItemDto dto);
}

// Items guardados (tabla SourceItems). Cada fila guarda el item completo en el formato común.
public class ItemService : IItemService
{
    // Se guarda en una sola línea; el formato indentado solo se usa al exportar
    private static readonly JsonSerializerOptions StorageOptions = new(DashboardExport.JsonOptions)
    {
        WriteIndented = false
    };

    private readonly ISourceItemRepository _itemRepository;
    private readonly ISourceRepository _sourceRepository;

    public ItemService(ISourceItemRepository itemRepository, ISourceRepository sourceRepository)
    {
        _itemRepository = itemRepository;
        _sourceRepository = sourceRepository;
    }

    public async Task<IEnumerable<SourceItemDto>> GetAllAsync()
    {
        var items = await _itemRepository.ReadWithSourceAsync();
        return items.Select(ToDto).ToList();
    }

    public async Task<SourceItemDto> SaveAsync(SaveItemDto dto)
    {
        var item = dto.Item;

        if (string.IsNullOrWhiteSpace(item.Id))
            throw ApiException.BadRequest("El item necesita un id.");
        if (string.IsNullOrWhiteSpace(item.Title))
            throw ApiException.BadRequest("El item necesita un título.");
        if (!MediaTypes.All.Contains(item.MediaType))
            throw ApiException.BadRequest("El tipo de contenido del item no es válido.");

        var source = await _sourceRepository.FindAsync(dto.SourceId)
            ?? throw ApiException.NotFound("La fuente indicada no existe.");

        // Mismo item de la misma fuente: no se guarda dos veces
        if (await _itemRepository.FindByExternalIdAsync(source.Id, item.Id) is not null)
            throw ApiException.Conflict("Este item ya está guardado.");

        var entity = new SourceItem
        {
            SourceId = source.Id,
            Json = JsonSerializer.Serialize(item, StorageOptions),
            CreatedAt = DateTime.UtcNow
        };

        await _itemRepository.CreateAsync(entity);
        entity.Source = source;
        return ToDto(entity);
    }

    // La API nunca devuelve la entidad de EF, solo el DTO
    private static SourceItemDto ToDto(SourceItem i) => new()
    {
        Id = i.Id,
        SourceId = i.SourceId,
        SourceName = i.Source?.Name ?? string.Empty,
        Json = i.Json,
        CreatedAt = i.CreatedAt
    };
}
