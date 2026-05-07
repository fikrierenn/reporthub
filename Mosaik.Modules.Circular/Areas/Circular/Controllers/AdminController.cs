using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Logging;
using Mosaik.Modules.Circular.Models;
using Mosaik.Modules.Circular.Services;

namespace Mosaik.Modules.Circular.Areas.Circular.Controllers
{
    // Plan 17 Faz D — Admin yönetim ekranları:
    //  - CompileNow: cron'u beklemeden manuel derleme (test ve operasyonel kontrol)
    //  - ReadingReport/{id}: bir tamimi kim okudu/okumadı (AuditLog 'circular_read')
    //
    // Not: Modül Mosaik.Models.User / AuditLog tiplerine erişemez (circular dep).
    //   Raw SQL projection ile okuyoruz (Database.SqlQueryRaw).
    [Area("Circular")]
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly DbContext _db;
        private readonly CompileCircularJob _compile;
        private readonly CircularSummaryService _summary;
        private readonly IAuditLog _audit;

        public AdminController(DbContext db, CompileCircularJob compile, CircularSummaryService summary, IAuditLog audit)
        {
            _db = db;
            _compile = compile;
            _summary = summary;
            _audit = audit;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateSummary(int id)
        {
            var result = await _summary.GenerateAsync(id);
            TempData["Message"] = result.IsSuccess
                ? $"AI özet yeniden üretildi (model={result.ModelUsed})."
                : $"AI özet üretilemedi: {result.Error}";
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return RedirectToAction("Details", "Circular", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompileNow(DateTime? date)
        {
            var result = await _compile.ExecuteAsync(date);
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.Success ? "success" : "warn";

            if (result.CircularId.HasValue)
            {
                return RedirectToAction("Details", "Circular", new { id = result.CircularId.Value });
            }
            return RedirectToAction("Index", "Circular");
        }

        public async Task<IActionResult> Dashboard()
        {
            var sinceDate = DateTime.UtcNow.Date.AddDays(-30);
            var today = DateTime.UtcNow.Date;

            // Son 30 gün yayınlanan circulars
            var publishedCirculars = await _db.Set<Models.Circular>()
                .AsNoTracking()
                .Where(c => c.CircularDate >= sinceDate)
                .Select(c => new { c.Id, c.CircularDate })
                .ToListAsync();

            // Toplam aktif kullanıcı sayısı
            var activeUserCount = await _db.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Users WHERE IsActive = 1")
                .FirstOrDefaultAsync();

            // Son 30 gün distinct read count: kaç farklı kullanıcı en az 1 tamim okudu
            var distinctReaders = await _db.Database
                .SqlQuery<int>($@"
                    SELECT COUNT(DISTINCT a.Username) AS [Value]
                    FROM AuditLog a
                    WHERE a.EventType = 'circular_read'
                      AND a.CreatedAt >= {sinceDate}")
                .FirstOrDefaultAsync();

            var seenRate = activeUserCount > 0 ? (distinctReaders * 100 / activeUserCount) : 0;

            // Son 30 gün departman katılımı (DailyBlocks → Department, count, distinct gün)
            var deptParticipation = await _db.Set<DailyBlock>()
                .AsNoTracking()
                .Where(b => b.IsActive && b.BlockDate >= sinceDate)
                .GroupBy(b => b.Department)
                .Select(g => new DeptParticipationRow
                {
                    Department = g.Key,
                    BlockCount = g.Count(),
                    DayCount = g.Select(x => x.BlockDate).Distinct().Count()
                })
                .OrderByDescending(x => x.BlockCount)
                .Take(10)
                .ToListAsync();

            // Bugün blok yazmamış departmanlar = son 7 günde yazıp bugün yazmamış
            var todayDepts = await _db.Set<DailyBlock>()
                .AsNoTracking()
                .Where(b => b.IsActive && b.BlockDate == today)
                .Select(b => b.Department)
                .Distinct()
                .ToListAsync();

            var recentDepts = await _db.Set<DailyBlock>()
                .AsNoTracking()
                .Where(b => b.IsActive && b.BlockDate >= today.AddDays(-7) && b.BlockDate < today)
                .Select(b => b.Department)
                .Distinct()
                .ToListAsync();

            var missingDepts = recentDepts.Except(todayDepts).ToList();

            // Son 30 gün günlük block trendi
            var trend = await _db.Set<DailyBlock>()
                .AsNoTracking()
                .Where(b => b.IsActive && b.BlockDate >= sinceDate)
                .GroupBy(b => b.BlockDate)
                .Select(g => new TrendRow { Date = g.Key, BlockCount = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var vm = new CircularDashboardViewModel
            {
                PublishedCircularCount = publishedCirculars.Count,
                ActiveUserCount = activeUserCount,
                DistinctReaderCount = distinctReaders,
                SeenRate = seenRate,
                DepartmentParticipation = deptParticipation,
                MissingDepartmentsToday = missingDepts,
                Trend = trend
            };
            return View(vm);
        }

        public async Task<IActionResult> ReadingReport(int id)
        {
            var circular = await _db.Set<Models.Circular>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
            if (circular == null) return NotFound();

            // Tüm aktif kullanıcılar + ilk circular_read tarihi (LEFT JOIN AuditLog)
            // Modül User/AuditLog tipine doğrudan erişemediği için raw SQL
            var rows = await _db.Database
                .SqlQuery<ReadingReportRow>($@"
                    SELECT
                        u.UserId    AS UserId,
                        u.Username  AS Username,
                        u.FullName  AS FullName,
                        u.Email     AS Email,
                        CASE WHEN MIN(a.CreatedAt) IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS HasRead,
                        MIN(a.CreatedAt) AS FirstReadAt
                    FROM Users u
                    LEFT JOIN AuditLog a
                        ON a.Username = u.Username
                       AND a.EventType = 'circular_read'
                       AND a.TargetType = 'circular'
                       AND a.TargetKey = {id.ToString()}
                    WHERE u.IsActive = 1
                    GROUP BY u.UserId, u.Username, u.FullName, u.Email
                    ORDER BY u.FullName")
                .ToListAsync();

            ViewBag.Circular = circular;
            ViewBag.TotalCount = rows.Count;
            ViewBag.ReadCount = rows.Count(r => r.HasRead);
            return View(rows);
        }

        // GET /Circular/Admin/ReadingReportXlsx/{id} — okuma raporu Excel indir
        public async Task<IActionResult> ReadingReportXlsx(int id)
        {
            var circular = await _db.Set<Models.Circular>().AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
            if (circular == null) return NotFound();

            var rows = await _db.Database
                .SqlQuery<ReadingReportRow>($@"
                    SELECT
                        u.UserId AS UserId, u.Username AS Username, u.FullName AS FullName, u.Email AS Email,
                        CASE WHEN MIN(a.CreatedAt) IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS HasRead,
                        MIN(a.CreatedAt) AS FirstReadAt
                    FROM Users u
                    LEFT JOIN AuditLog a
                        ON a.Username = u.Username
                       AND a.EventType = 'circular_read'
                       AND a.TargetType = 'circular'
                       AND a.TargetKey = {id.ToString()}
                    WHERE u.IsActive = 1
                    GROUP BY u.UserId, u.Username, u.FullName, u.Email
                    ORDER BY u.FullName")
                .ToListAsync();

            using var wb = new XLWorkbook();
            var sheet = wb.Worksheets.Add("Okuma Raporu");

            // Header
            sheet.Cell(1, 1).Value = $"{circular.CircularNumber} — Okuma Raporu";
            sheet.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            sheet.Cell(2, 1).Value = "Tamim:";
            sheet.Cell(2, 2).Value = circular.Title;
            sheet.Cell(3, 1).Value = "Tarih:";
            sheet.Cell(3, 2).Value = circular.CircularDate.ToString("dd.MM.yyyy");
            sheet.Cell(4, 1).Value = "Toplam:";
            sheet.Cell(4, 2).Value = $"{rows.Count} kullanıcı · {rows.Count(r => r.HasRead)} okudu · {rows.Count(r => !r.HasRead)} okumadı";
            sheet.Range(2, 1, 4, 1).Style.Font.Bold = true;

            // Tablo
            sheet.Cell(6, 1).Value = "Kullanıcı";
            sheet.Cell(6, 2).Value = "Ad Soyad";
            sheet.Cell(6, 3).Value = "E-posta";
            sheet.Cell(6, 4).Value = "Durum";
            sheet.Cell(6, 5).Value = "İlk Okuma";
            sheet.Range(6, 1, 6, 5).Style.Font.Bold = true;
            sheet.Range(6, 1, 6, 5).Style.Fill.BackgroundColor = XLColor.LightGray;

            int r = 7;
            foreach (var row in rows.OrderBy(x => x.HasRead).ThenBy(x => x.FullName))
            {
                sheet.Cell(r, 1).Value = row.Username;
                sheet.Cell(r, 2).Value = row.FullName;
                sheet.Cell(r, 3).Value = row.Email ?? "";
                sheet.Cell(r, 4).Value = row.HasRead ? "Okudu" : "Okumadı";
                sheet.Cell(r, 5).Value = row.FirstReadAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "";
                if (!row.HasRead)
                    sheet.Range(r, 1, r, 5).Style.Fill.BackgroundColor = XLColor.LightSalmon;
                r++;
            }

            sheet.Column(1).Width = 18;
            sheet.Column(2).Width = 28;
            sheet.Column(3).Width = 28;
            sheet.Column(4).Width = 12;
            sheet.Column(5).Width = 18;
            sheet.SheetView.FreezeRows(6);

            await _audit.LogAsync(
                eventType: "reading_report_export",
                targetType: "circular",
                targetKey: id.ToString());

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{circular.CircularNumber}-OkumaRaporu.xlsx");
        }
    }

    public class CircularDashboardViewModel
    {
        public int PublishedCircularCount { get; set; }
        public int ActiveUserCount { get; set; }
        public int DistinctReaderCount { get; set; }
        public int SeenRate { get; set; }
        public List<DeptParticipationRow> DepartmentParticipation { get; set; } = new();
        public List<string> MissingDepartmentsToday { get; set; } = new();
        public List<TrendRow> Trend { get; set; } = new();
    }

    public class DeptParticipationRow
    {
        public string Department { get; set; } = "";
        public int BlockCount { get; set; }
        public int DayCount { get; set; }
    }

    public class TrendRow
    {
        public DateTime Date { get; set; }
        public int BlockCount { get; set; }
    }

    public class ReadingReportRow
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Email { get; set; }
        public bool HasRead { get; set; }
        public DateTime? FirstReadAt { get; set; }
    }
}
