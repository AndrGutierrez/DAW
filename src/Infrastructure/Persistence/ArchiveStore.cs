using Core.Application.Management;
using Core.Application.Livestock;
using Core.Domain.Common;
using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
namespace Infrastructure.Persistence;
public sealed class ArchiveStore(AppDbContext db, IServiceProvider services) : IArchiveStore
{
    private static readonly string[] Resources = ["farms", "species", "breeds", "paddocks", "lots", "categories", "products", "inventory", "animals", "weights", "production", "photos"];
    private IQueryable<ArchiveEntry> Entries =>
        db.Set<Farm>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "farms", Label = e.Name, FarmId = e.Id, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId })
            .Concat(db.Set<Species>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "species", Label = e.Name, FarmId = null, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<Breed>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "breeds", Label = e.Name, FarmId = null, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<Paddock>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "paddocks", Label = e.Name, FarmId = e.FarmId, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<Lot>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "lots", Label = e.Name, FarmId = e.FarmId, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<InventoryCategory>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "categories", Label = e.Name, FarmId = null, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<Product>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "products", Label = e.Name + " · " + e.SKU, FarmId = null, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<FarmInventory>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "inventory", Label = e.Product.Name + " · " + e.Location, FarmId = e.FarmId, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<Animal>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "animals", Label = e.InternalTag + " · " + (e.Name ?? ""), FarmId = e.FarmId, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<WeightRecord>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "weights", Label = e.Animal.InternalTag + " · " + e.WeightKg.ToString() + " kg", FarmId = e.FarmId, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<AnimalProduction>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "production", Label = e.Animal.InternalTag + " · " + e.Quantity.ToString(), FarmId = e.FarmId, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }))
            .Concat(db.Set<AnimalPhoto>().IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => new ArchiveEntry { Id = e.Id, Resource = "photos", Label = e.Animal.InternalTag + " · fotografía", FarmId = e.FarmId, DeletedAt = e.DeletedAt!.Value, DeletedByUserId = e.DeletedByUserId }));
    public async Task<CarePage<ArchiveEntry>> PageAsync(ArchiveQuery q, CancellationToken ct)
    {
        var query = Entries;
        if (!string.IsNullOrWhiteSpace(q.Resource))
        {
            CheckResource(q.Resource); query = query.Where(e => e.Resource == q.Resource);
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim().ToUpperInvariant();
            query = query.Where(e => e.Label.ToUpper().Contains(search) || e.Resource.ToUpper().Contains(search));
        }
        var total = await query.CountAsync(ct);
        var page = await query.OrderByDescending(e => e.DeletedAt).ThenByDescending(e => e.Id).ThenBy(e => e.Resource).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        return new(page, total, q.Page, q.PageSize);
    }
    public async Task<ArchiveDetail> DetailAsync(string resource, Guid id, CancellationToken ct)
    {
        CheckResource(resource);
        var entry = await Entries.FirstOrDefaultAsync(e => e.Resource == resource && e.Id == id, ct) ?? throw new KeyNotFoundException("The archived record was not found.");
        BaseEntity record = resource switch
        {
            "farms" => await Find<Farm>(id, ct),
            "species" => await Find<Species>(id, ct),
            "breeds" => await Find<Breed>(id, ct),
            "paddocks" => await Find<Paddock>(id, ct),
            "lots" => await Find<Lot>(id, ct),
            "categories" => await Find<InventoryCategory>(id, ct),
            "products" => await Find<Product>(id, ct),
            "inventory" => await Find<FarmInventory>(id, ct),
            "animals" => await Find<Animal>(id, ct),
            "weights" => await Find<WeightRecord>(id, ct),
            "production" => await Find<AnimalProduction>(id, ct),
            "photos" => await Find<AnimalPhoto>(id, ct),
            _ => throw new ArgumentException("Unknown archive resource.")
        };
        var values = db.Entry(record).Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        return new(entry, JsonSerializer.Serialize(values));
    }
    private async Task<T> Find<T>(Guid id, CancellationToken ct) where T : BaseEntity =>
        await db.Set<T>().IgnoreQueryFilters().AsNoTracking().SingleAsync(e => e.Id == id && e.IsDeleted, ct);
    public async Task RestoreAsync(string resource, Guid id, CancellationToken ct)
    {
        CheckResource(resource);
        switch (resource)
        {
            case "farms": await services.GetRequiredService<ICrudService<FarmRequest>>().RestoreAsync(id, ct); break;
            case "species": await services.GetRequiredService<ICrudService<SpeciesRequest>>().RestoreAsync(id, ct); break;
            case "breeds": await services.GetRequiredService<ICrudService<BreedRequest>>().RestoreAsync(id, ct); break;
            case "paddocks": await services.GetRequiredService<ICrudService<PaddockRequest>>().RestoreAsync(id, ct); break;
            case "lots": await services.GetRequiredService<ICrudService<LotRequest>>().RestoreAsync(id, ct); break;
            case "categories": await services.GetRequiredService<ICrudService<CategoryRequest>>().RestoreAsync(id, ct); break;
            case "products": await services.GetRequiredService<ICrudService<ProductRequest>>().RestoreAsync(id, ct); break;
            case "inventory": await services.GetRequiredService<ICrudService<InventoryRequest>>().RestoreAsync(id, ct); break;
            case "animals": await services.GetRequiredService<ICrudService<AnimalRequest>>().RestoreAsync(id, ct); break;
            case "weights": await services.GetRequiredService<ICrudService<WeightRequest>>().RestoreAsync(id, ct); break;
            case "production": await services.GetRequiredService<ICrudService<ProductionRequest>>().RestoreAsync(id, ct); break;
            case "photos":
                var photo = await db.AnimalPhotos.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new KeyNotFoundException("The photograph was not found.");
                await services.GetRequiredService<IAnimalPhotoService>().RestoreAsync(photo.AnimalId, id, ct); break;
        }
    }
    private static void CheckResource(string resource)
    {
        if (!Resources.Contains(resource)) throw new ArgumentException("Unknown archive resource.");
    }
}
