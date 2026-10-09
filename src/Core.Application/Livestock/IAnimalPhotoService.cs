namespace Core.Application.Livestock;

public sealed record AnimalPhotoResult(Guid AnimalId, Guid PhotoId, string Url, DateTime UploadedAt);

public sealed record AnimalPhotoContent(Stream Content, string ContentType, string FileName);

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
    Task RestoreAsync(Guid animalId, Guid photoId, CancellationToken ct = default);

    Task<AnimalPhotoContent> OpenReadAsync(Guid animalId, Guid photoId, CancellationToken cancellationToken = default);
}
