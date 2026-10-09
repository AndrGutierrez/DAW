using Core.Application.Livestock;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Npgsql;
namespace Infrastructure.Security;

public sealed class UserManagementStore(AppDbContext db, UserManager<ApplicationUser> manager, ICurrentUser actor, IManagementRepository writes) : IUserManagementStore
{
    public async Task<T> ExecuteWriteAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try { return await writes.ExecuteWriteAsync(action, ct); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: "23505" })
        { throw new ConflictException("Username or email already exists. Reload and retry."); }
    }
    public async Task<CarePage<ManagedUser>> PageAsync(UserPageQuery q,CancellationToken ct)
    {
        var users=db.Users.AsNoTracking();
        if(q.IsActive.HasValue) users=users.Where(u=>u.IsActive==q.IsActive.Value);
        if(!string.IsNullOrWhiteSpace(q.Search)) { var s=q.Search.Trim().ToUpperInvariant(); users=users.Where(u=>u.NormalizedUserName!.Contains(s)||u.NormalizedEmail!.Contains(s)||u.FullName.ToUpper().Contains(s)); }
        var count=await users.CountAsync(ct); var page=await users.OrderBy(u=>u.UserName).ThenBy(u=>u.Id).Skip((q.Page-1)*q.PageSize).Take(q.PageSize).ToListAsync(ct);
        var result=new List<ManagedUser>(); foreach(var u in page)result.Add(await ReadAsync(u,ct));
        return new(result,count,q.Page,q.PageSize);
    }
    public async Task<ManagedUser?> FindAsync(Guid id,CancellationToken ct)
    { var u=await db.Users.AsNoTracking().FirstOrDefaultAsync(u=>u.Id==id,ct);return u==null?null:await ReadAsync(u,ct); }
    public Task<int> CountActiveAdministratorsAsync(CancellationToken ct) => db.Users.CountAsync(u=>u.IsActive && (u.IsSuperuser || db.UserRoles.Any(ur=>ur.UserId==u.Id && db.Roles.Any(r=>r.Id==ur.RoleId && (r.Name=="Admin"||r.Name=="Administrador")))),ct);
    public async Task ValidateReferencesAsync(UserWriteRequest q,CancellationToken ct)
    {
        if(!await db.Roles.AnyAsync(r=>r.Name==q.Role,ct))throw new ArgumentException("The selected role was not found.");
        if(await db.Farms.CountAsync(f=>q.FarmIds.Contains(f.Id),ct)!=q.FarmIds.Count)throw new ArgumentException("One or more selected farms were not found.");
        if(await db.Permissions.CountAsync(p=>q.DirectPermissions.Contains(p.Name),ct)!=q.DirectPermissions.Count)throw new ArgumentException("One or more permissions were not found.");
    }
    public async Task<ManagedUser> CreateAsync(UserWriteRequest q,CancellationToken ct)
    {
        var u=new ApplicationUser { Id=Guid.NewGuid(),UserName=q.Username,Email=q.Email,FullName=q.FullName,IsActive=q.IsActive,EmailConfirmed=true };
        Check(await manager.CreateAsync(u,q.InitialPassword!)); await AssignAsync(u,q,ct); await AuditAsync(u.Id,"UserCreated",null,await ReadAsync(u,ct),ct); return await ReadAsync(u,ct);
    }
    public async Task<ManagedUser> UpdateAsync(Guid id,UserWriteRequest q,CancellationToken ct)
    {
        var u=await db.Users.FirstOrDefaultAsync(u=>u.Id==id,ct)??throw new KeyNotFoundException(); var before=await ReadAsync(u,ct);
        if(u.ConcurrencyStamp!=q.ExpectedVersion)throw new ConflictException("The user changed. Reload before saving.");
        u.UserName=q.Username;u.Email=q.Email;u.FullName=q.FullName;u.IsActive=q.IsActive;
        Check(await manager.UpdateAsync(u)); await AssignAsync(u,q,ct); Check(await manager.UpdateSecurityStampAsync(u)); await RevokeAsync(id,ct);
        var after=await ReadAsync(u,ct);await AuditAsync(id,"UserUpdated",before,after,ct); return after;
    }
    public async Task ResetPasswordAsync(Guid id,PasswordResetRequest q,CancellationToken ct)
    {
        var u=await db.Users.FirstAsync(u=>u.Id==id,ct);
        if(u.ConcurrencyStamp!=q.ExpectedVersion)throw new ConflictException("The user changed. Reload before saving.");
        var token=await manager.GeneratePasswordResetTokenAsync(u); Check(await manager.ResetPasswordAsync(u,token,q.NewPassword)); await RevokeAsync(id,ct);
        await AuditAsync(id,"PasswordReset",null,new { SessionsRevoked=true },ct);
    }
    private async Task AssignAsync(ApplicationUser u,UserWriteRequest q,CancellationToken ct)
    {
        var selectedRole = await db.Roles.SingleAsync(r => r.Name == q.Role, ct);
        var roles = await db.UserRoles.IgnoreQueryFilters().Where(r => r.UserId == u.Id).ToListAsync(ct);
        foreach (var role in roles)
            if (role.RoleId == selectedRole.Id) db.RestoreLink(role);
            else if (!db.Entry(role).Property<bool>("IsDeleted").CurrentValue) db.UserRoles.Remove(role);
        if (roles.All(r => r.RoleId != selectedRole.Id)) db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = u.Id, RoleId = selectedRole.Id });
        var memberships = await db.UserFarms.IgnoreQueryFilters().Where(f => f.UserId == u.Id).ToListAsync(ct);
        foreach (var membership in memberships)
            if (q.FarmIds.Contains(membership.FarmId)) { membership.Restore(); membership.IsDefault = q.FarmIds.FirstOrDefault() == membership.FarmId; }
            else membership.MarkDeleted(actor.UserId);
        foreach (var farmId in q.FarmIds.Where(id => memberships.All(m => m.FarmId != id)))
            db.UserFarms.Add(new UserFarm { UserId = u.Id, FarmId = farmId, IsDefault = q.FarmIds.FirstOrDefault() == farmId });
        var selectedPermissions = await db.Permissions.Where(p => q.DirectPermissions.Contains(p.Name)).Select(p => p.Id).ToListAsync(ct);
        var permissions = await db.UserPermissions.IgnoreQueryFilters().Where(p => p.UserId == u.Id).ToListAsync(ct);
        foreach (var permission in permissions)
            if (selectedPermissions.Contains(permission.PermissionId)) db.RestoreLink(permission);
            else if (!db.Entry(permission).Property<bool>("IsDeleted").CurrentValue) db.UserPermissions.Remove(permission);
        foreach (var permissionId in selectedPermissions.Where(id => permissions.All(p => p.PermissionId != id)))
            db.UserPermissions.Add(new UserPermission { UserId = u.Id, PermissionId = permissionId });
        await db.SaveChangesAsync(ct);
    }
    private async Task RevokeAsync(Guid id,CancellationToken ct)
    { foreach(var token in await db.RefreshTokens.Where(t=>t.UserId==id&&t.RevokedAt==null).ToListAsync(ct))token.RevokedAt=DateTime.UtcNow;await db.SaveChangesAsync(ct); }
    private async Task<ManagedUser> ReadAsync(ApplicationUser u,CancellationToken ct) => new(u.Id,u.UserName!,u.Email??"",u.FullName,u.IsActive,u.IsSuperuser,(await manager.GetRolesAsync(u)).Order().ToList(),await db.UserFarms.AsNoTracking().Where(f=>f.UserId==u.Id).OrderBy(f=>f.FarmId).Select(f=>f.FarmId).ToListAsync(ct),await db.UserPermissions.AsNoTracking().Where(p=>p.UserId==u.Id).OrderBy(p=>p.Permission.Name).Select(p=>p.Permission.Name).ToListAsync(ct),u.ConcurrencyStamp??"");
    private async Task AuditAsync(Guid id,string action,object? before,object after,CancellationToken ct)
    { db.AuditLogs.Add(new AuditLog {UserId=actor.UserId,IpAddress=actor.IpAddress,EntityName="ApplicationUser",EntityId=id.ToString(),Action=action,OldValues=before==null?null:JsonSerializer.Serialize(before),NewValues=JsonSerializer.Serialize(after)});await db.SaveChangesAsync(ct); }
    private static void Check(IdentityResult result)
    { if(result.Succeeded)return;if(result.Errors.Any(e=>e.Code.StartsWith("Duplicate")||e.Code=="ConcurrencyFailure"))throw new ConflictException("Username or email already exists, or the account changed. Reload and retry.");throw new ArgumentException(string.Join(" ",result.Errors.Select(e=>e.Description))); }
}
