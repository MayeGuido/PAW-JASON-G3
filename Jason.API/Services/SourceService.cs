using Jason.API.Exceptions;
using Jason.DataAccess.Repositories;
using Jason.Models.Dtos;
using Jason.Models.Entities;

namespace Jason.API.Services;

public interface ISourceService
{
    Task<IEnumerable<SourceDto>> GetAllAsync();
    Task<SourceDto> CreateAsync(CreateSourceDto dto);
    Task DeleteAsync(int id);
}

public class SourceService : ISourceService
{
    private readonly ISourceRepository _repository;

    public SourceService(ISourceRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SourceDto>> GetAllAsync()
    {
        var sources = await _repository.ReadAsync();
        return sources.Select(ToDto);
    }

    public async Task<SourceDto> CreateAsync(CreateSourceDto dto)
    {
        if (await _repository.FindByUrlAsync(dto.Url) is not null)
            throw ApiException.Conflict("Ya existe una fuente con esa URL.");

        var source = new Source
        {
            Name = dto.Name.Trim(),
            Url = dto.Url.Trim(),
            Description = dto.Description?.Trim(),
            ComponentType = dto.ComponentType,
            RequiresSecret = dto.RequiresSecret
        };

        await _repository.CreateAsync(source);
        return ToDto(source);
    }

    public async Task DeleteAsync(int id)
    {
        var source = await _repository.FindAsync(id)
            ?? throw ApiException.NotFound("La fuente indicada no existe.");
        await _repository.DeleteAsync(source);
    }

    // La API nunca devuelve la entidad de EF, solo el DTO
    private static SourceDto ToDto(Source s) => new()
    {
        Id = s.Id,
        Url = s.Url,
        Name = s.Name,
        Description = s.Description,
        ComponentType = s.ComponentType,
        RequiresSecret = s.RequiresSecret
    };
}
