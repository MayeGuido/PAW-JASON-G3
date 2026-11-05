using Jason.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace Jason.DataAccess.Repositories;

// Basado en el RepositoryBase del curso, pero recibe el DbContext por inyección de dependencias.
public interface IRepositoryBase<T> where T : class
{
    Task<IEnumerable<T>> ReadAsync();
    Task<T?> FindAsync(int id);
    Task<bool> CreateAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(T entity);
}

public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected readonly AppDbContext Context;

    protected RepositoryBase(AppDbContext context)
    {
        Context = context;
    }

    protected DbSet<T> DbSet => Context.Set<T>();

    public async Task<IEnumerable<T>> ReadAsync() => await DbSet.AsNoTracking().ToListAsync();

    public async Task<T?> FindAsync(int id) => await DbSet.FindAsync(id);

    public async Task<bool> CreateAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        return await SaveAsync();
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        return await SaveAsync();
    }

    public async Task<bool> DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        return await SaveAsync();
    }

    protected async Task<bool> SaveAsync() => await Context.SaveChangesAsync() > 0;
}
