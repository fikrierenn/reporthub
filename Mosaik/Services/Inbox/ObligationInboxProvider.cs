using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Module.Capabilities;
using Mosaik.Models;

namespace Mosaik.Services.Inbox;

// M2 — vadesi yaklaşan/geçmiş yükümlülükleri inbox kalemine çevirir.
// Bekleyen + Gecikmiş ve vade <= bugün+30g (veya zaten geçmiş) olanlar.
public sealed class ObligationInboxProvider(MosaikContext db) : IInboxProvider
{
    private readonly MosaikContext _db = db;

    public string ProviderKey => "obligation";

    public async Task<IReadOnlyList<InboxItem>> GetItemsForUserAsync(
        int userId,
        ISet<string> roles,
        IReadOnlyList<int> firmaIds,
        CancellationToken ct)
    {
        if (firmaIds.Count == 0) return Array.Empty<InboxItem>();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var horizon = today.AddDays(30);

        var rows = await _db.ContractObligations.AsNoTracking()
            .Where(o => firmaIds.Contains(o.FirmaId)
                     && (o.Status == ObligationStatus.Pending || o.Status == ObligationStatus.Overdue)
                     && o.DueDate <= horizon)
            .OrderBy(o => o.DueDate)
            .Select(o => new { o.Id, o.Title, o.DueDate, o.Status })
            .ToListAsync(ct);

        if (rows.Count == 0) return Array.Empty<InboxItem>();

        return rows.Select(o =>
        {
            var dueAt = o.DueDate.ToDateTime(TimeOnly.MinValue);
            var isOverdue = o.DueDate < today;
            var daysLeft = o.DueDate.DayNumber - today.DayNumber;
            var subtitle = isOverdue
                ? $"{-daysLeft} gün gecikti · {o.DueDate:dd.MM.yyyy}"
                : $"Vade: {o.DueDate:dd.MM.yyyy} ({daysLeft} gün)";

            return new InboxItem
            {
                ProviderKey = "obligation",
                Category = "due",
                Title = o.Title,
                Subtitle = subtitle,
                Url = "/Obligations",
                CreatedAt = dueAt,
                DueAt = dueAt,
                IsOverdue = isOverdue
            };
        }).ToList();
    }
}
