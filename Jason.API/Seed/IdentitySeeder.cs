using Jason.DataAccess.Identity;
using Jason.Models.Constants;
using Microsoft.AspNetCore.Identity;

namespace Jason.API.Seed;

// Al arrancar: crea los roles y, si hay credenciales en la configuración, el Admin inicial.
// En desarrollo salen de appsettings.Development.json. En otros ambientes no se crea ningún Admin
// a menos que SeedAdmin:Email y SeedAdmin:Password estén en appsettings.Local.json o variables de entorno.
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        using var scope = services.CreateScope();

        try
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var email = configuration["SeedAdmin:Email"];
            var password = configuration["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogInformation("No hay SeedAdmin configurado: no se crea un Admin inicial.");
                return;
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            if (await userManager.FindByEmailAsync(email) is not null)
                return;

            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = configuration["SeedAdmin:DisplayName"] ?? "Admin"
            };

            var created = await userManager.CreateAsync(admin, password);
            if (!created.Succeeded)
            {
                logger.LogError("No se pudo crear el Admin inicial: {Errors}",
                    string.Join(" ", created.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(admin, Roles.Admin);
            logger.LogInformation("Admin inicial creado: {Email}", email);
        }
        catch (Exception ex)
        {
            // La API sigue arrancando (por ejemplo para ver Swagger), pero el login no va a funcionar
            logger.LogError(ex, "No se pudieron preparar los roles ni el Admin. ¿Corriste Database/03_identity_tables.sql?");
        }
    }
}
