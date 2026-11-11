using System.Text.Json;
using Jason.DataAccess.Repositories;
using Jason.Models.Entities;

namespace Jason.Tests.Fakes;

// Repositorios en memoria para probar los servicios sin base de datos.
// Imitan lo que hace EF: al crear una entidad le asignan su Id.

public class FakeSourceRepository : ISourceRepository
{
    public List<Source> Data { get; } = new();

    public Task<IEnumerable<Source>> ReadAsync() => Task.FromResult<IEnumerable<Source>>(Data.ToList());

    public Task<Source?> FindAsync(int id) => Task.FromResult(Data.FirstOrDefault(s => s.Id == id));

    public Task<bool> CreateAsync(Source entity)
    {
        entity.Id = Data.Count == 0 ? 1 : Data.Max(s => s.Id) + 1;
        Data.Add(entity);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateAsync(Source entity) => Task.FromResult(true);

    public Task<bool> DeleteAsync(Source entity) => Task.FromResult(Data.Remove(entity));

    public Task<Source?> FindByUrlAsync(string url) => Task.FromResult(Data.FirstOrDefault(s => s.Url == url));
}

public class FakeSourceItemRepository : ISourceItemRepository
{
    public List<SourceItem> Data { get; } = new();

    public Task<IEnumerable<SourceItem>> ReadAsync() => Task.FromResult<IEnumerable<SourceItem>>(Data.ToList());

    public Task<SourceItem?> FindAsync(int id) => Task.FromResult(Data.FirstOrDefault(i => i.Id == id));

    public Task<bool> CreateAsync(SourceItem entity)
    {
        entity.Id = Data.Count == 0 ? 1 : Data.Max(i => i.Id) + 1;
        Data.Add(entity);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateAsync(SourceItem entity) => Task.FromResult(true);

    public Task<bool> DeleteAsync(SourceItem entity) => Task.FromResult(Data.Remove(entity));

    public Task<IEnumerable<SourceItem>> ReadBySourceAsync(int sourceId) =>
        Task.FromResult<IEnumerable<SourceItem>>(Data.Where(i => i.SourceId == sourceId).ToList());

    // Igual que la consulta real: busca el id que viene dentro del Json
    public Task<SourceItem?> FindByExternalIdAsync(int sourceId, string externalId) =>
        Task.FromResult(Data.FirstOrDefault(i => i.SourceId == sourceId && ExternalId(i.Json) == externalId));

    private static string? ExternalId(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
    }
}
