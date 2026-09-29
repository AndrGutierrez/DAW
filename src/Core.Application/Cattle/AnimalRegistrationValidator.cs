namespace Core.Application.Cattle;

public sealed class AnimalRegistrationValidator : IAnimalRegistrationValidator
{
    public void Validate(string breed, DateOnly? dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(breed))
        {
            throw new ArgumentException("A breed is required.", nameof(breed));
        }

        if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("The date of birth cannot be in the future.", nameof(dateOfBirth));
        }
    }
}
