using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using FluentValidation;

namespace Core.Application.Livestock;

public sealed record AnimalWeighingRequest(Guid SubmissionId, DateOnly Date, decimal WeightKg, decimal? BodyConditionScore = null, string? Notes = null);
public sealed record AnimalWeighingResult(Guid Id, bool Replayed, WeightRequest Data);

public sealed class AnimalWeighingService(
    IManagementRepository repository, IFarmAccess farms, ICurrentUser user,
    IResourceDefinition<WeightRecord, WeightRequest> definition, IValidator<WeightRequest> validator)
{
    public Task<AnimalWeighingResult> RecordAsync(Guid animalId, AnimalWeighingRequest request, CancellationToken ct = default) =>
        repository.ExecuteWriteAsync(async () =>
        {
            if (request.SubmissionId == Guid.Empty)
                throw new ValidationException([new("SubmissionId", "A submission identifier is required.")]);
            var animal = await repository.GetAsync<Animal>(animalId, true, ct)
                ?? throw new KeyNotFoundException("The requested animal was not found.");
            if (!await farms.CanAccessAsync(animal.FarmId, ct))
                throw new KeyNotFoundException("The requested animal was not found.");
            var data = new WeightRequest(animal.FarmId, animalId, request.Date, request.WeightKg,
                request.BodyConditionScore, string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim());
            await validator.ValidateAndThrowAsync(data, ct);
            var existing = await repository.GetAsync<WeightRecord>(request.SubmissionId, false, ct);
            if (existing is not null)
            {
                if (existing.RecordedByUserId != user.UserId || definition.Read(existing) != data)
                    throw new ConflictException("The submission identifier has already been used for different data.");
                return new AnimalWeighingResult(existing.Id, true, data);
            }
            if (animal.Status != AnimalStatus.Active)
                throw new ConflictException("Only active animals can be weighed in this workflow.");
            var record = new WeightRecord(request.SubmissionId);
            await definition.CheckAsync(record, data, ct);
            definition.Apply(record, data);
            repository.Add(record);
            await repository.SaveAsync(ct);
            return new AnimalWeighingResult(record.Id, false, data);
        }, ct);
}
