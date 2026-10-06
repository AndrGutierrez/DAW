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
    private static readonly (string Name, string Description, Func<PermissionDefinition, bool> Selector)[] RoleDefinitions = [("Administrador", "Acceso total al sistema", _ => true), ("Admin", "Administración y mantenimiento de catálogos", _ => true), ("Employee", "Operación diaria sin eliminar registros", definition => definition.Action is "list" or "get" || (definition.Action is "create" or "update" && definition.Resource is "animals" or "lots" or "paddocks" or "inventory" or "production" or "weights" or "photos" or "clinical" or "reproduction")), ("Veterinario", "Salud y reproducción del ganado", definition => definition.Action is "list" or "get" || definition.Resource is "animals" or "weights" or "clinical" or "reproduction" && definition.Action is "create" or "update"), ("Capataz", "Manejo diario de lotes y animales", definition => definition.Action is "list" or "get" || definition.Resource is "animals" or "lots" or "paddocks" or "inventory" or "production" or "weights" or "photos" or "clinical" or "reproduction" && definition.Action is "create" or "update"), ("Operario", "Registro de datos de campo", definition => definition.Action is "list" or "get" || definition.Resource is "animals" or "production" or "weights" or "photos" or "clinical" or "reproduction" && definition.Action == "create"), ("SoloLectura", "Consulta sin modificar datos", definition => definition.Action is "list" or "get")];
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
        await SeedOperationalDataAsync(db, cancellationToken);
        await SeedInventoryAndProductionAsync(db, cancellationToken);
        await SeedEmployeeAsync(userManager, configuration, db, cancellationToken);
    }

    private static async Task SeedPermissionsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var catalog = PermissionCatalog.All().ToList();
        var catalogNames = catalog.Select(definition => definition.Name).ToHashSet();
        var existing = await db.Permissions.ToListAsync(cancellationToken);
        var obsolete = existing.Where(permission => !catalogNames.Contains(permission.Name)).ToList();
        if (obsolete.Count > 0)
        {
            db.Permissions.RemoveRange(obsolete);
        }

        var existingNames = existing.Select(permission => permission.Name).ToHashSet();
        foreach (var definition in catalog)
        {
            if (!existingNames.Contains(definition.Name))
            {
                db.Permissions.Add(new Permission { Name = definition.Name, GuardName = definition.GuardName });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager, AppDbContext db, CancellationToken cancellationToken)
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

            var desired = PermissionCatalog.All().Where(selector).Select(definition => definition.Name).ToHashSet();
            var current = await db.RolePermissions.Where(rolePermission => rolePermission.RoleId == role.Id).Select(rolePermission => rolePermission.Permission.Name).ToListAsync(cancellationToken);
            var missing = desired.Except(current).ToList();
            var extra = current.Except(desired).ToList();
            if (missing.Count > 0)
            {
                var permissions = await db.Permissions.Where(permission => missing.Contains(permission.Name)).ToListAsync(cancellationToken);
                foreach (var permission in permissions)
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
                }
            }

            if (extra.Count > 0)
            {
                var stale = await db.RolePermissions.Where(rolePermission => rolePermission.RoleId == role.Id && extra.Contains(rolePermission.Permission.Name)).ToListAsync(cancellationToken);
                db.RolePermissions.RemoveRange(stale);
            }

            if (missing.Count > 0 || extra.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var username = configuration["Seed:AdminUsername"] ?? "admin";
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Seed:AdminPassword is not configured. Set the Seed__AdminPassword environment variable (see .env.example).");
        }

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
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Could not seed the admin user: " + string.Join(" ", result.Errors.Select(error => error.Description)));
            }
        }
        else if (!await userManager.CheckPasswordAsync(admin, password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(admin);
            var result = await userManager.ResetPasswordAsync(admin, token, password);
            if (!result.Succeeded)
                throw new InvalidOperationException("Could not reset the admin password: " + string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        if (!await userManager.IsInRoleAsync(admin, "Administrador"))
        {
            await userManager.AddToRoleAsync(admin, "Administrador");
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");
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
        var existingCodes = await db.Species.Select(species => species.Code).ToHashSetAsync(cancellationToken);
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
                species.Breeds.Add(new Breed { Name = breedName, Purpose = definition.Purpose, Species = species });
            }

            db.Species.Add(species);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedOperationalDataAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Farms.AnyAsync(farm => farm.Code == "DEMO", cancellationToken))
        {
            return;
        }

        var speciesByCode = await db.Species.ToDictionaryAsync(species => species.Code, cancellationToken);
        var breedsByName = await db.Breeds.ToDictionaryAsync(breed => (breed.SpeciesId, breed.Name), cancellationToken);
        var farm = new Farm
        {
            Name = "Finca El Paraíso",
            Code = "DEMO",
            Address = "Ruta 1 km 5",
            Phone = "+00 000 000",
            Email = "demo@daw.local",
            IsActive = true
        };
        var north = new Paddock
        {
            Farm = farm,
            Name = "Potrero Norte",
            AreaHectares = 12.5m,
            Capacity = 40
        };
        var south = new Paddock
        {
            Farm = farm,
            Name = "Potrero Sur",
            AreaHectares = 9.0m,
            Capacity = 30
        };
        var bovine = speciesByCode["BO"];
        var ovine = speciesByCode["OV"];
        var engorde = new Lot
        {
            Farm = farm,
            Species = bovine,
            Name = "Engorde",
            Purpose = ProductivePurpose.Meat,
            Paddock = north
        };
        var cria = new Lot
        {
            Farm = farm,
            Species = bovine,
            Name = "Cría",
            Purpose = ProductivePurpose.DualPurpose,
            Paddock = south
        };
        var ovinos = new Lot
        {
            Farm = farm,
            Species = ovine,
            Name = "Ovinos",
            Purpose = ProductivePurpose.Wool,
            Paddock = north
        };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var bovineSpecs = new (string Tag, string Name, Sex Sex, string Breed, ProductivePurpose Purpose, int AgeMonths, decimal Weight, Lot Lot, Paddock Paddock)[]
        {
            ("DEMO-001", "Toro Bravo", Sex.Male, "Brahman", ProductivePurpose.Meat, 30, 520m, engorde, north),
            ("DEMO-002", "Vaca Luna", Sex.Female, "Holstein", ProductivePurpose.Milk, 48, 560m, engorde, north),
            ("DEMO-003", "Novillo Coco", Sex.Male, "Angus", ProductivePurpose.Meat, 18, 380m, engorde, north),
            ("DEMO-004", "Vaquillona Estrella", Sex.Female, "Brahman", ProductivePurpose.DualPurpose, 20, 340m, cria, south),
            ("DEMO-005", "Ternero Sol", Sex.Male, "Holstein", ProductivePurpose.Meat, 8, 180m, cria, south),
            ("DEMO-006", "Vaca Nube", Sex.Female, "Angus", ProductivePurpose.Milk, 36, 470m, cria, south)
        };
        var ovineSpecs = new (string Tag, string Name, Sex Sex, string Breed, int AgeMonths, decimal Weight)[]
        {
            ("DEMO-101", "Oveja Blanca", Sex.Female, "Merino", 24, 65m),
            ("DEMO-102", "Carnero Negro", Sex.Male, "Dorper", 30, 80m)
        };
        foreach (var spec in bovineSpecs)
        {
            db.Animals.Add(CreateAnimal(farm, bovine, breedsByName[(bovine.Id, spec.Breed)], spec.Lot, spec.Paddock, spec.Tag, spec.Name, spec.Sex, spec.Purpose, today.AddMonths(-spec.AgeMonths), spec.Weight, today));
        }

        foreach (var spec in ovineSpecs)
        {
            db.Animals.Add(CreateAnimal(farm, ovine, breedsByName[(ovine.Id, spec.Breed)], ovinos, north, spec.Tag, spec.Name, spec.Sex, ProductivePurpose.Wool, today.AddMonths(-spec.AgeMonths), spec.Weight, today));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedInventoryAndProductionAsync(AppDbContext db, CancellationToken ct)
    {
        var farm = await db.Farms.SingleAsync(x => x.Code == "DEMO", ct);
        foreach (var name in new[]
        {
            "Alimentación animal",
            "Sanidad animal"
        }

        )
            if (!await db.InventoryCategories.AnyAsync(x => x.Name == name, ct))
                db.InventoryCategories.Add(new InventoryCategory { Name = name, Description = "Insumos de la operación ganadera" });
        await db.SaveChangesAsync(ct);
        var categories = await db.InventoryCategories.ToDictionaryAsync(x => x.Name, ct);
        var samples = new[]
        {
            (SKU: "ALI-001", Name: "Concentrado bovino", Category: "Alimentación animal", Price: 24.50m, Cost: 18.00m, Stock: 40m, Min: 10m, Max: 100m, Unit: MeasurementUnit.Bag, Brand: "AgroCampo"),
            (SKU: "ALI-002", Name: "Sal mineral", Category: "Alimentación animal", Price: 16.00m, Cost: 12.00m, Stock: 25m, Min: 5m, Max: 80m, Unit: MeasurementUnit.Bag, Brand: "AgroCampo"),
            (SKU: "SAN-001", Name: "Vacuna bovina", Category: "Sanidad animal", Price: 8.75m, Cost: 6.50m, Stock: 60m, Min: 15m, Max: 150m, Unit: MeasurementUnit.Dose, Brand: "VetSalud"),
            (SKU: "SAN-002", Name: "Desparasitante", Category: "Sanidad animal", Price: 22.00m, Cost: 15.00m, Stock: 12m, Min: 3m, Max: 30m, Unit: MeasurementUnit.Liter, Brand: "VetSalud")
        };
        foreach (var s in samples)
        {
            var product = await db.Products.SingleOrDefaultAsync(x => x.SKU == s.SKU, ct);
            if (product is null)
            {
                product = new Product
                {
                    SKU = s.SKU,
                    Name = s.Name,
                    CategoryId = categories[s.Category].Id,
                    Price = s.Price,
                    CostPrice = s.Cost,
                    Unit = s.Unit,
                    Brand = s.Brand
                };
                db.Products.Add(product);
            }

            if (!await db.FarmInventory.AnyAsync(x => x.FarmId == farm.Id && x.ProductId == product.Id, ct))
                db.FarmInventory.Add(new FarmInventory { FarmId = farm.Id, Product = product, Stock = s.Stock, MinStock = s.Min, MaxStock = s.Max, Location = "Almacén principal" });
        }

        await db.SaveChangesAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var milkAnimal = await db.Animals.SingleAsync(x => x.FarmId == farm.Id && x.InternalTag == "DEMO-002", ct);
        var sheep = await db.Animals.SingleAsync(x => x.FarmId == farm.Id && x.InternalTag == "DEMO-101", ct);
        var operationIds = new[]
        {
            Guid.Parse("c3b917cc-5569-4fe3-89dc-41dcf3887301"),
            Guid.Parse("c3b917cc-5569-4fe3-89dc-41dcf3887302"),
            Guid.Parse("c3b917cc-5569-4fe3-89dc-41dcf3887303")
        };
        if (!await db.AnimalProduction.AnyAsync(x => x.OperationId == operationIds[0], ct))
            db.AnimalProduction.Add(new AnimalProduction { FarmId = farm.Id, AnimalId = milkAnimal.Id, OperationId = operationIds[0], Date = today, ProductType = AnimalProductType.Milk, Method = ProductionMethod.Milking, Quantity = 18.5m, Unit = MeasurementUnit.Liter, Notes = "Ordeño de demostración" });
        if (!await db.AnimalProduction.AnyAsync(x => x.OperationId == operationIds[1], ct))
            db.AnimalProduction.Add(new AnimalProduction { FarmId = farm.Id, AnimalId = sheep.Id, OperationId = operationIds[1], Date = today.AddDays(-30), ProductType = AnimalProductType.Wool, Method = ProductionMethod.Shearing, Quantity = 3.2m, Unit = MeasurementUnit.Kilogram, Notes = "Esquila previa al sacrificio de demostración" });
        if (!await db.AnimalProduction.AnyAsync(x => x.OperationId == operationIds[2], ct))
        {
            db.AnimalProduction.Add(new AnimalProduction { FarmId = farm.Id, AnimalId = sheep.Id, OperationId = operationIds[2], Date = today, ProductType = AnimalProductType.Meat, Method = ProductionMethod.Slaughter, Quantity = 28m, Unit = MeasurementUnit.Kilogram });
            db.AnimalProduction.Add(new AnimalProduction { FarmId = farm.Id, AnimalId = sheep.Id, OperationId = operationIds[2], Date = today, ProductType = AnimalProductType.Hide, Method = ProductionMethod.Slaughter, Quantity = 1m, Unit = MeasurementUnit.Unit });
            sheep.Status = AnimalStatus.Dead;
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedEmployeeAsync(UserManager<ApplicationUser> manager, IConfiguration configuration, AppDbContext db, CancellationToken ct)
    {
        var password = configuration["Seed:EmployeePassword"];
        if (string.IsNullOrWhiteSpace(password))
            return;
        var username = configuration["Seed:EmployeeUsername"] ?? "employee";
        var employee = await manager.FindByNameAsync(username);
        if (employee is null)
        {
            employee = new ApplicationUser
            {
                UserName = username,
                Email = configuration["Seed:EmployeeEmail"] ?? "employee@daw.local",
                FullName = "Operador de finca",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await manager.CreateAsync(employee, password);
            if (!result.Succeeded)
                throw new InvalidOperationException("Could not seed the employee: " + string.Join(" ", result.Errors.Select(x => x.Description)));
        }

        if (!await manager.IsInRoleAsync(employee, "Employee"))
            await manager.AddToRoleAsync(employee, "Employee");
        var farmId = await db.Farms.Where(x => x.Code == "DEMO").Select(x => x.Id).SingleAsync(ct);
        if (!await db.UserFarms.AnyAsync(x => x.UserId == employee.Id && x.FarmId == farmId, ct))
        {
            db.UserFarms.Add(new UserFarm { UserId = employee.Id, FarmId = farmId, IsDefault = true });
            await db.SaveChangesAsync(ct);
        }
    }

    private static Animal CreateAnimal(Farm farm, Species species, Breed breed, Lot lot, Paddock paddock, string tag, string name, Sex sex, ProductivePurpose purpose, DateOnly birthDate, decimal currentWeight, DateOnly weightDate)
    {
        var animal = new Animal
        {
            Farm = farm,
            Species = species,
            Breed = breed,
            Lot = lot,
            Paddock = paddock,
            InternalTag = tag,
            OfficialId = "OF-" + tag,
            Name = name,
            Sex = sex,
            BirthDate = birthDate,
            BirthWeightKg = Math.Round(currentWeight / 14m, 1),
            Color = sex == Sex.Female ? "Blanco" : "Negro",
            Status = AnimalStatus.Active,
            Origin = AnimalOrigin.Born,
            Purpose = purpose,
            HealthStatus = HealthStatus.Healthy
        };
        animal.WeightRecords.Add(new WeightRecord { FarmId = farm.Id, Animal = animal, Date = weightDate, WeightKg = currentWeight, BodyConditionScore = 3.5m });
        return animal;
    }
}
