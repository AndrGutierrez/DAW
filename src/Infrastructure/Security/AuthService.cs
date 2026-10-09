using Core.Application.Security;
using Core.Domain.Livestock;
using System.Text.Json;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    ICurrentUser currentUser) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ArgumentException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        RecordAccess("RegistrationSucceeded", user.Id);
        return await BuildResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identifier = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            await RecordRejectedLoginAsync("CredentialsRejected", cancellationToken);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var user = await userManager.FindByNameAsync(identifier)
            ?? await userManager.FindByEmailAsync(identifier);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            await RecordRejectedLoginAsync("CredentialsRejected", cancellationToken);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            await RecordRejectedLoginAsync("InactiveAccount", cancellationToken);
            throw new UnauthorizedAccessException("The user account is inactive.");
        }

        RecordAccess("LoginSucceeded", user.Id);
        return await BuildResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var token = await FindRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (token is null || !token.IsActive || !token.User.IsActive)
            throw new UnauthorizedAccessException("The refresh token is invalid or expired.");

        token.RevokedAt = DateTime.UtcNow;
        try { return await BuildResponseAsync(token.User, cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new UnauthorizedAccessException("The refresh token has already been used or revoked.");
        }
    }

    public Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        RevokeAsync(refreshToken, false, cancellationToken);

    public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        RevokeAsync(refreshToken, true, cancellationToken);

    private async Task RevokeAsync(string value, bool logLogout, CancellationToken ct)
    {
        var token = await FindRefreshTokenAsync(value, ct);
        if (token is null || token.RevokedAt is not null) return;
        var wasActive = token.IsActive;
        token.RevokedAt = DateTime.UtcNow;
        if (logLogout) RecordAccess("Logout", token.UserId, new { SessionWasActive = wasActive });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { /* The token and event are rolled back together if already revoked. */ }
    }

    private void RecordAccess(string action, Guid? userId, object? details = null) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId, EntityName = "Authentication", EntityId = userId?.ToString(),
            Action = action, IpAddress = currentUser.IpAddress,
            NewValues = details is null ? null : JsonSerializer.Serialize(details)
        });

    private async Task RecordRejectedLoginAsync(string reason, CancellationToken ct)
    {
        // A claimed username is not an authenticated actor. Do not store submitted identifiers or credentials.
        RecordAccess("LoginRejected", null, new { Reason = reason });
        await db.SaveChangesAsync(ct);
    }

    private Task<RefreshToken?> FindRefreshTokenAsync(string value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
            return Task.FromResult<RefreshToken?>(null);
        var hash = HashRefreshToken(value);
        // Accept existing plaintext rows until their next rotation or expiry.
        return db.RefreshTokens.Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == hash || (value.Length == 88 && token.Token == value), cancellationToken);
    }

    private static string HashRefreshToken(string value) =>
        "sha256:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    public async Task<UserResult> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("The requested user was not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("The user account is inactive.");
        }

        return await BuildUserResultAsync(user, cancellationToken);
    }

    private async Task<AuthResponse> BuildResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var userResult = await BuildUserResultAsync(user, cancellationToken);

        var (accessToken, expiresAt) = tokenService.CreateAccessToken(
            user.Id,
            user.UserName!,
            userResult.Roles,
            user.IsSuperuser,
            user.Email, user.SecurityStamp);

        var refreshToken = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = HashRefreshToken(refreshToken),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });

        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken, expiresAt, userResult);
    }

    private async Task<UserResult> BuildUserResultAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();

        List<string> permissions;

        if (user.IsSuperuser)
        {
            permissions = await db.Permissions
                .AsNoTracking()
                .Select(permission => permission.Name)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var directPermissions = db.UserPermissions
                .AsNoTracking()
                .Where(userPermission => userPermission.UserId == user.Id)
                .Select(userPermission => userPermission.Permission.Name);

            var rolePermissions = db.RolePermissions
                .AsNoTracking()
                .Where(rolePermission => db.UserRoles.Any(
                    userRole => userRole.UserId == user.Id && userRole.RoleId == rolePermission.RoleId))
                .Select(rolePermission => rolePermission.Permission.Name);

            permissions = await directPermissions
                .Union(rolePermissions)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        return new UserResult(
            user.Id,
            user.UserName!,
            user.Email ?? string.Empty,
            user.FullName,
            user.IsSuperuser,
            roles,
            permissions);
    }
}
