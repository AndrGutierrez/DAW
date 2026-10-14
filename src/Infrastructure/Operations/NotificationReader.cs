using Core.Application.Livestock;
using Core.Application.Operations;
using Core.Domain.Livestock;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
namespace Infrastructure.Operations;

public sealed class NotificationReader(AppDbContext db, GrowthMonitoringService growth) : INotificationReader
{
    // Each category has a bounded preview; Total includes every matching active condition.
    public async Task<NotificationSnapshot> ReadAsync(IReadOnlyCollection<Guid> farmIds, NotificationVisibility visibility, CancellationToken ct)
    {
        var items = new List<OperationalNotification>(); var total = 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-4));
        if (visibility.Inventory)
        {
            var query = db.FarmInventory.AsNoTracking().Where(i => farmIds.Contains(i.FarmId) && i.Farm.IsActive && i.Product.IsActive && i.Stock <= i.MinStock);
            total += await query.CountAsync(ct);
            var rows = await query.OrderBy(i => i.Stock).ThenBy(i => i.Id).Select(i => new { i.Id, i.FarmId, Farm = i.Farm.Name, Product = i.Product.Name, SKU = i.Product.SKU, i.Stock, i.MinStock, i.Location }).Take(25).ToListAsync(ct);
            items.AddRange(rows.Select(i => new OperationalNotification("stock:" + i.Id, i.FarmId, "stock", i.Stock == 0 ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                "Reponer " + i.Product, i.Farm + " · " + i.Location + " · saldo " + Qty(i.Stock) + "; mínimo " + Qty(i.MinStock), "/inventory?farm=" + i.FarmId + "&state=Low&search=" + Uri.EscapeDataString(i.SKU))));
        }
        if (visibility.Health)
        {
            var query = db.Animals.AsNoTracking().Where(a => farmIds.Contains(a.FarmId) && a.Farm.IsActive && a.Status == AnimalStatus.Active && a.HealthStatus != HealthStatus.Healthy);
            total += await query.CountAsync(ct);
            var rows = await query.OrderByDescending(a => a.HealthStatus).ThenBy(a => a.Id).Select(a => new { a.Id, a.FarmId, a.InternalTag, a.HealthStatus, Farm = a.Farm.Name }).Take(25).ToListAsync(ct);
            items.AddRange(rows.Select(a => new OperationalNotification("health:" + a.Id, a.FarmId, "health", a.HealthStatus == HealthStatus.Critical ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                "Seguimiento sanitario · " + a.InternalTag, a.Farm + " · " + Health(a.HealthStatus), "/animals/" + a.Id + "?tab=clinical")));
            var treatments = db.Set<Treatment>().AsNoTracking().Where(t => farmIds.Contains(t.FarmId) && t.Animal.Status == AnimalStatus.Active && t.Date <= today &&
                (t.WithdrawalEndDate >= today || t.WithdrawalEndDate == null && t.WithdrawalDays == null ||
                 t.WithdrawalDays >= 0 && t.WithdrawalDays <= 3650 && (t.EndDate ?? t.StartDate ?? t.Date).AddDays(t.WithdrawalDays.Value) >= today));
            total += await treatments.CountAsync(ct);
            var withdrawals = await treatments.OrderBy(t => t.Id).Select(t => new { t.Id, t.FarmId, t.AnimalId, t.Animal.InternalTag, t.WithdrawalEndDate, t.WithdrawalDays, t.Date, t.StartDate, t.EndDate }).Take(25).ToListAsync(ct);
            foreach (var t in withdrawals)
            {
                var end = t.WithdrawalEndDate;
                if (t.WithdrawalDays is >= 0 and <= 3650) { var calculated = (t.EndDate ?? t.StartDate ?? t.Date).AddDays(t.WithdrawalDays.Value); if (end == null || calculated > end) end = calculated; }
                items.Add(new("withdrawal:" + t.Id, t.FarmId, "withdrawal", NotificationSeverity.Critical, "Retiro vigente · " + t.InternalTag,
                    end.HasValue ? "Leche y sacrificio bloqueados hasta " + end.Value.ToString("yyyy-MM-dd") + " inclusive." : "Retiro sin fecha concluyente: verificar el tratamiento antes de producir.", "/animals/" + t.AnimalId + "?tab=clinical"));
            }
        }
        if (visibility.Paddocks)
        {
            var query = db.Paddocks.AsNoTracking().Where(p => farmIds.Contains(p.FarmId) && p.IsActive && p.Capacity != null)
                .Select(p => new { p.Id, p.FarmId, p.Name, Farm = p.Farm.Name, p.Capacity, Count = db.Animals.Count(a => a.PaddockId == p.Id && a.FarmId == p.FarmId && a.Status == AnimalStatus.Active) })
                .Where(p => p.Count >= p.Capacity);
            total += await query.CountAsync(ct);
            var rows = await query.OrderByDescending(p => p.Count).ThenBy(p => p.Id).Take(25).ToListAsync(ct);
            items.AddRange(rows.Select(p => new OperationalNotification("capacity:" + p.Id, p.FarmId, "capacity", p.Count > p.Capacity ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                "Revisar capacidad · " + p.Name, p.Farm + " · " + p.Count + " animales / " + p.Capacity + " de capacidad", "/paddocks?farm=" + p.FarmId)));
        }
        if (visibility.Growth && farmIds.Count > 0)
        {
            var alerts = await growth.AlertsAsync(null, new CarePageRequest(1, 25), ct);
            total += alerts.Total;
            items.AddRange(alerts.Items.Where(a => farmIds.Contains(a.FarmId)).Select(a => new OperationalNotification("growth:" + a.AnimalId, a.FarmId, "growth", a.Reason == "weight-loss" ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                "Revisar crecimiento · " + a.Tag, "Ganancia diaria " + Qty(a.DailyGainKg) + " kg/día · " + a.PreviousDate.ToString("yyyy-MM-dd") + " a " + a.CurrentDate.ToString("yyyy-MM-dd"), "/animals/" + a.AnimalId + "?tab=growth")));
        }
        return new(items.OrderByDescending(i => i.Severity).ThenBy(i => i.Kind).ThenBy(i => i.Id).ToArray(), total, DateTime.UtcNow);
    }
    private static string Qty(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    private static string Health(HealthStatus value) => value switch { HealthStatus.Critical => "Estado crítico", HealthStatus.Quarantine => "En cuarentena", HealthStatus.InTreatment => "En tratamiento", _ => "Bajo observación" };
}
