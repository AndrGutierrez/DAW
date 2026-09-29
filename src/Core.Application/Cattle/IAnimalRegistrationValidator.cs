namespace Core.Application.Cattle;

public interface IAnimalRegistrationValidator
{
    void Validate(string breed, DateOnly? dateOfBirth);
}
