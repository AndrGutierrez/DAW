namespace Core.Application.Security;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task<UserResult> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default);
}

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) CreateAccessToken(
        Guid userId,
        string username,
        IEnumerable<string> roles,
        bool isSuperuser,
        string? email = null);

    string CreateRefreshToken();
}
