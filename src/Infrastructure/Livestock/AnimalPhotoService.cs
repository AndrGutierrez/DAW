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

        animal.PhotoUrl = stored.RelativeUrl;

        db.Attachments.Add(new Attachment
        {
            FarmId = animal.FarmId,
            OwnerType = nameof(Animal),
            OwnerId = animal.Id,
            FileName = fileName,
            Url = stored.RelativeUrl,
            ContentType = contentType,
            SizeBytes = stored.SizeBytes,
            UploadedByUserId = userId
        });

        await db.SaveChangesAsync(cancellationToken);

        return new AnimalPhotoResult(animal.Id, stored.RelativeUrl);
    }

    public async Task DeleteAsync(Guid animalId, CancellationToken cancellationToken = default)
    {
        var animal = await db.Animals
            .FirstOrDefaultAsync(candidate => candidate.Id == animalId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested animal was not found.");

        if (string.IsNullOrWhiteSpace(animal.PhotoUrl))
        {
            return;
        }

        await storage.DeleteAsync(animal.PhotoUrl, cancellationToken);

        var attachments = await db.Attachments
            .Where(attachment => attachment.OwnerType == nameof(Animal)
                && attachment.OwnerId == animal.Id
                && attachment.Url == animal.PhotoUrl)
            .ToListAsync(cancellationToken);

        db.Attachments.RemoveRange(attachments);
        animal.PhotoUrl = null;

        await db.SaveChangesAsync(cancellationToken);
    }
}
