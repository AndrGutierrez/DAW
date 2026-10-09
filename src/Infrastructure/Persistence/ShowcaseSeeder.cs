using System.Data;
using System.Text.Json;
using Core.Domain.Common;
using Core.Domain.Livestock;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public sealed record ShowcaseAccount(string Username, string Role, string FullName, int[] FarmIndexes);
public sealed record ShowcaseSeedResult(bool Replayed, DateOnly AnchorDate, int Farms, int Animals, int Users,
    int Weights, int Production, int ClinicalEvents, int ReproductiveEvents, int Movements, int Inventory);

// Explicit, additive demo import. Historical dates are anchored once, never advanced on replay.
public static class ShowcaseSeeder
{
    public const string Version = "livestock-showcase-v1";
    public static readonly ShowcaseAccount[] Accounts =
    [
        new("demo.admin", "Admin", "Andrea Rojas", [0, 1, 2]),
        new("demo.administrador", "Administrador", "Gabriel Méndez", [0, 1, 2]),
        new("demo.capataz", "Capataz", "Luis Camacho", [0, 1]),
        new("demo.operador", "Employee", "Mariana Torres", [0]),
        new("demo.operario", "Operario", "José Moreno", [1]),
        new("demo.lectura", "SoloLectura", "Elena Salas", [0, 1, 2]),
        new("demo.veterinario", "Veterinario", "Valeria Peña", [0, 1, 2])
    ];
    private static readonly string[] FarmCodes = ["MEGA-VALLE", "MEGA-LLANO", "MEGA-SIERRA"];
    private static readonly string[] FarmNames = ["Finca Valle Verde", "Hacienda Los Llanos", "Finca La Sierra"];
    private static readonly string[] CategoryNames = ["MEGA · Alimentación", "MEGA · Sanidad", "MEGA · Materiales", "MEGA · Reproducción"];

    public static async Task<ShowcaseSeedResult> SeedAsync(IServiceProvider services, DateOnly? anchorDate = null, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var manager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var password = services.GetRequiredService<IConfiguration>()["Seed:DemoPassword"];
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        if (db.Database.IsRelational())
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(741004)", ct);
        var marker = await db.AuditLogs.AsNoTracking().SingleOrDefaultAsync(x => x.EntityName == "ShowcaseSeed" && x.EntityId == Version, ct);
        if (marker != null)
        {
            var saved = JsonSerializer.Deserialize<ShowcaseSeedResult>(marker.NewValues!)
                ?? throw new InvalidOperationException("The demo import manifest is invalid.");
            return saved with { Replayed = true };
        }
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Seed:DemoPassword is required for --seed-demo. Set SEED_DEMO_PASSWORD in the private .env file.");
        if (!await db.Roles.AnyAsync(x => x.Name == "Admin", ct) || !await db.Species.AnyAsync(x => x.Code == "BO" && x.IsActive, ct))
            throw new InvalidOperationException("Initialize the database with --seed before importing the expanded demo.");
        // Never adopt, reset, overwrite or restore an existing business record or account.
        var usernames = Accounts.Select(x => x.Username.ToUpperInvariant()).ToArray();
        if (await db.Users.AnyAsync(x => usernames.Contains(x.NormalizedUserName!), ct) ||
            await db.Farms.IgnoreQueryFilters().AnyAsync(x => FarmCodes.Contains(x.Code), ct) ||
            await db.Products.IgnoreQueryFilters().AnyAsync(x => x.SKU.StartsWith("MEGA-"), ct) ||
            await db.InventoryCategories.IgnoreQueryFilters().AnyAsync(x => CategoryNames.Contains(x.Name), ct))
            throw new InvalidOperationException("The expanded demo identifiers are already in use without a completed manifest. No records were changed.");
        foreach (var account in Accounts)
        {
            if (!await db.Roles.AnyAsync(x => x.Name == account.Role, ct))
                throw new InvalidOperationException("Missing demo role: " + account.Role);
            var probe = new ApplicationUser { UserName = account.Username, Email = account.Username + "@example.test" };
            foreach (var validator in manager.PasswordValidators)
            {
                var validation = await validator.ValidateAsync(manager, probe, password);
                Check(validation);
            }
        }
        var today = anchorDate ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/Caracas"));
        if (today > DateOnly.FromDateTime(DateTime.UtcNow)) throw new ArgumentException("The demo anchor cannot be in the future.", nameof(anchorDate));
        var species = await db.Species.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, ct);
        if (!species.ContainsKey("OV")) throw new InvalidOperationException("The active ovine catalog is required by this demo.");
        var breeds = await db.Breeds.Where(x => x.IsActive).ToListAsync(ct);
        foreach (var name in new[] { "Holstein", "Brahman", "Angus", "Merino", "Dorper" })
            if (!breeds.Any(b => b.Name == name && b.SpeciesId == species[name is "Merino" or "Dorper" ? "OV" : "BO"].Id))
                throw new InvalidOperationException("Missing active demo breed: " + name);
        var farms = FarmCodes.Select((code, index) => new Farm
        {
            Code = code, Name = FarmNames[index], Address = new[] { "Sector La Fría, Táchira", "Sector San Fernando, Apure", "Sector La Grita, Táchira" }[index],
            Email = "finca." + (index + 1) + "@example.test", IsActive = true
        }).ToArray();
        db.Farms.AddRange(farms);
        await db.SaveChangesAsync(ct);
        var users = new Dictionary<string, ApplicationUser>();
        foreach (var account in Accounts)
        {
            var user = new ApplicationUser { UserName = account.Username, Email = account.Username + "@example.test", FullName = account.FullName,
                EmailConfirmed = true, IsActive = true, IsStaff = account.Role is "Admin" or "Administrador" };
            Check(await manager.CreateAsync(user, password));
            Check(await manager.AddToRoleAsync(user, account.Role));
            users.Add(account.Role, user);
            foreach (var index in account.FarmIndexes)
                db.UserFarms.Add(new UserFarm { UserId = user.Id, FarmId = farms[index].Id, IsDefault = index == account.FarmIndexes[0] });
            db.AuditLogs.Add(new AuditLog { Action = "Seeded", EntityName = "ApplicationUser", EntityId = user.Id.ToString(),
                NewValues = JsonSerializer.Serialize(new { SeedVersion = Version, account.Username, account.FullName, account.Role, FarmCodes = account.FarmIndexes.Select(i => farms[i].Code) }) });
        }
        var categories = CategoryNames.Select(name => new InventoryCategory { Name = name }).ToArray();
        db.InventoryCategories.AddRange(categories);
        var products = BuildProducts(categories);
        db.Products.AddRange(products);
        var supplier = new Supplier { Name = "Agroinsumos del Valle · demostración", ContactName = "Departamento de suministros", Email = "suministros@example.test" };
        db.Suppliers.Add(supplier);
        var disease = new Disease { Name = "Mastitis · escenario de demostración", SpeciesId = species["BO"].Id };
        db.Diseases.Add(disease);
        for (var index = 0; index < farms.Length; index++)
            BuildFarm(db, farms[index], index, today, species, breeds, products, supplier, disease, users);
        // Record the real import, not fabricated historical user actions. Identity hashes are excluded.
        var imported = db.ChangeTracker.Entries<BaseEntity>().Where(e => e.State == EntityState.Added && e.Entity is not AuditLog).ToArray();
        foreach (var entry in imported)
        {
            var values = entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
            values["SeedVersion"] = Version;
            var farmId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "FarmId")?.CurrentValue as Guid?;
            db.AuditLogs.Add(new AuditLog { Action = "Seeded", EntityName = entry.Entity.GetType().Name, EntityId = entry.Entity.Id.ToString(),
                FarmId = farmId, NewValues = JsonSerializer.Serialize(values) });
        }
        foreach (var farm in farms)
            db.AuditLogs.Add(new AuditLog { Action = "Seeded", EntityName = nameof(Farm), EntityId = farm.Id.ToString(), FarmId = farm.Id,
                NewValues = JsonSerializer.Serialize(new { SeedVersion = Version, farm.Code, farm.Name, AnchorDate = today }) });
        var pending = imported.Select(e => e.Entity).ToArray();
        var result = new ShowcaseSeedResult(false, today, farms.Length, pending.OfType<Animal>().Count(), users.Count,
            pending.OfType<WeightRecord>().Count(), pending.OfType<AnimalProduction>().Count(), pending.OfType<HealthEvent>().Count(),
            pending.OfType<ReproductiveEvent>().Count(), pending.OfType<AnimalMovement>().Count(), pending.OfType<FarmInventory>().Count());
        db.AuditLogs.Add(new AuditLog { Action = "Seeded", EntityName = "ShowcaseSeed", EntityId = Version, NewValues = JsonSerializer.Serialize(result) });
        await db.SaveChangesAsync(ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        return result;
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Could not create the demo account: " + string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    private static Product[] BuildProducts(InventoryCategory[] categories)
    {
        var definitions = new (string Name, int Category, MeasurementUnit Unit, decimal Cost, decimal Price, int Withdrawal, bool Prescription)[]
        {
            ("Concentrado lechero", 0, MeasurementUnit.Bag, 18, 24, 0, false),
            ("Sal mineralizada", 0, MeasurementUnit.Bag, 12, 16, 0, false),
            ("Heno empacado", 0, MeasurementUnit.Kilogram, 0.30m, 0.45m, 0, false),
            ("Ensilaje de maíz", 0, MeasurementUnit.Kilogram, 0.18m, 0.25m, 0, false),
            ("Vacuna de demostración", 1, MeasurementUnit.Dose, 5, 7.5m, 0, false),
            ("Antibiótico de demostración", 1, MeasurementUnit.Dose, 9, 12, 7, true),
            ("Desparasitante de demostración", 1, MeasurementUnit.Dose, 3, 5, 0, false),
            ("Suero de demostración", 1, MeasurementUnit.Liter, 4, 6, 0, false),
            ("Guantes de examen", 2, MeasurementUnit.Unit, 0.12m, 0.20m, 0, false),
            ("Desinfectante de instalaciones", 2, MeasurementUnit.Liter, 3.5m, 5, 0, false),
            ("Jeringas desechables", 2, MeasurementUnit.Unit, 0.25m, 0.40m, 0, false),
            ("Pajuelas de demostración", 3, MeasurementUnit.Dose, 15, 22, 0, false)
        };
        return definitions.Select((p, i) => new Product { SKU = "MEGA-" + (i + 1).ToString("000"), Name = p.Name, InventoryCategory = categories[p.Category],
            Unit = p.Unit, CostPrice = p.Cost, Price = p.Price, Brand = "Campo", WithdrawalDays = p.Withdrawal, RequiresPrescription = p.Prescription }).ToArray();
    }

    private static void BuildFarm(AppDbContext db, Farm farm, int farmIndex, DateOnly today, Dictionary<string, Species> species,
        List<Breed> breeds, Product[] products, Supplier supplier, Disease disease, Dictionary<string, ApplicationUser> users)
    {
        var veterinarian = users["Veterinario"].Id;
        // Every history author has access to the corresponding farm.
        var operatorId = users[farmIndex == 0 ? "Employee" : farmIndex == 1 ? "Operario" : "Veterinario"].Id;
        var moverId = users[farmIndex < 2 ? "Capataz" : "Admin"].Id;
        var paddockNames = new[] { "Ordeño", "Cría", "Engorde", "Terneros", "Ovinos", "Reserva sanitaria" };
        var capacities = new[] { 12, 24, 20, 16, 6, 20 };
        var paddocks = paddockNames.Select((name, i) => new Paddock { Farm = farm, Name = "Potrero " + name,
            Code = farm.Code + "-P" + (i + 1), Capacity = capacities[i], AreaHectares = 3 + i * 1.5m,
            MaxStayDays = 15 + i * 3, MapX = 4 + i % 3 * 32, MapY = 5 + i / 3 * 46, MapWidth = 28, MapHeight = 38 }).ToArray();
        db.Paddocks.AddRange(paddocks);
        var lots = new[]
        {
            new Lot { Farm = farm, Species = species["BO"], Paddock = paddocks[0], Name = "Vacas en ordeño", Purpose = ProductivePurpose.Milk },
            new Lot { Farm = farm, Species = species["BO"], Paddock = paddocks[1], Name = "Reproductoras", Purpose = ProductivePurpose.DualPurpose },
            new Lot { Farm = farm, Species = species["BO"], Paddock = paddocks[2], Name = "Engorde y reproductores", Purpose = ProductivePurpose.Meat },
            new Lot { Farm = farm, Species = species["BO"], Paddock = paddocks[3], Name = "Terneros y destete", Purpose = ProductivePurpose.DualPurpose },
            new Lot { Farm = farm, Species = species["OV"], Paddock = paddocks[4], Name = "Ovinos", Purpose = ProductivePurpose.Wool }
        };
        db.Lots.AddRange(lots);
        var animals = new List<Animal>();
        var calfAges = new[] { 280, 220, 160, 100, 60, 40, 20, 10 };
        var calfDams = new[] { 0, 1, 2, 3, 22, 23, 24, 25 };
        for (var i = 0; i < 60; i++)
        {
            var isSheep = i >= 56;
            var isCalf = i is >= 48 and < 56;
            var female = i < 30 || (i >= 48 && i % 2 == 0);
            var breedName = isSheep ? (i % 2 == 0 ? "Merino" : "Dorper") : i < 12 ? "Holstein" : i < 24 ? "Brahman" : "Angus";
            var sp = species[isSheep ? "OV" : "BO"];
            var lotIndex = isSheep ? 4 : isCalf ? 3 : i < 12 ? 0 : i < 30 ? 1 : 2;
            var paddockIndex = i == 30 ? 5 : lotIndex;
            var birth = isCalf ? today.AddDays(-calfAges[i - 48]) : today.AddMonths(-(isSheep ? 24 + i % 4 * 6 : i < 36 ? 36 + i % 12 * 3 : 8 + i - 36));
            var animal = new Animal { Farm = farm, Species = sp, Breed = breeds.Single(b => b.Name == breedName && b.SpeciesId == sp.Id),
                Lot = lots[lotIndex], Paddock = paddocks[paddockIndex], InternalTag = "MEGA-" + (farmIndex + 1) + "-" + (i + 1).ToString("000"),
                OfficialId = "OF-MEGA-" + (farmIndex + 1) + "-" + (i + 1).ToString("000"),
                Name = (isSheep ? "Ovino" : isCalf ? "Ternero" : female ? "Vaca" : i < 36 ? "Toro" : "Novillo") + " " + new[] { "Aurora", "Lucero", "Canela", "Brisa", "Cacao", "Jazmín", "Nube", "Roble", "Alba", "Trigal", "Sombra", "Sol" }[i % 12] + " " + (i + 1),
                Sex = female ? Sex.Female : Sex.Male, BirthDate = birth, BirthWeightKg = isSheep ? 4 : 32 + i % 7,
                TargetDailyGainKg = isSheep ? 0.10m : isCalf ? 0.55m : i >= 36 ? 0.75m : 0.30m,
                Color = new[] { "Negro", "Blanco y negro", "Castaño", "Gris" }[i % 4],
                Purpose = lots[lotIndex].Purpose, Origin = i % 7 == 0 ? AnimalOrigin.Purchased : AnimalOrigin.Born,
                Status = i switch { 33 => AnimalStatus.Dead, 34 => AnimalStatus.Sold, 35 => AnimalStatus.Transferred, 47 => AnimalStatus.Lost, _ => AnimalStatus.Active },
                HealthStatus = i switch { 0 or 32 => HealthStatus.InTreatment, 12 => HealthStatus.UnderObservation, 30 => HealthStatus.Quarantine, 31 => HealthStatus.Critical, _ => HealthStatus.Healthy } };
            animals.Add(animal); db.Animals.Add(animal);
            var endDate = animal.Status == AnimalStatus.Active ? today : today.AddDays(-10);
            var currentWeight = isSheep ? 55 + i % 4 * 8 : isCalf ? 42 + calfAges[i - 48] * 0.58m : i >= 36 ? 180 + (i - 36) * 13 : female ? 420 + i % 10 * 15 : 610 + i % 6 * 18;
            var gain = isSheep ? 0.08m : isCalf ? 0.55m : i >= 36 ? 0.50m + i % 4 * 0.10m : 0.12m + i % 4 * 0.06m;
            for (var day = -210; day <= 0; day += 30)
            {
                var date = endDate.AddDays(day);
                if (date < birth) continue;
                db.WeightRecords.Add(new WeightRecord { FarmId = farm.Id, Animal = animal, Date = date, WeightKg = decimal.Round(currentWeight + day * gain, 1),
                    BodyConditionScore = 2.5m + i % 4 * 0.5m, RecordedByUserId = operatorId });
            }
            var initialDate = today.AddDays(-90) < birth ? birth : today.AddDays(-90);
            var movementDate = endDate.AddDays(-(i % 9 + 1));
            if (movementDate < initialDate) movementDate = initialDate;
            var source = paddocks[(paddockIndex + 1) % paddocks.Length];
            db.AnimalMovements.Add(new AnimalMovement { FarmId = farm.Id, Animal = animal, ToPaddock = source, ToLot = lots[lotIndex],
                Date = initialDate, Reason = "Ingreso al lote", UserId = moverId });
            db.AnimalMovements.Add(new AnimalMovement { FarmId = farm.Id, Animal = animal, FromPaddock = source, ToPaddock = paddocks[paddockIndex], FromLot = lots[lotIndex], ToLot = lots[lotIndex],
                Date = movementDate, Reason = i == 30 ? "Aislamiento sanitario" : "Rotación de pastoreo", UserId = moverId });
        }
        for (var i = 48; i < 56; i++)
        {
            animals[i].Dam = animals[calfDams[i - 48]]; animals[i].Sire = animals[30 + i % 3]; animals[i].Origin = AnimalOrigin.Born;
            AddCalving(db, farm, animals[calfDams[i - 48]], animals[i].BirthDate!.Value, veterinarian, i % 3 == 0 ? CalvingDifficulty.Assisted : CalvingDifficulty.Easy);
            if (calfAges[i - 48] >= 160)
                db.ReproductiveEvents.Add(new Weaning { Farm = farm, Dam = animals[calfDams[i - 48]], Offspring = animals[i], Date = animals[i].BirthDate!.Value.AddDays(150), WeightKg = 125, UserId = veterinarian });
        }
        for (var i = 4; i < 12; i++) AddCalving(db, farm, animals[i], today.AddDays(-150 - i * 10), veterinarian, CalvingDifficulty.Easy);
        var semen = new SemenBatch { Farm = farm, Breed = breeds.Single(b => b.Name == "Angus" && b.SpeciesId == species["BO"].Id),
            Supplier = supplier, SireName = "Reproductor Sierra", BatchNumber = farm.Code + "-SEM-01", StrawCount = 40, StorageTank = "Tanque A", ExpirationDate = today.AddYears(1) };
        db.SemenBatches.Add(semen);
        for (var i = 0; i < 22; i++)
        {
            var date = today.AddDays(i < 18 ? -29 + i % 3 : -8 + i % 3);
            db.ReproductiveEvents.Add(new Heat { Farm = farm, Dam = animals[i], Date = date.AddDays(-1), Method = "Observación", UserId = veterinarian });
            ReproductiveEvent service = i % 2 == 0
                ? new Mating { Sire = animals[30 + i % 3] }
                : new Insemination { SemenBatch = semen, TechnicianUserId = veterinarian };
            service.Farm = farm; service.Dam = animals[i]; service.Date = date; service.UserId = veterinarian;
            db.ReproductiveEvents.Add(service);
            if (i < 18)
                db.ReproductiveEvents.Add(new PregnancyCheck { Farm = farm, Dam = animals[i], Date = today.AddDays(-1),
                    Result = i < 10 ? PregnancyResult.Positive : i < 15 ? PregnancyResult.Negative : PregnancyResult.Uncertain,
                    Method = "Ecografía", ExpectedCalvingDate = i < 10 ? date.AddDays(283) : null, UserId = veterinarian });
        }
        // One cow per farm is in withdrawal; no milk is imported during that interval.
        for (var i = 0; i < 12; i++)
            for (var daysAgo = 59; daysAgo >= 0; daysAgo--)
            {
                if (i == 0 && daysAgo <= 2) continue;
                var quantity = 12m + farmIndex * 2 + i % 6 * 1.4m + (decimal)Math.Sin((59 - daysAgo) / 7d + i) * 2.2m;
                db.AnimalProduction.Add(new AnimalProduction { Farm = farm, Animal = animals[i], Date = today.AddDays(-daysAgo),
                    OperationId = Guid.NewGuid(), ProductType = AnimalProductType.Milk, Method = ProductionMethod.Milking, Quantity = decimal.Round(quantity, 2), Unit = MeasurementUnit.Liter });
            }
        for (var i = 56; i < 60; i++)
            db.AnimalProduction.Add(new AnimalProduction { Farm = farm, Animal = animals[i], Date = today.AddDays(-14), OperationId = Guid.NewGuid(),
                ProductType = AnimalProductType.Wool, Method = ProductionMethod.Shearing, Quantity = 2.2m + i % 3 * 0.4m, Unit = MeasurementUnit.Kilogram });
        BuildHealthAndInventory(db, farm, farmIndex, today, animals, products, supplier, disease, veterinarian, operatorId);
    }

    private static void AddCalving(AppDbContext db, Farm farm, Animal dam, DateOnly date, Guid userId, CalvingDifficulty difficulty)
    {
        db.ReproductiveEvents.Add(new Mating { Farm = farm, Dam = dam, Date = date.AddDays(-283), UserId = userId });
        db.ReproductiveEvents.Add(new PregnancyCheck { Farm = farm, Dam = dam, Date = date.AddDays(-230), Result = PregnancyResult.Positive,
            ExpectedCalvingDate = date, Method = "Ecografía", UserId = userId });
        db.ReproductiveEvents.Add(new Calving { Farm = farm, Dam = dam, Date = date, OffspringCount = 1, StillbornCount = 0, Difficulty = difficulty, UserId = userId });
    }

    private static void BuildHealthAndInventory(AppDbContext db, Farm farm, int farmIndex, DateOnly today, List<Animal> animals,
        Product[] products, Supplier supplier, Disease disease, Guid veterinarian, Guid operatorId)
    {
        var consumption = new List<(Animal Animal, int Product, DateOnly Date, string Reason)>();
        void Consume(Animal animal, int product, DateOnly date, string reason)
        {
            consumption.Add((animal, product, date, reason));
        }
        for (var i = 0; i < 60; i++)
        {
            var animal = animals[i];
            var vaccineDate = today.AddDays(-45);
            if (animal.BirthDate <= vaccineDate)
            {
                db.HealthEvents.Add(new Vaccination { Farm = farm, Animal = animal, Date = vaccineDate, Product = products[4], Dose = 1, NextDueDate = today.AddDays(7 + i % 20), UserId = veterinarian, Cost = products[4].CostPrice });
                Consume(animal, 4, vaccineDate, "Vacunación registrada");
            }
            var wormDate = today.AddDays(-30);
            if (animal.BirthDate <= wormDate)
            {
                db.HealthEvents.Add(new Deworming { Farm = farm, Animal = animal, Date = wormDate, Product = products[6], Dose = 1, UserId = veterinarian, Cost = products[6].CostPrice });
                Consume(animal, 6, wormDate, "Desparasitación registrada");
            }
            if (animal.HealthStatus == HealthStatus.Healthy) continue;
            var date = today.AddDays(-2);
            db.HealthStatusChanges.Add(new HealthStatusChange { FarmId = farm.Id, Animal = animal, PreviousStatus = HealthStatus.Healthy, NewStatus = animal.HealthStatus,
                ChangedAt = date.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc), UserId = veterinarian, Reason = "Evaluación sanitaria" });
            if (i is 0 or 32)
            {
                db.HealthEvents.Add(new DiseaseCase { Farm = farm, Animal = animal, Date = date, Disease = i == 0 ? disease : null, Severity = "Moderada", IsContagious = false, UserId = veterinarian });
                db.HealthEvents.Add(new Treatment { Farm = farm, Animal = animal, Date = date, StartDate = date, EndDate = today,
                    Product = products[5], Dose = 1, Route = MedicationRoute.Intramuscular, WithdrawalDays = 7, WithdrawalEndDate = today.AddDays(7), UserId = veterinarian, Cost = products[5].CostPrice });
                Consume(animal, 5, date, "Tratamiento registrado");
            }
            else if (i == 30)
                db.HealthEvents.Add(new Quarantine { Farm = farm, Animal = animal, Date = date, StartDate = date, EndDate = today.AddDays(12), Reason = "Control de ingreso", UserId = veterinarian });
            else
                db.HealthEvents.Add(new DiseaseCase { Farm = farm, Animal = animal, Date = date, Severity = i == 31 ? "Grave" : "Leve", IsContagious = false, UserId = veterinarian });
        }
        db.HealthEvents.Add(new MortalityEvent { Farm = farm, Animal = animals[33], Date = today.AddDays(-7), Cause = "Accidente", UserId = veterinarian });
        for (var i = 0; i < products.Length; i++)
        {
            var target = i == farmIndex || i == 5 ? 5m : i == 2 ? 210m : 45m + i * 4;
            var opening = target + consumption.Count(m => m.Product == i);
            var inventory = new FarmInventory { Farm = farm, Product = products[i], Stock = target, MinStock = 10, MaxStock = 200, Location = i < 4 ? "Almacén de alimentación" : i < 8 ? "Depósito sanitario" : "Almacén general" };
            db.FarmInventory.Add(inventory);
            var batch = new ProductBatch { Farm = farm, Product = products[i], Supplier = supplier, BatchNumber = farm.Code + "-INS-" + (i + 1),
                InitialQuantity = opening, UnitCost = products[i].CostPrice, ExpirationDate = today.AddDays(i == 7 ? 10 : 180 + i * 10) };
            db.ProductBatches.Add(batch);
            db.StockMovements.Add(new StockMovement { FarmId = farm.Id, Product = products[i], ProductBatch = batch, Type = StockMovementType.Adjustment, Quantity = opening,
                Date = today.AddDays(-65), ReferenceType = "OpeningBalance", ReferenceId = inventory.Id, Reason = "Saldo inicial de demostración", UserId = operatorId });
            var flows = new[] { (-50, StockMovementType.In, 25m), (-35, StockMovementType.Out, 7m), (-18, StockMovementType.Out, 11m), (-10, StockMovementType.In, 8m), (-2, StockMovementType.Out, 15m) };
            foreach (var (offset, type, quantity) in flows)
                db.StockMovements.Add(new StockMovement { FarmId = farm.Id, Product = products[i], Type = type, Quantity = quantity, Date = today.AddDays(offset),
                    ReferenceType = "Inventory", ReferenceId = inventory.Id, Reason = type == StockMovementType.In ? "Reposición de insumos" : "Consumo de operación", UserId = operatorId });
        }
        db.StockMovements.AddRange(consumption.Select(m => new StockMovement { FarmId = farm.Id, Product = products[m.Product],
            Type = StockMovementType.Out, Quantity = 1, Date = m.Date, Reason = m.Reason, ReferenceType = "Animal", ReferenceId = m.Animal.Id, UserId = veterinarian }));
    }
}
