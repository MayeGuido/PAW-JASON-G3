using Jason.DataAccess.Identity;
using Jason.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jason.DataAccess.Context;

// Incluye las tablas de ASP.NET Identity (usuarios y roles) además de las nuestras.
// Las tablas de Identity se crean con Database/03_identity_tables.sql.
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Source> Sources => Set<Source>();
    public DbSet<SourceItem> SourceItems => Set<SourceItem>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentity(modelBuilder);

        // Mismas columnas y largos que el script del profesor (Database/01_create_tables.sql)
        modelBuilder.Entity<Source>(e =>
        {
            e.ToTable("Sources");
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.ComponentType).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<SourceItem>(e =>
        {
            e.ToTable("SourceItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Json).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnType("datetime");
            e.HasOne(x => x.Source)
                .WithMany(s => s.Items)
                .HasForeignKey(x => x.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Setting>(e =>
        {
            e.ToTable("Settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.ValueEncrypted).IsRequired();
            e.Property(x => x.UpdatedAt).HasColumnType("datetime");
        });
    }

    // Se separa para poder generar el script de las tablas de Identity con el mismo modelo
    public static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.DisplayName).HasMaxLength(100);
        });
    }
}
