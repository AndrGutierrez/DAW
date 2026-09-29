using Core.Domain.Common;

namespace Core.Domain.Cattle;

public sealed class Animal : BaseEntity
{
    public Animal(string earTag, string breed, Herd herd, DateOnly? dateOfBirth = null)
    {
        if (string.IsNullOrWhiteSpace(earTag))
        {
            throw new ArgumentException("An ear tag is required.", nameof(earTag));
        }

        if (string.IsNullOrWhiteSpace(breed))
        {
            throw new ArgumentException("A breed is required.", nameof(breed));
        }

        Herd = herd ?? throw new ArgumentNullException(nameof(herd));
        EarTag = earTag.Trim();
        Breed = breed.Trim();
        DateOfBirth = dateOfBirth;
        HealthStatus = AnimalHealthStatus.Healthy;
    }

    public string EarTag { get; }
    public string Breed { get; }
    public DateOnly? DateOfBirth { get; }
    public AnimalHealthStatus HealthStatus { get; private set; }
    public Herd Herd { get; }
    public Guid HerdId => Herd.Id;

    public void ChangeHealthStatus(AnimalHealthStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        HealthStatus = status;
    }
}
