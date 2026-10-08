using Core.Application.Livestock;
using System.Text.Json;
using Core.Application.Security;
using Core.Application.Storage;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Livestock;

public sealed class AnimalPhotoService(AppDbContext db, IFileStorage storage, IFarmAccess farmAccess, ICurrentUser? actor = null) : IAnimalPhotoService
{
    public async Task<AnimalPhotoResult> UploadAsync(
        Guid animalId,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var farmIds = await farmAccess.GetAccessibleFarmIdsAsync(cancellationToken);
        var animal = await db.Animals
            .FirstOrDefaultAsync(candidate => candidate.Id == animalId && farmIds.Contains(candidate.FarmId), cancellationToken)
            ?? throw new KeyNotFoundException("The requested animal was not found.");

        if (sizeBytes <= 0)
        {
            throw new ArgumentException("The file is empty.", nameof(sizeBytes));
        }

        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
        {
            throw new ArgumentException("The file name must contain between 1 and 255 characters.", nameof(fileName));
        }

        var stored = await storage.SaveAsync(
            content,
            $"animals/{animal.Id}",
            fileName,
            contentType,
            cancellationToken);

        var photo = new AnimalPhoto
        {
            FarmId = animal.FarmId,
            AnimalId = animal.Id,
            FileName = stored.StoredFileName,
            ContentType = contentType,
            SizeBytes = stored.SizeBytes,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow
        };
        photo.Url = $"/api/animals/{animal.Id}/photos/{photo.Id}/content";

        db.AnimalPhotos.Add(photo);
        RecordAudit(photo, "Added", null, Snapshot(photo), actor?.UserId ?? userId);
        animal.UpdatedAt = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(stored.RelativeUrl, CancellationToken.None);
            throw;
        }

        return new AnimalPhotoResult(animal.Id, photo.Id, photo.Url, photo.UploadedAt);
    }

    public async Task DeleteAsync(Guid animalId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var farmIds = await farmAccess.GetAccessibleFarmIdsAsync(cancellationToken);
        var animal = await db.Animals
            .FirstOrDefaultAsync(candidate => candidate.Id == animalId && farmIds.Contains(candidate.FarmId), cancellationToken)
            ?? throw new KeyNotFoundException("The requested animal was not found.");

        var photo = await db.AnimalPhotos
            .FirstOrDefaultAsync(
                candidate => candidate.Id == photoId && candidate.AnimalId == animalId,
                cancellationToken)
            ?? throw new KeyNotFoundException("The requested photo was not found.");

        RecordAudit(photo, "Deleted", Snapshot(photo), null, actor?.UserId);
        db.AnimalPhotos.Remove(photo);
        animal.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync($"animals/{animal.Id}", photo.FileName, cancellationToken);
    }

    private static string Snapshot(AnimalPhoto photo) => JsonSerializer.Serialize(new
    {
        photo.Id, photo.FarmId, photo.AnimalId, photo.Url, photo.FileName, photo.ContentType,
        photo.SizeBytes, photo.UploadedAt, photo.UploadedByUserId
    });

    private void RecordAudit(AnimalPhoto photo, string action, string? before, string? after, Guid? userId) =>
        db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(AnimalPhoto), EntityId = photo.Id.ToString(), Action = action,
            FarmId = photo.FarmId, UserId = userId, IpAddress = actor?.IpAddress,
            OldValues = before, NewValues = after
        });

    public async Task<AnimalPhotoContent> OpenReadAsync(Guid animalId, Guid photoId,
        CancellationToken cancellationToken = default)
    {
        var farmIds = await farmAccess.GetAccessibleFarmIdsAsync(cancellationToken);
        var photo = await db.AnimalPhotos.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == photoId && candidate.AnimalId == animalId
                && db.Animals.Any(animal => animal.Id == animalId && farmIds.Contains(animal.FarmId)), cancellationToken)
            ?? throw new KeyNotFoundException("The requested photo was not found.");
        var contentType = Path.GetExtension(photo.FileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => throw new KeyNotFoundException("The image format is not supported.")
        };
        var content = await storage.OpenReadAsync($"animals/{animalId}", photo.FileName, cancellationToken);
        return new AnimalPhotoContent(content, contentType, photo.FileName);
    }
}
