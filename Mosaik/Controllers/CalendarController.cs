using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;

        public CalendarController(MosaikContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        private IReadOnlyList<int> FirmaIds => _currentUser.FirmaIds;

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> Events(int? year, int? month)
        {
            var firmas = FirmaIds;
            if (firmas.Count == 0)
                return Json(Array.Empty<object>());

            var now = DateTime.UtcNow;
            var from = new DateOnly(year ?? now.Year, month ?? now.Month, 1).AddMonths(-1);
            var to   = from.AddMonths(3);

            var events = await _db.ContractEvents
                .AsNoTracking()
                .Include(ev => ev.Contract)
                .Where(ev => firmas.Contains(ev.FirmaId)
                          && ev.EventDate >= from
                          && ev.EventDate <= to)
                .OrderBy(ev => ev.EventDate)
                .ToListAsync();

            var result = events.Select(ev => new
            {
                id    = ev.Id,
                title = ev.Title,
                start = ev.EventDate.ToString("yyyy-MM-dd"),
                backgroundColor = EventColor(ev.EventType, ev.Status),
                borderColor     = EventColor(ev.EventType, ev.Status),
                extendedProps = new
                {
                    type           = ev.EventType.ToString(),
                    typeLabel      = EventTypeLabel(ev.EventType),
                    status         = ev.Status.ToString(),
                    statusLabel    = EventStatusLabel(ev.Status),
                    contractTitle  = ev.Contract?.Title,
                    contractId     = ev.ContractId,
                    reminderDays   = ev.ReminderDays,
                    notes          = ev.Notes
                }
            });

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContractEvent model)
        {
            var firmas = FirmaIds;
            if (firmas.Count == 0) return Forbid();

            model.FirmaId = firmas[0];

            if (model.ContractId.HasValue)
            {
                var ok = await _db.Contracts.AnyAsync(c => c.Id == model.ContractId && firmas.Contains(c.FirmaId));
                if (!ok) return BadRequest();
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _db.ContractEvents.Add(model);
            await _db.SaveChangesAsync();
            return Json(new { success = true, id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var firmas = FirmaIds;
            var ev = await _db.ContractEvents.FirstOrDefaultAsync(e => e.Id == id && firmas.Contains(e.FirmaId));
            if (ev is null) return NotFound();

            ev.Status = EventStatus.Completed;
            ev.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var firmas = FirmaIds;
            var ev = await _db.ContractEvents.FirstOrDefaultAsync(e => e.Id == id && firmas.Contains(e.FirmaId));
            if (ev is null) return NotFound();

            _db.ContractEvents.Remove(ev);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        private static string EventColor(EventType type, EventStatus status)
        {
            if (status == EventStatus.Completed) return "#6b7280";
            if (status == EventStatus.Cancelled) return "#9ca3af";
            return type switch
            {
                EventType.Payment    => "#dc2626",
                EventType.Deadline   => "#dc2626",
                EventType.Tax        => "#d97706",
                EventType.Compliance => "#d97706",
                EventType.Renewal    => "#92400e",
                _                   => "#2563eb"
            };
        }

        private static string EventTypeLabel(EventType type) => type switch
        {
            EventType.Payment    => "Ödeme",
            EventType.Deadline   => "Son Tarih",
            EventType.Renewal    => "Yenileme",
            EventType.Tax        => "Vergi",
            EventType.Compliance => "Uyumluluk",
            EventType.Operation  => "Operasyon",
            _                   => type.ToString()
        };

        private static string EventStatusLabel(EventStatus status) => status switch
        {
            EventStatus.Upcoming  => "Yaklaşan",
            EventStatus.Completed => "Tamamlandı",
            EventStatus.Cancelled => "İptal",
            _                    => status.ToString()
        };
    }
}
