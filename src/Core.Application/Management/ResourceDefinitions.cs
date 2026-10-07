using System.Linq.Expressions;
using Core.Domain.Common;
using Core.Domain.Livestock;
using Core.Application.Security;
using Core.Application.Livestock;

namespace Core.Application.Management;

public abstract class ResourceDefinition<TEntity, TRequest>(IManagementRepository repository) : IResourceDefinition<TEntity, TRequest> where TEntity : BaseEntity, new()
{
    protected IManagementRepository Repository { get; } = repository;

    public virtual Guid? FarmId(TEntity entity) => null;
    public virtual Guid? FarmId(TRequest request) => null;
    public virtual Expression<Func<TEntity, bool>>? Scope(IReadOnlyCollection<Guid> ids) => null;
    public abstract TRequest Read(TEntity entity);
    public abstract void Apply(TEntity entity, TRequest request);
    public virtual Task CheckAsync(TEntity entity, TRequest request, CancellationToken ct) => Task.CompletedTask;
    public virtual Task BeforeDeleteAsync(TEntity entity, CancellationToken ct) => Task.CompletedTask;
    protected async Task<T> Require<T>(Guid id, CancellationToken ct)
        where T : BaseEntity => await Repository.GetAsync<T>(id, ct: ct) ?? throw new ArgumentException($"The referenced {typeof(T).Name} does not exist.");
    protected static void Check(bool condition, string message)
    {
        if (!condition)
            throw new ConflictException(message);
    }

    protected async Task Unique<T>(Expression<Func<T, bool>> predicate, CancellationToken ct)
        where T : BaseEntity => Check(!await Repository.ExistsAsync(predicate, ct), $"A {typeof(T).Name} with the same identifying value already exists.");
    protected static string Clean(string text) => text.Trim();
    protected static string? Optional(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

public sealed class FarmDefinition(IManagementRepository r) : ResourceDefinition<Farm, FarmRequest>(r)
{
    public override Guid? FarmId(Farm e) => e.Id;
    public override Expression<Func<Farm, bool>> Scope(IReadOnlyCollection<Guid> ids) => e => ids.Contains(e.Id);
    public override FarmRequest Read(Farm e) => new(e.Name, e.Code, e.Address, e.Phone, e.Email, e.IsActive);
    public override void Apply(Farm e, FarmRequest q)
    {
        e.Name = Clean(q.Name);
        e.Code = Clean(q.Code).ToUpperInvariant();
        e.Address = Optional(q.Address);
        e.Phone = Optional(q.Phone);
        e.Email = Optional(q.Email);
        e.IsActive = q.IsActive;
    }

    public override Task CheckAsync(Farm e, FarmRequest q, CancellationToken ct) => Unique<Farm>(x => x.Id != e.Id && x.Code == q.Code.Trim().ToUpper(), ct);
}

public sealed class SpeciesDefinition(IManagementRepository r) : ResourceDefinition<Species, SpeciesRequest>(r)
{
    public override SpeciesRequest Read(Species e) => new(e.Name, e.Code, e.Purpose, e.GestationDays, e.IsActive);
    public override void Apply(Species e, SpeciesRequest q)
    {
        e.Name = Clean(q.Name);
        e.Code = Clean(q.Code).ToUpperInvariant();
        e.Purpose = q.Purpose;
        e.GestationDays = q.GestationDays;
        e.IsActive = q.IsActive;
    }

    public override Task CheckAsync(Species e, SpeciesRequest q, CancellationToken ct) => Unique<Species>(x => x.Id != e.Id && (x.Code == q.Code.Trim().ToUpper() || x.Name == q.Name.Trim()), ct);
}

public sealed class BreedDefinition(IManagementRepository r) : ResourceDefinition<Breed, BreedRequest>(r)
{
    public override BreedRequest Read(Breed e) => new(e.SpeciesId, e.Name, e.Purpose, e.Origin, e.IsActive);
    public override void Apply(Breed e, BreedRequest q)
    {
        e.SpeciesId = q.SpeciesId;
        e.Name = Clean(q.Name);
        e.Purpose = q.Purpose;
        e.Origin = Optional(q.Origin);
        e.IsActive = q.IsActive;
    }

    public override async Task CheckAsync(Breed e, BreedRequest q, CancellationToken ct)
    {
        await Require<Species>(q.SpeciesId, ct);
        if (e.SpeciesId != Guid.Empty && e.SpeciesId != q.SpeciesId)
            Check(!await Repository.ExistsAsync<Animal>(a => a.BreedId == e.Id, ct), "A breed used by animals cannot change species.");
        await Unique<Breed>(x => x.Id != e.Id && x.SpeciesId == q.SpeciesId && x.Name == q.Name.Trim(), ct);
    }
}

public sealed class PaddockDefinition(IManagementRepository r) : ResourceDefinition<Paddock, PaddockRequest>(r)
{
    public override Guid? FarmId(Paddock e) => e.FarmId;
    public override Guid? FarmId(PaddockRequest q) => q.FarmId;
    public override Expression<Func<Paddock, bool>> Scope(IReadOnlyCollection<Guid> ids) => e => ids.Contains(e.FarmId);
    public override PaddockRequest Read(Paddock e) => new(e.FarmId, e.Name, e.Code, e.AreaHectares, e.Capacity, e.IsActive);
    public override void Apply(Paddock e, PaddockRequest q)
    {
        e.FarmId = q.FarmId;
        e.Name = Clean(q.Name);
        e.Code = Optional(q.Code);
        e.AreaHectares = q.AreaHectares;
        e.Capacity = q.Capacity;
        e.IsActive = q.IsActive;
    }

    public override async Task CheckAsync(Paddock e, PaddockRequest q, CancellationToken ct)
    {
        Check((await Require<Farm>(q.FarmId, ct)).IsActive, "The farm is inactive.");
        var occupied = await Repository.CountAsync<Animal>(a => a.PaddockId == e.Id && a.Status == AnimalStatus.Active, ct);
        Check(q.Capacity == null || q.Capacity >= occupied, "The capacity cannot be lower than the current occupancy.");
        Check(q.IsActive || occupied == 0, "An occupied paddock cannot be deactivated.");
        await Unique<Paddock>(x => x.Id != e.Id && x.FarmId == q.FarmId && x.Name == q.Name.Trim(), ct);
    }
}

public sealed class LotDefinition(IManagementRepository r) : ResourceDefinition<Lot, LotRequest>(r)
{
    public override Guid? FarmId(Lot e) => e.FarmId;
    public override Guid? FarmId(LotRequest q) => q.FarmId;
    public override Expression<Func<Lot, bool>> Scope(IReadOnlyCollection<Guid> ids) => e => ids.Contains(e.FarmId);
    public override LotRequest Read(Lot e) => new(e.FarmId, e.SpeciesId, e.Name, e.Purpose, e.PaddockId, e.IsActive);
    public override void Apply(Lot e, LotRequest q)
    {
        e.FarmId = q.FarmId;
        e.SpeciesId = q.SpeciesId;
        e.Name = Clean(q.Name);
        e.Purpose = q.Purpose;
        e.PaddockId = q.PaddockId;
        e.IsActive = q.IsActive;
    }

    public override async Task CheckAsync(Lot e, LotRequest q, CancellationToken ct)
    {
        await Require<Farm>(q.FarmId, ct);
        await Require<Species>(q.SpeciesId, ct);
        if (q.PaddockId is Guid p)
            Check((await Require<Paddock>(p, ct)).FarmId == q.FarmId, "The paddock belongs to a different farm.");
        if (e.SpeciesId != Guid.Empty && e.SpeciesId != q.SpeciesId)
            Check(!await Repository.ExistsAsync<Animal>(a => a.LotId == e.Id, ct), "A lot used by animals cannot change species.");
        await Unique<Lot>(x => x.Id != e.Id && x.FarmId == q.FarmId && x.Name == q.Name.Trim(), ct);
    }
}

public sealed class CategoryDefinition(IManagementRepository r) : ResourceDefinition<InventoryCategory, CategoryRequest>(r)
{
    public override CategoryRequest Read(InventoryCategory e) => new(e.Name, e.Description, e.IsActive);
    public override void Apply(InventoryCategory e, CategoryRequest q)
    {
        e.Name = Clean(q.Name);
        e.Description = Optional(q.Description);
        e.IsActive = q.IsActive;
    }

    public override Task CheckAsync(InventoryCategory e, CategoryRequest q, CancellationToken ct) => Unique<InventoryCategory>(x => x.Id != e.Id && x.Name == q.Name.Trim(), ct);
}

public sealed class ProductDefinition(IManagementRepository r) : ResourceDefinition<Product, ProductRequest>(r)
{
    public override ProductRequest Read(Product e) => new(e.SKU, e.Name, e.CategoryId, e.Price, e.CostPrice, e.Unit, e.Brand, e.WithdrawalDays, e.RequiresPrescription, e.IsActive);
    public override void Apply(Product e, ProductRequest q)
    {
        e.SKU = Clean(q.SKU).ToUpperInvariant();
        e.Name = Clean(q.Name);
        e.CategoryId = q.CategoryId;
        e.Price = q.Price;
        e.CostPrice = q.CostPrice;
        e.Unit = q.Unit;
        e.Brand = Clean(q.Brand);
        e.WithdrawalDays = q.WithdrawalDays;
        e.RequiresPrescription = q.RequiresPrescription;
        e.IsActive = q.IsActive;
    }

    public override async Task CheckAsync(Product e, ProductRequest q, CancellationToken ct)
    {
        Check((await Require<InventoryCategory>(q.CategoryId, ct)).IsActive, "The category is inactive.");
        await Unique<Product>(x => x.Id != e.Id && x.SKU == q.SKU.Trim().ToUpper(), ct);
        if (e.Unit != q.Unit)
            Check(!await Repository.ExistsAsync<FarmInventory>(i => i.ProductId == e.Id, ct), "A stocked product cannot change its measurement unit.");
    }
}

public sealed class InventoryDefinition(IManagementRepository r) : ResourceDefinition<FarmInventory, InventoryRequest>(r)
{
    public override Guid? FarmId(FarmInventory e) => e.FarmId;
    public override Guid? FarmId(InventoryRequest q) => q.FarmId;
    public override Expression<Func<FarmInventory, bool>> Scope(IReadOnlyCollection<Guid> ids) => e => ids.Contains(e.FarmId);
    public override InventoryRequest Read(FarmInventory e) => new(e.FarmId, e.ProductId, e.Stock, e.MinStock, e.MaxStock, e.Location);
    public override void Apply(FarmInventory e, InventoryRequest q)
    {
        e.FarmId = q.FarmId;
        e.ProductId = q.ProductId;
        e.Stock = q.Stock;
        e.MinStock = q.MinStock;
        e.MaxStock = q.MaxStock;
        e.Location = Clean(q.Location);
    }

    public override async Task CheckAsync(FarmInventory e, InventoryRequest q, CancellationToken ct)
    {
        await Require<Farm>(q.FarmId, ct);
        Check((await Require<Product>(q.ProductId, ct)).IsActive, "The product is inactive.");
        Check(e.ProductId == Guid.Empty || e.ProductId == q.ProductId, "An inventory record cannot change product.");
        await Unique<FarmInventory>(x => x.Id != e.Id && x.FarmId == q.FarmId && x.ProductId == q.ProductId, ct);
    }
}

public sealed class AnimalDefinition(IManagementRepository r, ICurrentUser user, AnimalLocationPolicy location) : ResourceDefinition<Animal, AnimalRequest>(r)
{
    public override Guid? FarmId(Animal e) => e.FarmId;
    public override Guid? FarmId(AnimalRequest q) => q.FarmId;
    public override Expression<Func<Animal, bool>> Scope(IReadOnlyCollection<Guid> ids) => e => ids.Contains(e.FarmId);
    public override AnimalRequest Read(Animal e) => new(e.FarmId, e.SpeciesId, e.InternalTag, e.Sex, e.Purpose, e.BreedId, e.LotId, e.PaddockId, e.OfficialId, e.Rfid, e.Name, e.BirthDate, e.BirthWeightKg, e.Color, e.Markings, e.Status, e.Origin, e.HealthStatus, e.DamId, e.SireId, e.Notes);
    public override void Apply(Animal e, AnimalRequest q)
    {
        e.FarmId = q.FarmId;
        e.SpeciesId = q.SpeciesId;
        e.InternalTag = Clean(q.InternalTag).ToUpperInvariant();
        e.Sex = q.Sex;
        e.Purpose = q.Purpose;
        e.BreedId = q.BreedId;
        e.LotId = q.LotId;
        e.PaddockId = q.PaddockId;
        e.OfficialId = Optional(q.OfficialId)?.ToUpperInvariant();
        e.Rfid = Optional(q.Rfid)?.ToUpperInvariant();
        e.Name = Optional(q.Name);
        e.BirthDate = q.BirthDate;
        e.BirthWeightKg = q.BirthWeightKg;
        e.Color = Optional(q.Color);
        e.Markings = Optional(q.Markings);
        e.Status = q.Status;
        e.Origin = q.Origin;
        e.HealthStatus = q.HealthStatus;
        e.DamId = q.DamId;
        e.SireId = q.SireId;
        e.Notes = Optional(q.Notes);
    }

    public override async Task CheckAsync(Animal e, AnimalRequest q, CancellationToken ct)
    {
        Check((await Require<Farm>(q.FarmId, ct)).IsActive, "The farm is inactive.");
        Check((await Require<Species>(q.SpeciesId, ct)).IsActive, "The species is inactive.");
        if (q.BreedId is Guid b)
        {
            var breed = await Require<Breed>(b, ct);
            Check(breed.SpeciesId == q.SpeciesId && breed.IsActive, "The breed does not match the active species.");
        }

        await location.CheckAsync(e.Id, q.FarmId, q.SpeciesId, q.Status, q.PaddockId, q.LotId, ct);

        await CheckParent(e, q, q.DamId, Sex.Female, ct);
        await CheckParent(e, q, q.SireId, Sex.Male, ct);
        var official = Optional(q.OfficialId)?.ToUpperInvariant();
        var rfid = Optional(q.Rfid)?.ToUpperInvariant();
        var tag = q.InternalTag.Trim().ToUpperInvariant();
        await Unique<Animal>(x => x.Id != e.Id && x.FarmId == q.FarmId && (x.InternalTag == tag || (official != null && x.OfficialId == official) || (rfid != null && x.Rfid == rfid)), ct);
        if (e.FarmId != Guid.Empty)
        {
            if (e.BirthDate != q.BirthDate && q.BirthDate is DateOnly birth)
                Check(!await Repository.ExistsAsync<WeightRecord>(record => record.AnimalId == e.Id && record.Date < birth, ct), "Birth date cannot follow an existing weighing date.");
            if (e.SpeciesId != q.SpeciesId || e.Sex != q.Sex || e.BirthDate != q.BirthDate)
            {
                Check(!await Repository.ExistsAsync<HealthEvent>(x => x.AnimalId == e.Id, ct) &&
                    !await Repository.ExistsAsync<ReproductiveEvent>(x => x.DamId == e.Id || (x is Mating && ((Mating)x).SireId == e.Id) || (x is Insemination && ((Insemination)x).SireId == e.Id) || (x is Weaning && ((Weaning)x).OffspringId == e.Id), ct), "An animal with care history cannot change species, sex or birth date.");
                Check(!await Repository.ExistsAsync<AnimalProduction>(x => x.AnimalId == e.Id, ct), "An animal with production history cannot change species, sex or birth date.");
                Check(!await Repository.ExistsAsync<Animal>(x => x.DamId == e.Id || x.SireId == e.Id, ct), "A recorded parent cannot change species, sex or birth date while offspring reference it.");
            }
            if (e.Status == AnimalStatus.Dead && q.Status != AnimalStatus.Dead)
                Check(!await Repository.ExistsAsync<AnimalProduction>(x => x.AnimalId == e.Id && x.Method == ProductionMethod.Slaughter, ct), "A slaughtered animal cannot become active again.");
            if (e.HealthStatus != q.HealthStatus)
            {
                Check(e.Status == AnimalStatus.Active && q.Status == AnimalStatus.Active, "Only active animals can change health status.");
                Repository.Add(new HealthStatusChange { FarmId = e.FarmId, AnimalId = e.Id, PreviousStatus = e.HealthStatus, NewStatus = q.HealthStatus, Reason = "Animal record updated", UserId = user.UserId });
            }
        }
        if (e.PaddockId != q.PaddockId || e.LotId != q.LotId)
            Repository.Add(new AnimalMovement { FarmId = q.FarmId, AnimalId = e.Id, FromPaddockId = e.PaddockId, ToPaddockId = q.PaddockId,
                FromLotId = e.LotId, ToLotId = q.LotId, Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Reason = e.FarmId == Guid.Empty ? "Initial location registered" : "Animal record updated", UserId = user.UserId });
    }

    private async Task CheckParent(Animal e, AnimalRequest q, Guid? parentId, Sex expectedSex, CancellationToken ct)
    {
        if (parentId is not Guid id)
            return;
        var parent = await Require<Animal>(id, ct);
        Check(parent.Id != e.Id && parent.FarmId == q.FarmId && parent.SpeciesId == q.SpeciesId && parent.Sex == expectedSex, "The parent does not match the farm, species or sex.");
        Check(q.BirthDate == null || parent.BirthDate == null || parent.BirthDate < q.BirthDate, "A parent must be older than the offspring.");
        var pending = new Queue<Guid>();
        pending.Enqueue(id);
        var seen = new HashSet<Guid>();
        while (pending.TryDequeue(out var ancestorId))
        {
            Check(ancestorId != e.Id, "The parent relationship would create a cycle.");
            if (!seen.Add(ancestorId))
                continue;
            var ancestor = await Require<Animal>(ancestorId, ct);
            if (ancestor.DamId is Guid dam)
                pending.Enqueue(dam);
            if (ancestor.SireId is Guid sire)
                pending.Enqueue(sire);
        }
    }

    public override async Task BeforeDeleteAsync(Animal e, CancellationToken ct)
    {
        Check(!await Repository.ExistsAsync<AnimalPhoto>(x => x.AnimalId == e.Id, ct), "Delete the animal photos before deleting the animal.");
        Check(!await Repository.ExistsAsync<HealthStatusChange>(x => x.AnimalId == e.Id, ct), "This animal has health history. Deactivate it to preserve traceability.");
    }
}

public sealed class WeightDefinition(IManagementRepository r, ICurrentUser user) : ResourceDefinition<WeightRecord, WeightRequest>(r)
{
    public override Guid? FarmId(WeightRecord e) => e.FarmId;
    public override Guid? FarmId(WeightRequest q) => q.FarmId;
    public override Expression<Func<WeightRecord, bool>> Scope(IReadOnlyCollection<Guid> ids) => e => ids.Contains(e.FarmId);
    public override WeightRequest Read(WeightRecord e) => new(e.FarmId, e.AnimalId, e.Date, e.WeightKg, e.BodyConditionScore, e.Notes);
    public override void Apply(WeightRecord e, WeightRequest q)
    {
        e.FarmId = q.FarmId;
        e.AnimalId = q.AnimalId;
        e.Date = q.Date;
        e.WeightKg = q.WeightKg;
        e.BodyConditionScore = q.BodyConditionScore;
        e.Notes = Optional(q.Notes);
    }

    public override async Task CheckAsync(WeightRecord e, WeightRequest q, CancellationToken ct)
    {
        var animal = await Repository.GetAsync<Animal>(q.AnimalId, true, ct) ?? throw new ArgumentException("The animal does not exist.");
        Check(animal.FarmId == q.FarmId, "The animal belongs to a different farm.");
        Check(e.AnimalId == Guid.Empty || e.AnimalId == q.AnimalId, "A weight record cannot change animal.");
        Check(animal.BirthDate == null || q.Date >= animal.BirthDate, "The weighing date precedes the animal birth.");
        var slaughter = await Repository.ListAsync<AnimalProduction>(x => x.AnimalId == animal.Id && x.Method == ProductionMethod.Slaughter, ct);
        Check(slaughter.All(x => q.Date < x.Date ||
            (e.AnimalId != Guid.Empty && e.Date == q.Date && q.Date == x.Date && e.CreatedAt < x.CreatedAt)),
            "Live weight cannot be recorded on or after slaughter; an existing weight recorded earlier that day may be corrected.");
        if (e.AnimalId == Guid.Empty)
            e.RecordedByUserId = user.UserId;
        animal.UpdatedAt = DateTime.UtcNow;
    }
}

public sealed class ProductionDefinition(IManagementRepository r) : ResourceDefinition<AnimalProduction, ProductionRequest>(r)
{
    public override Guid? FarmId(AnimalProduction e) => e.FarmId;
    public override Guid? FarmId(ProductionRequest q) => q.FarmId;
    public override Expression<Func<AnimalProduction, bool>> Scope(IReadOnlyCollection<Guid> ids) => e => ids.Contains(e.FarmId);
    public override ProductionRequest Read(AnimalProduction e) => new(e.FarmId, e.AnimalId, e.Date, e.ProductType, e.Method, e.Quantity, e.Unit, e.OperationId, e.Notes);
    public override void Apply(AnimalProduction e, ProductionRequest q)
    {
        e.FarmId = q.FarmId;
        e.AnimalId = q.AnimalId;
        e.Date = q.Date;
        e.ProductType = q.ProductType;
        e.Method = q.Method;
        e.Quantity = q.Quantity;
        e.Unit = q.Unit;
        e.OperationId = q.OperationId;
        e.Notes = Optional(q.Notes);
    }

    public override async Task CheckAsync(AnimalProduction e, ProductionRequest q, CancellationToken ct)
    {
        var animal = await Repository.GetAsync<Animal>(q.AnimalId, true, ct) ?? throw new ArgumentException("The animal does not exist.");
        Check(animal.FarmId == q.FarmId, "The animal belongs to a different farm.");
        Check(e.AnimalId == Guid.Empty || e.AnimalId == q.AnimalId, "A production record cannot change animal.");
        Check(animal.BirthDate == null || q.Date >= animal.BirthDate, "The production date precedes the animal birth.");
        if (e.AnimalId != Guid.Empty)
            Check(e.OperationId == q.OperationId && e.Method == q.Method && e.Date == q.Date, "An existing operation keeps its identifier, method and date; only its yield can be corrected.");
        if (q.Method is ProductionMethod.Milking or ProductionMethod.Slaughter)
            await new WithdrawalPolicy(Repository).CheckAsync(animal.Id, q.Date, ct);
        var slaughter = await Repository.ListAsync<AnimalProduction>(x => x.AnimalId == q.AnimalId && x.Method == ProductionMethod.Slaughter, ct);
        if (q.Method == ProductionMethod.Slaughter)
        {
            Check(slaughter.All(x => x.OperationId == q.OperationId && x.Date == q.Date), "An animal cannot be slaughtered in a second operation.");
            Check(animal.Status == AnimalStatus.Active || slaughter.Any(x => x.OperationId == q.OperationId), "Only an active animal can start a slaughter operation.");
            Check(!await Repository.ExistsAsync<AnimalProduction>(x => x.AnimalId == q.AnimalId && x.Method != ProductionMethod.Slaughter && x.Date > q.Date, ct), "Slaughter cannot precede an existing production record.");
            Check(!await Repository.ExistsAsync<WeightRecord>(x => x.AnimalId == q.AnimalId && x.Date > q.Date, ct), "Slaughter cannot precede an existing live weight record.");
            animal.Status = AnimalStatus.Dead;
        }
        else
        {
            Check(animal.Status == AnimalStatus.Active || (slaughter.Count > 0 && slaughter.All(x => q.Date < x.Date)), "This animal is not available for production on that date.");
            Check(slaughter.All(x => q.Date < x.Date), "Production cannot occur on or after slaughter.");
        }

        if (q.Method == ProductionMethod.Milking)
            Check(animal.Sex == Sex.Female, "Only female animals can be milked.");
        var species = await Require<Species>(animal.SpeciesId, ct);
        if (q.Method == ProductionMethod.Milking)
            Check(species.Code is "BO" or "CA" or "OV", "This species is not supported for milking.");
        if (q.Method == ProductionMethod.Shearing)
            Check(species.Code == "OV", "Only sheep are supported for shearing.");
        if (q.ProductType == AnimalProductType.Eggs)
            Check(species.Code == "AV" && animal.Sex == Sex.Female, "Egg production requires a female bird.");
        var operation = await Repository.ListAsync<AnimalProduction>(x => x.OperationId == q.OperationId && x.Id != e.Id, ct);
        Check(operation.All(x => x.AnimalId == q.AnimalId && x.FarmId == q.FarmId && x.Date == q.Date && x.Method == q.Method), "The operation identifier already belongs to a different animal, date or method.");
        await Unique<AnimalProduction>(x => x.Id != e.Id && x.OperationId == q.OperationId && x.ProductType == q.ProductType, ct);
        animal.UpdatedAt = DateTime.UtcNow;
    }

    public override Task BeforeDeleteAsync(AnimalProduction e, CancellationToken ct)
    {
        Check(e.Method != ProductionMethod.Slaughter, "Slaughter is irreversible. Correct its yield or deactivate the record through a future reversal workflow.");
        return Task.CompletedTask;
    }
}
