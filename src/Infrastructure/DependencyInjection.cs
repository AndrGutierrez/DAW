using Core.Application.Operations;
using Infrastructure.Operations;
using Core.Application.Livestock;
using Core.Application.Management;
using Core.Domain.Livestock;
using Core.Application.Security;
using Core.Application.Storage;
using Infrastructure.Livestock;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Identity;
using Infrastructure.Security;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccessAdministration, AccessAdministration>();
        services.AddScoped<IUserManagementStore, UserManagementStore>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<IAnimalPhotoService, AnimalPhotoService>();
        services.AddScoped<IAnimalQueryService, AnimalQueryService>();
        services.AddScoped<IAnimalWeightReader, AnimalWeightReader>();
        services.AddScoped<IFarmAccess, FarmAccess>();
        services.AddScoped<IManagementRepository, ManagementRepository>();
        services.AddScoped<AnimalHealthService>();
        services.AddScoped<WithdrawalPolicy>();
        services.AddScoped<AnimalProductionService>();
        services.AddScoped<AnimalCareService>();
        services.AddScoped<AnimalLocationPolicy>();
        services.AddScoped<AnimalMovementService>();
        services.AddScoped<PaddockService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<AnalyticsService>();
        services.AddScoped<ReportService>();
        services.AddScoped<IOperationsReader, OperationsReader>();
        services.AddScoped<IPaddockReader, PaddockReader>();
        services.AddScoped<AnimalReproductionService>();
        services.AddScoped<AnimalGrowthService>();
        services.AddScoped<GrowthMonitoringService>();
        services.AddScoped<IGrowthMonitoringReader, GrowthMonitoringReader>();
        services.AddScoped<AnimalWeighingService>();
        AddResource<Farm, FarmRequest, FarmDefinition>(services);
        AddResource<Species, SpeciesRequest, SpeciesDefinition>(services);
        AddResource<Breed, BreedRequest, BreedDefinition>(services);
        AddResource<Paddock, PaddockRequest, PaddockDefinition>(services);
        AddResource<Lot, LotRequest, LotDefinition>(services);
        AddResource<InventoryCategory, CategoryRequest, CategoryDefinition>(services);
        AddResource<Product, ProductRequest, ProductDefinition>(services);
        AddResource<FarmInventory, InventoryRequest, InventoryDefinition>(services);
        AddResource<Animal, AnimalRequest, AnimalDefinition>(services);
        AddResource<AnimalProduction, ProductionRequest, ProductionDefinition>(services);
        AddResource<WeightRecord, WeightRequest, WeightDefinition>(services);

        return services;
    }

    private static void AddResource<TEntity, TRequest, TDefinition>(IServiceCollection services)
        where TEntity : Core.Domain.Common.BaseEntity, new()
        where TDefinition : class, IResourceDefinition<TEntity, TRequest>
    {
        services.AddScoped<IResourceDefinition<TEntity, TRequest>, TDefinition>();
        services.AddScoped<ICrudService<TRequest>, CrudService<TEntity, TRequest>>();
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }
}
