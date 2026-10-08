using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;
namespace Core.Application.Operations;

public sealed class InventoryService(IManagementRepository repository, IFarmAccess farms, ICurrentUser user, IOperationsReader reader)
{
    public async Task<CarePage<InventoryRow>> PageAsync(InventoryQuery q, CancellationToken ct = default) { await new InventoryQueryValidator().ValidateAndThrowAsync(q, ct); return await reader.InventoryAsync(q, await farms.GetAccessibleFarmIdsAsync(ct), ct); }
    public async Task<CarePage<ResourceResult<ProductRequest>>> ProductsAsync(ProductQuery q, CancellationToken ct = default) { await new ProductQueryValidator().ValidateAndThrowAsync(q, ct); return await reader.ProductsAsync(q, ct); }
    private async Task<FarmInventory> FindAsync(Guid id, bool tracking, CancellationToken ct) { var e = await repository.GetAsync<FarmInventory>(id, tracking, ct) ?? throw new KeyNotFoundException("Inventory not found."); if (!await farms.CanAccessAsync(e.FarmId, ct)) throw new KeyNotFoundException("Inventory not found."); return e; }
    public async Task<CarePage<StockRecord>> HistoryAsync(Guid id, CarePageRequest q, CancellationToken ct = default)
    {
        await new CarePageRequestValidator().ValidateAndThrowAsync(q, ct); var e = await FindAsync(id, false, ct);
        var total = await repository.CountAsync<StockMovement>(m => m.FarmId == e.FarmId && m.ProductId == e.ProductId, ct);
        var items = await repository.PageAsync<StockMovement, DateTime>(m => m.FarmId == e.FarmId && m.ProductId == e.ProductId, m => m.CreatedAt, (q.Page - 1) * q.PageSize, q.PageSize, ct);
        return new(items.Select(Read).ToList(), total, q.Page, q.PageSize);
    }
    public Task<CareSubmission<StockRecord>> RecordAsync(Guid id, StockRequest request, CancellationToken ct = default) => repository.ExecuteWriteAsync(async () =>
    {
        await new StockRequestValidator().ValidateAndThrowAsync(request, ct); var q = request with { Reason = request.Reason.Trim() };
        var e = await FindAsync(id, true, ct); var existing = await repository.GetAsync<StockMovement>(q.SubmissionId, ct: ct);
        var reference = q.AnimalId.HasValue ? "Animal" : "Inventory"; var referenceId = q.AnimalId ?? e.Id;
        if (existing != null)
        {
            if (existing.FarmId != e.FarmId || existing.ProductId != e.ProductId || existing.UserId != user.UserId || existing.Type != q.Type || existing.Quantity != q.Quantity || existing.Reason != q.Reason || existing.ReferenceType != reference || existing.ReferenceId != referenceId)
                throw new ConflictException("The submission identifier belongs to a different stock movement.");
            return new CareSubmission<StockRecord>(existing.Id, true, Read(existing));
        }
        if (e.Stock != q.ExpectedStock) throw new ConflictException("The stock changed. Refresh the balance before recording a movement.");
        var product = await repository.GetAsync<Product>(e.ProductId, ct: ct);
        var farm = await repository.GetAsync<Farm>(e.FarmId, ct: ct);
        if (product is not { IsActive: true } || farm is not { IsActive: true }) throw new ConflictException("The farm or product is inactive.");
        if (q.AnimalId is Guid animalId)
        {
            var animal = await repository.GetAsync<Animal>(animalId, ct: ct);
            if (animal == null || animal.FarmId != e.FarmId) throw new KeyNotFoundException("The animal is not in this farm.");
            if (animal.Status != AnimalStatus.Active) throw new ConflictException("Only active animals can receive supplies.");
        }
        var next = e.Stock + (q.Type == StockMovementType.In ? q.Quantity : -q.Quantity);
        if (next < 0 || next >= 10000000000m) throw new ConflictException("The movement exceeds the available stock or storage limit.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!await repository.ExistsAsync<StockMovement>(m => m.FarmId == e.FarmId && m.ProductId == e.ProductId && m.ReferenceType == "OpeningBalance", ct))
            repository.Add(new StockMovement { FarmId = e.FarmId, ProductId = e.ProductId, Type = StockMovementType.Adjustment, Quantity = e.Stock, Date = today, Reason = "Opening balance for traced movements", ReferenceType = "OpeningBalance", ReferenceId = e.Id, UserId = user.UserId });
        var movement = new StockMovement(q.SubmissionId) { FarmId = e.FarmId, ProductId = e.ProductId, Type = q.Type, Quantity = q.Quantity, Date = today, Reason = q.Reason, ReferenceType = reference, ReferenceId = referenceId, UserId = user.UserId };
        e.Stock = next; repository.Add(movement); await repository.SaveAsync(ct);
        return new CareSubmission<StockRecord>(movement.Id, false, Read(movement));
    }, ct);
    private static StockRecord Read(StockMovement m) => new(m.Id, m.Date, m.Type, m.Quantity, m.Reason, m.ReferenceType == "Animal" ? m.ReferenceId : null, m.UserId, m.ReferenceType == "OpeningBalance");
}
