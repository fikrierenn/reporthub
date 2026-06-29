using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
        private readonly Mosaik.Core.Lookup.ILookupService _lookup;

        public CalendarController(MosaikContext db, ICurrentUserService currentUser, Mosaik.Core.Lookup.ILookupService lookup)
        {
            _db = db;
            _currentUser = currentUser;
            _lookup = lookup;
        }

        private IReadOnlyList<int> FirmaIds => _currentUser.FirmaIds;

        public async Task<IActionResult> Index()
        {
            ViewBag.EventTypes = await _lookup.GetValuesAsync("eventType");
            return View();
        }

        // ADR-016: vw_CalendarUnified birleşik takvim verisi.
        // sources parametresi boş = hepsi; virgülle ayrılmış SourceType filtresi.
        [HttpGet]
        public async Task<IActionResult> Events(int? year, int? month, string? sources)
        {
            var firmas = FirmaIds;
            if (firmas.Count == 0)
                return Json(Array.Empty<object>());

            var now  = DateTime.UtcNow;
            var from = new DateOnly(year ?? now.Year, month ?? now.Month, 1).AddMonths(-1);
            var to   = from.AddMonths(3);

            // FirmaIds server-side claim'den gelir (int listesi), STRING_SPLIT ile parametre.
            var firmasCsv    = string.Join(",", firmas);
            var sourcesParam = string.IsNullOrWhiteSpace(sources) ? null : sources;

            var sql = """
                SELECT EventId, FirmaId, Title, CAST(EventDate AS NVARCHAR(10)) AS EventDate,
                       SourceType, SourceId, Notes, ReminderDays, HolidayType
                FROM vw_CalendarUnified
                WHERE (FirmaId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@firmas, ',')) OR FirmaId = 0)
                  AND EventDate >= @from
                  AND EventDate <= @to
                  AND (@sources IS NULL OR SourceType IN (SELECT value FROM STRING_SPLIT(@sources, ',')))
                ORDER BY EventDate
                """;

            var rows = await _db.Database
                .SqlQueryRaw<CalendarUnifiedEventDto>(
                    sql,
                    new SqlParameter("@firmas",  firmasCsv),
                    new SqlParameter("@from",    from.ToString("yyyy-MM-dd")),
                    new SqlParameter("@to",      to.ToString("yyyy-MM-dd")),
                    new SqlParameter("@sources", (object?)sourcesParam ?? DBNull.Value))
                .ToListAsync();

            var result = rows.Select(r => new
            {
                id              = $"{r.SourceType}_{r.EventId}",
                title           = r.Title,
                start           = r.EventDate,
                backgroundColor = SourceColor(r.SourceType),
                borderColor     = SourceColor(r.SourceType),
                extendedProps   = new
                {
                    sourceType   = r.SourceType,
                    sourceId     = r.SourceId,
                    reminderDays = r.ReminderDays,
                    notes        = r.Notes,
                    holidayType  = r.HolidayType
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

        private static string SourceColor(string sourceType) => sourceType switch
        {
            "Holiday"       => "#2d9e4a",
            "ImportantDate" => "#e07b00",
            "Obligation"    => "#e63946",
            _               => "#5b6bff"  // ContractEvent
        };
    }

    // EF Core SqlQueryRaw için keyless DTO (vw_CalendarUnified şemasına eşleşir)
    public class CalendarUnifiedEventDto
    {
        public int     EventId      { get; set; }
        public int     FirmaId      { get; set; }
        public string  Title        { get; set; } = "";
        public string  EventDate    { get; set; } = "";
        public string  SourceType   { get; set; } = "";
        public int     SourceId     { get; set; }
        public string? Notes        { get; set; }
        public int     ReminderDays { get; set; }
        public string? HolidayType  { get; set; }
    }
}
