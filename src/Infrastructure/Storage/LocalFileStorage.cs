using Core.Application.Storage;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

public sealed class LocalFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private readonly StorageOptions _options = options.Value;

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string folder,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name is required.", nameof(fileName));
        }

        if (_options.AllowedContentTypes.Length > 0
            && !_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The content type '{contentType}' is not allowed.", nameof(contentType));
        }

        var safeFolder = SanitizeFolder(folder);
        var extension = ResolveExtension(fileName, contentType);
        var storedName = $"{Guid.NewGuid():N}{extension}";

        var directory = Path.Combine(Path.GetFullPath(_options.RootPath), safeFolder);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, storedName);
        long size;

        await using (var target = File.Create(fullPath))
        {
            await content.CopyToAsync(target, cancellationToken);
            size = target.Length;
        }

        if (size > _options.MaxFileSizeBytes)
        {
            File.Delete(fullPath);
            throw new ArgumentException("The file exceeds the maximum allowed size.", nameof(content));
        }

        var requestPath = _options.RequestPath.TrimEnd('/');
        var relativeUrl = $"{requestPath}/{safeFolder}/{storedName}";

        return new StoredFile(relativeUrl, storedName, size);
    }

    public Task DeleteAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return Task.CompletedTask;
        }

        var requestPath = _options.RequestPath.TrimEnd('/');

        if (!relativeUrl.StartsWith(requestPath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var relative = relativeUrl[requestPath.Length..]
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        var fullPath = Path.Combine(Path.GetFullPath(_options.RootPath), relative);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static string SanitizeFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }

        var segments = folder
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Where(segment => segment != "..")
            .ToArray();

        return string.Join('/', segments);
    }

    private static string ResolveExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);

        if (!string.IsNullOrWhiteSpace(extension) && extension.Length <= 10)
        {
            return extension.ToLowerInvariant();
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin"
        };
    }
}
