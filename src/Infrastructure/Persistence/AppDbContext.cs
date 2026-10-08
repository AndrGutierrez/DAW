using Core.Domain.Common;
using Core.Domain.Livestock;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, Role, Guid>(options)
{
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<UserFarm> UserFarms => Set<UserFarm>();
    public DbSet<Paddock> Paddocks => Set<Paddock>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<Species> Species => Set<Species>();
    public DbSet<Breed> Breeds => Set<Breed>();
    public DbSet<Disease> Diseases => Set<Disease>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<AnimalPhoto> AnimalPhotos => Set<AnimalPhoto>();
    public DbSet<WeightRecord> WeightRecords => Set<WeightRecord>();
    public DbSet<HealthEvent> HealthEvents => Set<HealthEvent>();
    public DbSet<HealthStatusChange> HealthStatusChanges => Set<HealthStatusChange>();
    public DbSet<ReproductiveEvent> ReproductiveEvents => Set<ReproductiveEvent>();
    public DbSet<SemenBatch> SemenBatches => Set<SemenBatch>();
    public DbSet<AnimalProduction> AnimalProduction => Set<AnimalProduction>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<FarmInventory> FarmInventory => Set<FarmInventory>();
    public DbSet<Ration> Rations => Set<Ration>();
    public DbSet<RationIngredient> RationIngredients => Set<RationIngredient>();
    public DbSet<FeedingRecord> FeedingRecords => Set<FeedingRecord>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AnimalMovement> AnimalMovements => Set<AnimalMovement>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(14, 4);
        base.ConfigureConventions(configurationBuilder);
    }

    public override int SaveChanges()
    {
        TouchUpdatedAnimals();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchUpdatedAnimals();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TouchUpdatedAnimals()
    {
        foreach (var entry in ChangeTracker.Entries<Animal>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(candidate => candidate.BaseType is null
                && typeof(BaseEntity).IsAssignableFrom(candidate.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.CreatedAt))
                .IsRequired()
                .HasDefaultValueSql("now()");
        }
    }
}
