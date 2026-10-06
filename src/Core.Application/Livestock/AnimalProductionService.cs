using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;

namespace Core.Application.Livestock;

public sealed record AnimalProductionRequest(Guid SubmissionId, DateOnly Date, AnimalProductType ProductType,
    ProductionMethod Method, decimal Quantity, MeasurementUnit Unit, string? Notes = null);
public sealed class AnimalProductionService(IManagementRepository repository, IFarmAccess farms,
    IResourceDefinition<AnimalProduction, ProductionRequest> definition, IValidator<ProductionRequest> validator,
    IValidator<CarePageRequest> pages)
{
    public async Task<CarePage<ResourceResult<ProductionRequest>>> GetAsync(Guid animalId, CarePageRequest page, CancellationToken ct = default)
    {
        await pages.ValidateAndThrowAsync(page, ct);
        var animal = await AnimalCareRules.FindAsync(repository, farms, animalId, false, ct);
        var total = await repository.CountAsync<AnimalProduction>(x => x.AnimalId == animal.Id && x.FarmId == animal.FarmId, ct);
        var records = await repository.PageAsync<AnimalProduction, DateOnly>(x => x.AnimalId == animal.Id && x.FarmId == animal.FarmId, x => x.Date, (page.Page - 1) * page.PageSize, page.PageSize, ct);
        return new(records.Select(x => new ResourceResult<ProductionRequest>(x.Id, x.CreatedAt, definition.Read(x))).ToList(), total, page.Page, page.PageSize);
    }
    public Task<CareSubmission<ProductionRequest>> RecordAsync(Guid animalId, AnimalProductionRequest request, CancellationToken ct = default) =>
        repository.ExecuteWriteAsync<CareSubmission<ProductionRequest>>(async () =>
        {
            var animal = await AnimalCareRules.FindAsync(repository, farms, animalId, true, ct);
            var q = new ProductionRequest(animal.FarmId, animal.Id, request.Date, request.ProductType, request.Method,
                request.Quantity, request.Unit, request.SubmissionId, AnimalCareRules.Clean(request.Notes));
            await validator.ValidateAndThrowAsync(q, ct);
            var existing = await repository.GetAsync<AnimalProduction>(request.SubmissionId, ct: ct);
            if (existing != null)
            {
                AnimalCareRules.Check(definition.Read(existing) == q, "The submission identifier belongs to a different production record.");
                return new(existing.Id, true, q);
            }
            var entity = new AnimalProduction(request.SubmissionId);
            await definition.CheckAsync(entity, q, ct);
            definition.Apply(entity, q);
            repository.Add(entity);
            await repository.SaveAsync(ct);
            return new(entity.Id, false, q);
        }, ct);
}
