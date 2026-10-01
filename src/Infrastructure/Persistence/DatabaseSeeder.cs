using Core.Application.Security;
using Core.Domain.Livestock;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly (string Name, string Description, Func<PermissionDefinition, bool> Selector)[] RoleDefinitions =
    [
        ("Administrador", "Acceso total al sistema", _ => true),
        ("Veterinario", "Salud y reproducción del ganado", definition =>
            definition.Module == "animals"
            || (definition.Module == "herds" && definition.Action == "view")
            || (definition.Module == "farms" && definition.Action == "view")),
        ("Capataz", "Manejo diario de lotes y animales", definition =>
            (definition.Module == "animals" && definition.Action != "delete")
            || (definition.Module == "herds" && definition.Action != "delete")
            || (definition.Module == "farms" && definition.Action == "view")),
        ("Operario", "Registro de datos de campo", definition =>
            (definition.Module == "animals" && definition.Action is "add" or "view")
            || (definition.Module == "herds" && definition.Action == "view")
            || (definition.Module == "farms" && definition.Action == "view")),
        ("SoloLectura", "Consulta sin modificar datos", definition => definition.Action == "view")
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        await SeedPermissionsAsync(db, cancellationToken);
        await SeedRolesAsync(roleManager, db, cancellationToken);
        await SeedAdminAsync(userManager, configuration, cancellationToken);
        await SeedLivestockAsync(db, cancellationToken);
    }

    private static async Task SeedPermissionsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.Permissions
            .Select(permission => permission.Name)
            .ToHashSetAsync(cancellationToken);

        var added = false;

        foreach (var definition in PermissionCatalog.All())
        {
            if (existing.Contains(definition.Name))
            {
                continue;
            }

            db.Permissions.Add(new Permission
            {
                Name = definition.Name,
                GuardName = definition.GuardName
            });

            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedRolesAsync(
        RoleManager<Role> roleManager,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        foreach (var (name, description, selector) in RoleDefinitions)
        {
            var role = await roleManager.FindByNameAsync(name);

            if (role is null)
            {
                role = new Role
                {
                    Name = name,
                    GuardName = PermissionCatalog.GuardName,
                    Description = description
                };

                await roleManager.CreateAsync(role);
            }
            else if (role.Description != description || role.GuardName != PermissionCatalog.GuardName)
            {
                role.Description = description;
                role.GuardName = PermissionCatalog.GuardName;
                await roleManager.UpdateAsync(role);
            }

            var desired = PermissionCatalog.All()
                .Where(selector)
                .Select(definition => definition.Name)
                .ToHashSet();

            var current = await db.RolePermissions
                .Where(rolePermission => rolePermission.RoleId == role.Id)
                .Select(rolePermission => rolePermission.Permission.Name)
                .ToListAsync(cancellationToken);

            var missing = desired.Except(current).ToList();

            if (missing.Count == 0)
            {
                continue;
            }

            var permissions = await db.Permissions
                .Where(permission => missing.Contains(permission.Name))
                .ToListAsync(cancellationToken);

            foreach (var permission in permissions)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var username = configuration["Seed:AdminUsername"] ?? "admin";
        var admin = await userManager.FindByNameAsync(username);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = username,
                Email = configuration["Seed:AdminEmail"] ?? "admin@daw.local",
                FullName = "Administrador",
                EmailConfirmed = true,
                IsSuperuser = true,
                IsStaff = true,
                IsActive = true
            };

            var password = configuration["Seed:AdminPassword"] ?? "REMOVED-SECRET";
            var result = await userManager.CreateAsync(admin, password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not seed the admin user: " + string.Join(" ", result.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Administrador"))
        {
            await userManager.AddToRoleAsync(admin, "Administrador");
        }
    }

    private static async Task SeedLivestockAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var speciesDefinitions = new (string Code, string Name, ProductivePurpose Purpose, int? GestationDays, string[] Breeds)[]
        {
            ("BO", "Bovino", ProductivePurpose.DualPurpose, 283, ["Brahman", "Holstein", "Angus"]),
            ("OV", "Ovino", ProductivePurpose.Wool, 147, ["Dorper", "Merino"]),
            ("PO", "Porcino", ProductivePurpose.Meat, 114, ["Duroc", "Yorkshire"]),
            ("CA", "Caprino", ProductivePurpose.DualPurpose, 150, ["Boer", "Saanen"]),
            ("EQ", "Equino", ProductivePurpose.Work, 340, ["Cuarto de Milla"]),
            ("AV", "Avícola", ProductivePurpose.Eggs, 21, ["Leghorn", "Rhode Island"]),
            ("CU", "Cunícola", ProductivePurpose.Meat, 31, ["Nueva Zelanda"]),
            ("AP", "Apícola", ProductivePurpose.Work, null, []),
            ("AC", "Acuícola", ProductivePurpose.Meat, null, [])
        };

        var existingCodes = await db.Species
            .Select(species => species.Code)
            .ToHashSetAsync(cancellationToken);

        foreach (var definition in speciesDefinitions)
        {
            if (existingCodes.Contains(definition.Code))
            {
                continue;
            }

            var species = new Species
            {
                Code = definition.Code,
                Name = definition.Name,
                Purpose = definition.Purpose,
                GestationDays = definition.GestationDays
            };

            foreach (var breedName in definition.Breeds)
            {
                species.Breeds.Add(new Breed
                {
                    Name = breedName,
                    Purpose = definition.Purpose,
                    Species = species
                });
            }

            db.Species.Add(species);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
