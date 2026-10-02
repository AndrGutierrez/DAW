namespace Core.Application.Security;

public interface IFarmAccess
{
    Task<bool> CanAccessAsync(Guid farmId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetAccessibleFarmIdsAsync(CancellationToken cancellationToken = default);
}
