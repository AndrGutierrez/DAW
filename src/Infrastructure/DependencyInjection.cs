using Core.Application.Livestock;
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
            .AddEntityFrameworkStores<AppDbContext>();

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
        services.AddScoped<IAnimalPhotoService, AnimalPhotoService>();
        services.AddScoped<IAnimalQueryService, AnimalQueryService>();
        services.AddScoped<IAnimalWeightReader, AnimalWeightReader>();

        return services;
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }
}
