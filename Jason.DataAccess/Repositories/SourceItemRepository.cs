using Jason.DataAccess.Context;
using Jason.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jason.DataAccess.Repositories;

public interface ISourceItemRepository : IRepositoryBase<SourceItem>
{
    Task<IEnumerable<SourceItem>> ReadBySourceAsync(int sourceId);

    // Busca un item por el id que trae dentro del JSON (regla del formato común: no duplicar al importar)
    Task<SourceItem?> FindByExternalIdAsync(int sourceId, string externalId);
}

public class SourceItemRepository : RepositoryBase<SourceItem>, ISourceItemRepository
{
    public SourceItemRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SourceItem>> ReadBySourceAsync(int sourceId) =>
        await DbSet.AsNoTracking()
            .Where(i => i.SourceId == sourceId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

    public async Task<SourceItem?> FindByExternalIdAsync(int sourceId, string externalId) =>
        await DbSet
            .FromSqlInterpolated($"SELECT * FROM SourceItems WHERE SourceId = {sourceId} AND JSON_VALUE([Json], '$.id') = {externalId}")
            .FirstOrDefaultAsync();
}
