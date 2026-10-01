namespace Core.Application.Security;

public sealed record RegisterRequest(string Username, string Email, string Password, string FullName);

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record UserResult(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsSuperuser,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    UserResult User);
