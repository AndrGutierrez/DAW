using System.Text.Json;
using System.Text.Json.Nodes;
using Core.Application.Auditing;
using Core.Application.Livestock;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AuditReader(AppDbContext db) : IAuditReader
{
    private IQueryable<AuditEntry> Entries =>
        from log in db.AuditLogs.AsNoTracking()
        join user in db.Users.AsNoTracking() on log.UserId equals user.Id into users
        from user in users.DefaultIfEmpty()
        join farm in db.Farms.AsNoTracking() on log.FarmId equals farm.Id into farms
        from farm in farms.DefaultIfEmpty()
        select new AuditEntry
        {
            Id = log.Id, OccurredAt = log.OccurredAt, Action = log.Action, EntityName = log.EntityName,
            EntityId = log.EntityId, UserId = log.UserId, FarmId = log.FarmId,
            ActorName = user == null ? null : (user.FullName == "" ? user.UserName : user.FullName + " · " + user.UserName),
            FarmName = farm == null ? null : farm.Name
        };

    public async Task<CarePage<AuditEntry>> PageAsync(AuditQuery q, CancellationToken ct = default)
    {
        if (q.From > q.To) throw new ArgumentException("The start date must be before the end date.");
        var query = Entries;
        if (q.From is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.OccurredAt >= start);
        }
        if (q.To is { } to)
        {
            var end = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(x => x.OccurredAt <= end);
        }
        if (!string.IsNullOrWhiteSpace(q.Action)) query = query.Where(x => x.Action == q.Action.Trim());
        if (!string.IsNullOrWhiteSpace(q.EntityName)) query = query.Where(x => x.EntityName == q.EntityName.Trim());
        if (q.UserId is { } userId) query = query.Where(x => x.UserId == userId);
        if (q.FarmId is { } farmId) query = query.Where(x => x.FarmId == farmId);
        if (!string.IsNullOrWhiteSpace(q.Actor))
        {
            var actor = q.Actor.Trim().ToUpperInvariant();
            query = query.Where(x => x.ActorName != null && x.ActorName.ToUpper().Contains(actor));
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim().ToUpperInvariant();
            query = query.Where(x => x.EntityName.ToUpper().Contains(search) ||
                (x.EntityId != null && x.EntityId.ToUpper().Contains(search)) ||
                x.Action.ToUpper().Contains(search));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        return new(items, total, q.Page, q.PageSize);
    }

    public async Task<AuditDetail> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await Entries.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("The audit entry was not found.");
        var payload = await db.AuditLogs.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.IpAddress, x.OldValues, x.NewValues }).SingleAsync(ct);
        return new(entry, payload.IpAddress, Redact(payload.OldValues), Redact(payload.NewValues));
    }

    public async Task<AuditOptions> OptionsAsync(CancellationToken ct = default) => new(
        await db.AuditLogs.AsNoTracking().Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync(ct),
        await db.AuditLogs.AsNoTracking().Select(x => x.EntityName).Distinct().OrderBy(x => x).ToListAsync(ct),
        await db.Farms.AsNoTracking().OrderBy(x => x.Name).Select(x => new AuditFarm(x.Id, x.Name)).ToListAsync(ct));

    // Defend historical payloads too: never expose secrets even if an older writer stored one.
    private static string? Redact(string? json)
    {
        if (json is null) return null;
        try
        {
            var node = JsonNode.Parse(json);
            Scrub(node);
            return node?.ToJsonString();
        }
        catch (JsonException) { return "{\"Unavailable\":\"Invalid historical snapshot\"}"; }
    }

    private static void Scrub(JsonNode? node)
    {
        if (node is JsonObject obj)
            foreach (var key in obj.Select(x => x.Key).ToArray())
                if (key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("SecurityStamp", StringComparison.OrdinalIgnoreCase)) obj[key] = "[redacted]";
                else Scrub(obj[key]);
        else if (node is JsonArray array)
            foreach (var value in array) Scrub(value);
    }
}
