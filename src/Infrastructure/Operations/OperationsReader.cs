using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Operations;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace Infrastructure.Operations;

public sealed class OperationsReader(AppDbContext db) : IOperationsReader
{
    public async Task<CarePage<InventoryRow>> InventoryAsync(InventoryQuery q, IReadOnlyCollection<Guid> farms, CancellationToken ct)
    {
        var rows = db.FarmInventory.AsNoTracking().Where(i => farms.Contains(i.FarmId));
        if (q.FarmId is Guid farm) rows = rows.Where(i => i.FarmId == farm);
        var search = q.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search)) rows = rows.Where(i => i.Product.Name.ToLower().Contains(search) || i.Product.SKU.ToLower().Contains(search));
        if (q.State == "Low") rows = rows.Where(i => i.Stock <= i.MinStock);
        if (q.State == "High") rows = rows.Where(i => i.Stock >= i.MaxStock);
        if (q.State == "Normal") rows = rows.Where(i => i.Stock > i.MinStock && i.Stock < i.MaxStock);
        var total = await rows.CountAsync(ct);
        var items = await rows.OrderBy(i => i.Product.Name).ThenBy(i => i.Farm.Name).ThenBy(i => i.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(i => new InventoryRow(i.Id, new InventoryRequest(i.FarmId, i.ProductId, i.Stock, i.MinStock, i.MaxStock, i.Location), i.Farm.Name, i.Product.Name, i.Product.SKU, i.Product.InventoryCategory.Name, i.Product.Unit, i.Product.IsActive)).ToListAsync(ct);
        return new(items, total, q.Page, q.PageSize);
    }
    public async Task<CarePage<ResourceResult<ProductRequest>>> ProductsAsync(ProductQuery q, CancellationToken ct)
    {
        var rows = db.Products.AsNoTracking().AsQueryable(); var search = q.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search)) rows = rows.Where(p => p.Name.ToLower().Contains(search) || p.SKU.ToLower().Contains(search));
        if (q.CategoryId is Guid category) rows = rows.Where(p => p.CategoryId == category);
        if (q.IsActive is bool active) rows = rows.Where(p => p.IsActive == active);
        var total = await rows.CountAsync(ct);
        var items = await rows.OrderBy(p => p.Name).ThenBy(p => p.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).Select(p => new ResourceResult<ProductRequest>(p.Id, p.CreatedAt, new ProductRequest(p.SKU, p.Name, p.CategoryId, p.Price, p.CostPrice, p.Unit, p.Brand, p.WithdrawalDays, p.RequiresPrescription, p.IsActive))).ToListAsync(ct);
        return new(items, total, q.Page, q.PageSize);
    }
    public async Task<AnalyticsInputs> AnalyticsAsync(PeriodQuery q, IReadOnlyCollection<Guid> farms, CancellationToken ct)
    {
        var ids = q.FarmId is Guid farm ? farms.Where(f => f == farm).ToArray() : farms.ToArray();
        var inventory = await db.FarmInventory.AsNoTracking().Where(i => ids.Contains(i.FarmId)).Select(i => new InventoryInput(i.Id, i.FarmId, i.ProductId, i.Farm.Name, i.Product.Name, i.Product.InventoryCategory.Name, i.Product.Unit, i.Stock, i.MinStock, i.MaxStock, i.Product.CostPrice, i.Product.Price)).ToListAsync(ct);
        var movements = await db.StockMovements.AsNoTracking().Where(m => ids.Contains(m.FarmId)).ToListAsync(ct);
        var milk = await db.AnimalProduction.IgnoreQueryFilters().AsNoTracking().Where(p => !p.IsDeleted && ids.Contains(p.FarmId) && p.Animal.FarmId == p.FarmId && p.Date >= q.From && p.Date <= q.To && p.ProductType == AnimalProductType.Milk)
            .Select(p => new MilkInput(p.Date, p.Farm.Name + " · " + (p.Animal.Lot == null ? "Sin lote actual" : p.Animal.Lot.Name), p.Quantity, p.Unit, p.FarmId, p.Animal.LotId)).ToListAsync(ct);
        var weights = await db.WeightRecords.IgnoreQueryFilters().AsNoTracking().Where(w => !w.IsDeleted && !w.Animal.IsDeleted && ids.Contains(w.FarmId) && w.Animal.FarmId == w.FarmId && w.Animal.Species.Code == "BO" && w.Date >= q.From && w.Date <= q.To)
            .Select(w => new WeightInput(w.AnimalId, w.Id, w.CreatedAt, w.Date, w.Animal.BirthDate, w.WeightKg)).ToListAsync(ct);
        var checks = await db.ReproductiveEvents.OfType<PregnancyCheck>().IgnoreQueryFilters().AsNoTracking().Where(c => !c.IsDeleted && ids.Contains(c.FarmId) && c.Dam.FarmId == c.FarmId && c.Date >= q.From && c.Date <= q.To).ToListAsync(ct);
        var calvings = await db.ReproductiveEvents.OfType<Calving>().IgnoreQueryFilters().AsNoTracking().Where(c => !c.IsDeleted && ids.Contains(c.FarmId) && c.Dam.FarmId == c.FarmId && c.Date >= q.From && c.Date <= q.To).ToListAsync(ct);
        var reproduction = await db.ReproductiveEvents.IgnoreQueryFilters().AsNoTracking().Where(e => !e.IsDeleted && ids.Contains(e.FarmId) &&
            e.Dam.FarmId == e.FarmId && e.Dam.Species.Code == "BO" && e.Dam.Sex == Sex.Female && e.Date >= q.From && e.Date <= q.To).ToListAsync(ct);
        return new(inventory, movements, milk, weights, checks, calvings, reproduction);
    }
    public async Task<ReportResult> ReportAsync(ReportQuery q, bool clinical, IReadOnlyCollection<Guid> farms, CancellationToken ct)
    {
        await using var snapshot = q.PageSize == 10000 && db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        var generatedAt = DateTime.UtcNow;
        var ids = q.FarmId is Guid farm ? farms.Where(f => f == farm).ToArray() : farms.ToArray();
        if (!clinical)
        {
            var rows = db.AnimalProduction.IgnoreQueryFilters().AsNoTracking().Where(p => !p.IsDeleted && ids.Contains(p.FarmId) && p.Animal.FarmId == p.FarmId && p.Date >= q.From && p.Date <= q.To);
            var total = await rows.CountAsync(ct);
            if (q.PageSize == 10000 && total > 10000) throw new ConflictException("The report exceeds 10000 records. Narrow the period or farm before exporting.");
            var page = await rows.OrderByDescending(p => p.Date).ThenByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
                .Select(p => new ReportRow(p.Id, p.Date, p.Farm.Name, p.Animal.InternalTag, p.ProductType.ToString(), null, p.Quantity, p.Unit.ToString(), p.Notes, null, p.Method.ToString())).ToListAsync(ct);
            return new(new(page, total, q.Page, q.PageSize), generatedAt);
        }
        var events = db.HealthEvents.IgnoreQueryFilters().AsNoTracking().Where(e => !e.IsDeleted && ids.Contains(e.FarmId) && e.Animal.FarmId == e.FarmId && e.Date >= q.From && e.Date <= q.To);
        var count = await events.CountAsync(ct);
        if (q.PageSize == 10000 && count > 10000) throw new ConflictException("The report exceeds 10000 records. Narrow the period or farm before exporting.");
        var items = await events.Include(e => e.Farm).Include(e => e.Animal).OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        var productIds = items.Select(AnimalCareService.Read).Where(e => e.ProductId.HasValue).Select(e => e.ProductId!.Value).ToArray();
        var products = await db.Products.IgnoreQueryFilters().AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name + " · " + p.SKU, ct);
        return new(new(items.Select(e => { var data = AnimalCareService.Read(e); return new ReportRow(e.Id, e.Date, e.Farm.Name, e.Animal.InternalTag, data.Kind.ToString(), data.ProductId is Guid product && products.TryGetValue(product, out var name) ? name : null, data.Dose, data.Dose.HasValue ? "DoseWithoutUnit" : null, e.Notes, (e as Treatment)?.WithdrawalEndDate, data.Reason ?? data.Severity ?? (e is Treatment ? data.Route.ToString() : null)); }).ToList(), count, q.Page, q.PageSize), generatedAt);
    }
}
