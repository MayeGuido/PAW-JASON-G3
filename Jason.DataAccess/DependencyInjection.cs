using Jason.DataAccess.Context;
using Jason.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jason.DataAccess;

public static class DependencyInjection
{
    // Registra el DbContext y los repositorios para usarlos con inyección de dependencias
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.CommandTimeout(120)));

        services.AddScoped<ISourceRepository, SourceRepository>();
        services.AddScoped<ISourceItemRepository, SourceItemRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        return services;
    }
}
