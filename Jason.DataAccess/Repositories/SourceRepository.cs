using Jason.DataAccess.Context;
using Jason.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jason.DataAccess.Repositories;

public interface ISourceRepository : IRepositoryBase<Source>
{
    Task<Source?> FindByUrlAsync(string url);
}

public class SourceRepository : RepositoryBase<Source>, ISourceRepository
{
    public SourceRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Source?> FindByUrlAsync(string url) =>
        await DbSet.AsNoTracking().FirstOrDefaultAsync(s => s.Url == url);
}
