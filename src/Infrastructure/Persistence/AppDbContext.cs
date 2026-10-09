using Core.Domain.Common;
using Core.Application.Security;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Core.Domain.Livestock;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, Role, Guid>
{
    private readonly ICurrentUser? actor;
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? actor = null) : base(options)
    {
        this.actor = actor;
        ChangeTracker.CascadeDeleteTiming = CascadeTiming.Never;
        ChangeTracker.DeleteOrphansTiming = CascadeTiming.Never;
    }
    public static readonly Type[] RetainedLinks = [typeof(RolePermission), typeof(UserPermission), typeof(IdentityUserRole<Guid>), typeof(Permission)];
    private void RetainDeletedRecords()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted).ToList())
        {
            if (entry.Entity is AuditLog) throw new InvalidOperationException("Audit history cannot be deleted.");
            if (entry.Entity is BaseEntity record)
            {
                entry.State = EntityState.Modified; record.MarkDeleted(actor?.UserId);
            }
            else if (RetainedLinks.Contains(entry.Metadata.ClrType))
            {
                entry.State = EntityState.Modified;
                entry.Property("IsDeleted").CurrentValue = true;
                entry.Property("DeletedAt").CurrentValue = DateTime.UtcNow;
                entry.Property("DeletedByUserId").CurrentValue = actor?.UserId;
            }
            else throw new InvalidOperationException("Persistent records must be retained instead of physically deleted.");
        }
    }
    public void RestoreLink(object link)
    {
        var entry = Entry(link);
        if (link is BaseEntity record) record.Restore();
        else { entry.Property("IsDeleted").CurrentValue = false; entry.Property("DeletedAt").CurrentValue = null; entry.Property("DeletedByUserId").CurrentValue = null; }
    }
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

    public override int SaveChanges() => SaveChanges(true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RetainDeletedRecords();
        TouchUpdatedAnimals();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(true, cancellationToken);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RetainDeletedRecords();
        TouchUpdatedAnimals();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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

        foreach (var type in RetainedLinks)
        {
            var builder = modelBuilder.Entity(type);
            builder.Property<bool>("IsDeleted").HasDefaultValue(false);
            builder.Property<DateTime?>("DeletedAt"); builder.Property<Guid?>("DeletedByUserId");
            var parameter = Expression.Parameter(type, "record");
            var deleted = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(bool)], parameter, Expression.Constant("IsDeleted"));
            builder.HasQueryFilter(Expression.Lambda(Expression.Not(deleted), parameter));
        }
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(candidate => candidate.BaseType is null
                && typeof(BaseEntity).IsAssignableFrom(candidate.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.CreatedAt))
                .IsRequired()
                .HasDefaultValueSql("now()");
            var builder = modelBuilder.Entity(entityType.ClrType);
            if (entityType.ClrType == typeof(AuditLog))
            {
                builder.Ignore(nameof(BaseEntity.IsDeleted)); builder.Ignore(nameof(BaseEntity.DeletedAt)); builder.Ignore(nameof(BaseEntity.DeletedByUserId));
                continue;
            }
            builder.Property(nameof(BaseEntity.IsDeleted)).HasDefaultValue(false);
            builder.HasIndex(nameof(BaseEntity.IsDeleted), nameof(BaseEntity.DeletedAt));
            var parameter = Expression.Parameter(entityType.ClrType, "record");
            builder.HasQueryFilter(Expression.Lambda(Expression.Not(Expression.Property(parameter, nameof(BaseEntity.IsDeleted))), parameter));
        }
    }
}
