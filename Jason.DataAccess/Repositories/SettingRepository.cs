using Jason.DataAccess.Context;
using Jason.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jason.DataAccess.Repositories;

public interface ISettingRepository : IRepositoryBase<Setting>
{
    // Devuelve la entidad con seguimiento, lista para actualizarse
    Task<Setting?> FindByKeyAsync(string key);
}

public class SettingRepository : RepositoryBase<Setting>, ISettingRepository
{
    public SettingRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Setting?> FindByKeyAsync(string key) =>
        await DbSet.FirstOrDefaultAsync(s => s.Key == key);
}
