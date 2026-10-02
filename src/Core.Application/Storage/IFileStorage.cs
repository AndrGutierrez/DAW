namespace Core.Application.Storage;

public sealed record StoredFile(string RelativeUrl, string StoredFileName, long SizeBytes);

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(
        Stream content,
        string folder,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativeUrl, CancellationToken cancellationToken = default);

    Task DeleteAsync(string folder, string storedFileName, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string folder, string storedFileName, CancellationToken cancellationToken = default);
}
