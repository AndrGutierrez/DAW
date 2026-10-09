using Core.Application.Livestock;
using Core.Application.Management;
using FluentValidation;
namespace Core.Application.Security;

public sealed record UserPageQuery(int Page = 1, int PageSize = 12, string? Search = null, bool? IsActive = null);
public sealed record ManagedUser(Guid Id, string Username, string Email, string FullName, bool IsActive, bool IsSuperuser, IReadOnlyList<string> Roles, IReadOnlyList<Guid> FarmIds, IReadOnlyList<string> DirectPermissions, string Version);
public sealed record UserWriteRequest(string Username, string Email, string FullName, string Role, IReadOnlyList<Guid> FarmIds, IReadOnlyList<string> DirectPermissions, bool IsActive = true, string? InitialPassword = null, string? ExpectedVersion = null);
public sealed record PasswordResetRequest(string NewPassword, string ExpectedVersion);
public interface IUserManagementStore
{
    Task<CarePage<ManagedUser>> PageAsync(UserPageQuery q, CancellationToken ct);
    Task<ManagedUser?> FindAsync(Guid id, CancellationToken ct);
    Task<int> CountActiveAdministratorsAsync(CancellationToken ct);
    Task ValidateReferencesAsync(UserWriteRequest q, CancellationToken ct);
    Task<ManagedUser> CreateAsync(UserWriteRequest q, CancellationToken ct);
    Task<ManagedUser> UpdateAsync(Guid id, UserWriteRequest q, CancellationToken ct);
    Task ResetPasswordAsync(Guid id, PasswordResetRequest q, CancellationToken ct);
    Task<T> ExecuteWriteAsync<T>(Func<Task<T>> action, CancellationToken ct);
}
public sealed class UserWriteRequestValidator : AbstractValidator<UserWriteRequest>
{
    public UserWriteRequestValidator()
    {
        RuleFor(q => q.Username).NotEmpty().Length(3, 100).Matches("^[a-zA-Z0-9._@+-]+$");
        RuleFor(q => q.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(q => q.FullName).NotEmpty().MaximumLength(200);
        RuleFor(q => q.Role).Must(r => r is "Admin" or "Administrador" or "Employee" or "SoloLectura" or "Veterinario" or "Capataz" or "Operario");
        RuleFor(q => q.FarmIds).NotNull().Must(ids => ids != null && ids.Count <= 100 && ids.All(i => i != Guid.Empty) && ids.Distinct().Count() == ids.Count);
        RuleFor(q => q.DirectPermissions).NotNull().Must(p => p != null && p.Count <= 100 && p.All(v => !string.IsNullOrWhiteSpace(v)) && p.Distinct().Count() == p.Count);
    }
}
public sealed class PasswordResetRequestValidator : AbstractValidator<PasswordResetRequest>
{
    public PasswordResetRequestValidator()
    {
        RuleFor(q => q.NewPassword).NotEmpty().Length(8,256).Matches("[a-z]").Matches("[A-Z]").Matches("[0-9]").Matches("[^a-zA-Z0-9]");
        RuleFor(q => q.ExpectedVersion).NotEmpty().MaximumLength(100);
    }
}
public sealed class UserManagementService(IUserManagementStore store, ICurrentUser actor)
{
    public async Task<CarePage<ManagedUser>> PageAsync(UserPageQuery q, CancellationToken ct = default)
    {
        await new CarePageRequestValidator().ValidateAndThrowAsync(new(q.Page, q.PageSize), ct);
        if (q.Search?.Length > 100) throw new ArgumentException("Search must be at most 100 characters.");
        return await store.PageAsync(q, ct);
    }
    public Task<ManagedUser> CreateAsync(UserWriteRequest q, CancellationToken ct = default) => store.ExecuteWriteAsync(async () =>
    {
        q = Normalize(q); await new UserWriteRequestValidator().ValidateAndThrowAsync(q, ct);
        await new RegisterRequestValidator().ValidateAndThrowAsync(new(q.Username,q.Email,q.InitialPassword!,q.FullName),ct);
        await store.ValidateReferencesAsync(q,ct); return await store.CreateAsync(q,ct);
    },ct);
    public Task<ManagedUser> UpdateAsync(Guid id, UserWriteRequest q, CancellationToken ct = default) => store.ExecuteWriteAsync(async () =>
    {
        q = Normalize(q); await new UserWriteRequestValidator().ValidateAndThrowAsync(q,ct);
        if (q.InitialPassword != null) throw new ArgumentException("Use the password reset operation.");
        var current = await FindEditableAsync(id,q.ExpectedVersion,ct);
        if (id == actor.UserId && (!q.IsActive || !IsAdmin(q.Role) || current.Roles.Count != 1 || current.Roles[0] != q.Role || !current.DirectPermissions.Order().SequenceEqual(q.DirectPermissions.Order())))
            throw new ConflictException("You cannot deactivate your account or change your own access.");
        if (current.IsActive && current.Roles.Any(IsAdmin) && (!q.IsActive || !IsAdmin(q.Role)) && await store.CountActiveAdministratorsAsync(ct) <= 1)
            throw new ConflictException("Keep at least one active administrator.");
        await store.ValidateReferencesAsync(q,ct); return await store.UpdateAsync(id,q,ct);
    },ct);
    public Task<bool> ResetPasswordAsync(Guid id, PasswordResetRequest q, CancellationToken ct = default) => store.ExecuteWriteAsync(async () =>
    {
        await new PasswordResetRequestValidator().ValidateAndThrowAsync(q,ct);
        if (id == actor.UserId) throw new ConflictException("Use a separate administrator to reset your password.");
        await FindEditableAsync(id,q.ExpectedVersion,ct); await store.ResetPasswordAsync(id,q,ct); return true;
    },ct);
    private async Task<ManagedUser> FindEditableAsync(Guid id,string? version,CancellationToken ct)
    {
        var user = await store.FindAsync(id,ct) ?? throw new KeyNotFoundException("The requested user was not found.");
        if (user.IsSuperuser) throw new ConflictException("The system superuser is protected.");
        if (string.IsNullOrWhiteSpace(version) || user.Version != version) throw new ConflictException("The user changed. Reload before saving.");
        return user;
    }
    private static bool IsAdmin(string role) => role is "Admin" or "Administrador";
    private static UserWriteRequest Normalize(UserWriteRequest q) => q with { Username = q.Username?.Trim()!, Email=q.Email?.Trim()!, FullName=q.FullName?.Trim()! };
}
