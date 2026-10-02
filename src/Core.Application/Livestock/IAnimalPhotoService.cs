namespace Core.Application.Livestock;

public sealed record AnimalPhotoResult(Guid AnimalId, Guid PhotoId, string Url, DateTime UploadedAt);

public interface IAnimalPhotoService
{
    Task<AnimalPhotoResult> UploadAsync(
        Guid animalId,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid animalId, Guid photoId, CancellationToken cancellationToken = default);
}
