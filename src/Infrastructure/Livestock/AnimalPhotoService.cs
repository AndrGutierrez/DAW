using Core.Application.Livestock;
using Core.Application.Storage;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Livestock;

public sealed class AnimalPhotoService(AppDbContext db, IFileStorage storage) : IAnimalPhotoService
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
        var animal = await db.Animals
            .FirstOrDefaultAsync(candidate => candidate.Id == animalId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested animal was not found.");

        if (sizeBytes <= 0)
        {
            throw new ArgumentException("The file is empty.", nameof(sizeBytes));
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
            Url = stored.RelativeUrl,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = stored.SizeBytes,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow
        };

        db.AnimalPhotos.Add(photo);
        animal.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new AnimalPhotoResult(animal.Id, photo.Id, photo.Url, photo.UploadedAt);
    }

    public async Task DeleteAsync(Guid animalId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var animal = await db.Animals
            .FirstOrDefaultAsync(candidate => candidate.Id == animalId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested animal was not found.");

        var photo = await db.AnimalPhotos
            .FirstOrDefaultAsync(
                candidate => candidate.Id == photoId && candidate.AnimalId == animalId,
                cancellationToken)
            ?? throw new KeyNotFoundException("The requested photo was not found.");

        await storage.DeleteAsync(photo.Url, cancellationToken);

        db.AnimalPhotos.Remove(photo);
        animal.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }
}
