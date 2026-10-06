using System.Linq.Expressions;
using System.Text.Json;
using Core.Application.Management;
using Core.Application.Security;
using Core.Domain.Common;
using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence;

public sealed class ManagementRepository(AppDbContext db, ICurrentUser user) : IManagementRepository
{
    private async Task<IAsyncDisposable> BeginWriteAsync(CancellationToken ct) => db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct) : new NoTransaction();
    public async Task<TResult> ExecuteWriteAsync<TResult>(Func<Task<TResult>> operation, CancellationToken ct = default)
    {
        await using var scope = await BeginWriteAsync(ct);
        try
        {
            var result = await operation();
            if (db.Database.CurrentTransaction is { } transaction) await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (PostgresCause(ex)?.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new ConflictException("A concurrent operation changed this data. Refresh and retry the request.");
        }
    }

    private static PostgresException? PostgresCause(Exception error)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
            if (current is PostgresException postgres) return postgres;
        return null;
    }
    private sealed class NoTransaction : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        where T : BaseEntity
    {
        var query = db.Set<T>().AsNoTracking();
        if (predicate != null)
            query = query.Where(predicate);
        return await query.OrderBy(e => e.CreatedAt).ThenBy(e => e.Id).ToListAsync(ct);
    }

    public Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default) where T : BaseEntity => db.Set<T>().AsNoTracking().CountAsync(predicate, ct);
    public async Task<IReadOnlyList<T>> PageAsync<T, TOrder>(Expression<Func<T, bool>> predicate, Expression<Func<T, TOrder>> order, int skip, int take, CancellationToken ct = default) where T : BaseEntity =>
        await db.Set<T>().AsNoTracking().Where(predicate).OrderByDescending(order).ThenByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip(skip).Take(take).ToListAsync(ct);

    public Task<T?> GetAsync<T>(Guid id, bool tracking = false, CancellationToken ct = default)
        where T : BaseEntity => (tracking ? db.Set<T>() : db.Set<T>().AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : BaseEntity => db.Set<T>().AsNoTracking().AnyAsync(predicate, ct);
    public void Add<T>(T entity)
        where T : BaseEntity => db.Add(entity);
    public void Remove<T>(T entity)
        where T : BaseEntity => db.Remove(entity);
    public async Task SaveAsync(CancellationToken ct = default)
    {
        db.ChangeTracker.DetectChanges();
        var changes = db.ChangeTracker.Entries<BaseEntity>().Where(e => e.Entity is not AuditLog && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        foreach (var entry in changes)
        {
            var old = entry.State == EntityState.Added ? null : entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
            var current = entry.State == EntityState.Deleted ? null : entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
            db.AuditLogs.Add(new AuditLog { UserId = user.UserId, FarmId = entry.Metadata.FindProperty("FarmId") != null ? (Guid?)entry.Property("FarmId").CurrentValue : null, Action = entry.State.ToString(), EntityName = entry.Metadata.ClrType.Name, EntityId = entry.Entity.Id.ToString(), OldValues = old == null ? null : JsonSerializer.Serialize(old), NewValues = current == null ? null : JsonSerializer.Serialize(current) });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            throw new ConflictException("A concurrent operation changed this data. Refresh and retry the request.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected })
        {
            throw new ConflictException("A concurrent operation changed this data. Refresh and retry the request.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("The record conflicts with an existing unique value.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ConflictException("The record has dependent data or references a missing record. Resolve the dependencies first.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.CheckViolation })
        {
            throw new ArgumentException("The values violate a database integrity rule.");
        }
    }
}
