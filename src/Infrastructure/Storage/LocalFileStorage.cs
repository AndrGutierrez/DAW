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
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name is required.", nameof(fileName));
        }

        var extension = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new ArgumentException("Only JPEG, PNG and WebP images are supported.", nameof(contentType))
        };

        if (_options.AllowedContentTypes.Length > 0
            && !_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The content type '{contentType}' is not allowed.", nameof(contentType));
        }

        var segments = SafeSegments(folder);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var header = new byte[12];
        var headerSize = 0;
        while (headerSize < header.Length)
        {
            var read = await content.ReadAsync(header.AsMemory(headerSize), cancellationToken);
            if (read == 0) break;
            headerSize += read;
        }

        if (!HasImageSignature(header.AsSpan(0, headerSize), contentType))
        {
            throw new ArgumentException("The file does not match its image content type.", nameof(content));
        }

        EnsureSize(headerSize);
        var directory = ResolvePath(segments);
        Directory.CreateDirectory(directory);
        var fullPath = ResolvePath([.. segments, storedName]);
        long size = headerSize;

        try
        {
            await using var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, useAsync: true);
            await target.WriteAsync(header.AsMemory(0, headerSize), cancellationToken);
            var buffer = new byte[81920];
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                EnsureSize(size + read);
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                size += read;
            }
        }
        catch
        {
            File.Delete(fullPath);
            throw;
        }

        var relativeUrl = _options.RequestPath.TrimEnd('/') + "/"
            + string.Join('/', segments.Append(storedName).Select(Uri.EscapeDataString));

        return new StoredFile(relativeUrl, storedName, size);
    }

    public Task DeleteAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return Task.CompletedTask;
        }

        var prefix = _options.RequestPath.TrimEnd('/') + "/";

        if (!relativeUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var fullPath = ResolvePath(SafeSegments(Uri.UnescapeDataString(relativeUrl[prefix.Length..])));

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string folder, string storedFileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        File.Delete(ResolveFilePath(folder, storedFileName));
        return Task.CompletedTask;
    }

    public async Task<Stream> OpenReadAsync(string folder, string storedFileName,
        CancellationToken cancellationToken = default)
    {
        var path = ResolveFilePath(folder, storedFileName);
        var contentType = Path.GetExtension(storedFileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => throw new KeyNotFoundException("The image format is not supported.")
        };
        if (!File.Exists(path)) throw new KeyNotFoundException("The requested image was not found.");
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
            81920, useAsync: true);
        try
        {
            var header = new byte[12];
            var length = 0;
            while (length < header.Length)
            {
                var read = await stream.ReadAsync(header.AsMemory(length), cancellationToken);
                if (read == 0) break;
                length += read;
            }
            if (!HasImageSignature(header.AsSpan(0, length), contentType))
                throw new KeyNotFoundException("The stored file is not a supported image.");
            stream.Position = 0;
            return stream;
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }

    private string ResolveFilePath(string folder, string storedFileName)
    {
        var fileSegments = SafeSegments(storedFileName);
        if (fileSegments.Length != 1 || fileSegments[0] != storedFileName)
            throw new ArgumentException("A stored file name must be a single safe path segment.", nameof(storedFileName));
        return ResolvePath([.. SafeSegments(folder), storedFileName]);
    }

    private void EnsureSize(long size)
    {
        if (size > _options.MaxFileSizeBytes)
        {
            throw new ArgumentException("The file exceeds the maximum allowed size.", "content");
        }
    }

    private static string[] SafeSegments(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return [];
        if (Path.IsPathRooted(folder) || folder.StartsWith('/') || folder.StartsWith('\\'))
            throw new ArgumentException("Storage paths must be relative.", nameof(folder));
        var segments = folder
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
        if (segments.Any(segment => segment is "." or ".."
            || segment.Any(character => char.IsControl(character) || ":*?\"<>|".Contains(character))))
            throw new ArgumentException("The storage path contains an unsafe segment.", nameof(folder));
        return segments;
    }

    private string ResolvePath(string[] segments)
    {
        var root = Path.GetFullPath(_options.RootPath);
        var path = Path.GetFullPath(Path.Combine([root, .. segments]));
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!path.Equals(root, comparison) && !path.StartsWith(prefix, comparison))
            throw new ArgumentException("The storage path is outside the configured root.", nameof(segments));
        return path;
    }

    private static bool HasImageSignature(ReadOnlySpan<byte> header, string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/png" => header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/jpeg" => header.Length >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255,
            "image/webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
}
