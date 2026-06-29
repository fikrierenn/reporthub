using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
{
    // Plan 54 M4 — Dashboard→Alert eşik kuralları admin CRUD.
    // Kurallar EscalationSweeperJob (saatlik) tarafından GLOBAL değerlendirilir.
    [Authorize(Roles = "admin")]
    public class EscalationController : Controller
    {
        private static readonly string[] Aggregations = { "first", "count", "sum", "avg", "min", "max" };
        private static readonly string[] Operators = { "gt", "gte", "lt", "lte", "eq" };

        private readonly MosaikContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly AuditLogService _auditLog;
        private readonly ILogger<EscalationController> _logger;

        public EscalationController(
            MosaikContext db,
            ICurrentUserService currentUser,
            AuditLogService auditLog,
            ILogger<EscalationController> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _auditLog = auditLog;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rules = await _db.EscalationRules.AsNoTracking()
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.Name)
                .ToListAsync();

            var reportTitles = await _db.ReportCatalog.AsNoTracking()
                .ToDictionaryAsync(r => r.ReportId, r => r.Title);

            return View(new EscalationRuleListViewModel
            {
                Rules = rules,
                ReportTitles = reportTitles
            });
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new EscalationRuleFormViewModel();
            await PopulateOptionsAsync(vm);
            return View("Edit", vm);
        }

        [HttpGet]
        [Route("Escalation/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var rule = await _db.EscalationRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (rule == null)
                return NotFound();

            var vm = new EscalationRuleFormViewModel
            {
                Id = rule.Id,
                Name = rule.Name,
                ReportId = rule.ReportId,
                ResultSet = rule.ResultSet,
                Column = rule.Column,
                Aggregation = rule.Aggregation,
                Operator = rule.Operator,
                Threshold = rule.Threshold,
                NotifyUserIdsList = ParseUserIds(rule.NotifyUserIds),
                IsActive = rule.IsActive
            };
            await PopulateOptionsAsync(vm);
            return View("Edit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(EscalationRuleFormViewModel form)
        {
            if (!Aggregations.Contains(form.Aggregation))
                ModelState.AddModelError(nameof(form.Aggregation), "Geçersiz toplama tipi.");
            if (!Operators.Contains(form.Operator))
                ModelState.AddModelError(nameof(form.Operator), "Geçersiz karşılaştırma.");
            if (form.NotifyUserIdsList.Count == 0)
                ModelState.AddModelError(nameof(form.NotifyUserIdsList), "En az bir bildirim alıcısı seçin.");
            // count dışında kolon zorunlu (count satır sayar, kolona bakmaz).
            if (form.Aggregation != "count" && string.IsNullOrWhiteSpace(form.Column))
                ModelState.AddModelError(nameof(form.Column), "Kolon adı zorunlu.");

            // Rapor gerçekten var mı + aktif mi.
            var reportExists = await _db.ReportCatalog.AsNoTracking()
                .AnyAsync(r => r.ReportId == form.ReportId && r.IsActive);
            if (!reportExists)
                ModelState.AddModelError(nameof(form.ReportId), "Seçilen rapor bulunamadı veya pasif.");

            // Seçilen alıcılar gerçekten aktif kullanıcı mı (UI dışı manipülasyona karşı).
            var validUserIds = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive && form.NotifyUserIdsList.Contains(u.UserId))
                .Select(u => u.UserId)
                .ToListAsync();
            if (validUserIds.Count != form.NotifyUserIdsList.Count)
                ModelState.AddModelError(nameof(form.NotifyUserIdsList), "Geçersiz veya pasif kullanıcı seçimi.");

            if (!ModelState.IsValid)
            {
                await PopulateOptionsAsync(form);
                return View("Edit", form);
            }

            var notifyCsv = string.Join(",", validUserIds);
            var isNew = form.Id == 0;

            if (isNew)
            {
                var rule = new EscalationRule
                {
                    Name = form.Name.Trim(),
                    ReportId = form.ReportId,
                    ResultSet = form.ResultSet,
                    Column = form.Column?.Trim() ?? string.Empty,
                    Aggregation = form.Aggregation,
                    Operator = form.Operator,
                    Threshold = form.Threshold,
                    NotifyUserIds = notifyCsv,
                    IsActive = form.IsActive,
                    CreatedBy = _currentUser.UserId ?? 0,
                    CreatedAt = DateTime.UtcNow
                };
                _db.EscalationRules.Add(rule);
                await _db.SaveChangesAsync();
                await AuditAsync("escalation_rule_create", rule.Id, rule.Name, Snapshot(rule));
                TempData["Message"] = "Eşik kuralı oluşturuldu.";
                TempData["MessageType"] = "success";
            }
            else
            {
                var rule = await _db.EscalationRules.FirstOrDefaultAsync(r => r.Id == form.Id);
                if (rule == null)
                    return NotFound();

                rule.Name = form.Name.Trim();
                rule.ReportId = form.ReportId;
                rule.ResultSet = form.ResultSet;
                rule.Column = form.Column?.Trim() ?? string.Empty;
                rule.Aggregation = form.Aggregation;
                rule.Operator = form.Operator;
                rule.Threshold = form.Threshold;
                rule.NotifyUserIds = notifyCsv;
                rule.IsActive = form.IsActive;
                await _db.SaveChangesAsync();
                await AuditAsync("escalation_rule_update", rule.Id, rule.Name, Snapshot(rule));
                TempData["Message"] = "Eşik kuralı güncellendi.";
                TempData["MessageType"] = "success";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Escalation/Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _db.EscalationRules.FirstOrDefaultAsync(r => r.Id == id);
            if (rule == null)
                return NotFound();

            var name = rule.Name;
            _db.EscalationRules.Remove(rule);
            await _db.SaveChangesAsync();
            await AuditAsync("escalation_rule_delete", id, name);
            TempData["Message"] = "Eşik kuralı silindi.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        // Manuel tetik — sweeper'ı şimdi çalıştır (smoke + acil değerlendirme).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunNow()
        {
            BackgroundJob.Enqueue<EscalationSweeperJob>(j => j.ExecuteAsync(CancellationToken.None));
            await AuditAsync("escalation_rule_run_now", 0, "Manuel değerlendirme tetiklendi");
            TempData["Message"] = "Değerlendirme kuyruğa alındı. Sonuçlar birkaç saniye içinde güncellenir.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateOptionsAsync(EscalationRuleFormViewModel vm)
        {
            vm.Reports = await _db.ReportCatalog.AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Title)
                .Select(r => new EscalationRuleFormViewModel.ReportOption(r.ReportId, r.Title))
                .ToListAsync();

            vm.Users = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Username)
                .Select(u => new EscalationRuleFormViewModel.UserOption(
                    u.UserId, u.FullName != "" ? u.FullName : u.Username))
                .ToListAsync();
        }

        private Task AuditAsync(string eventType, int ruleId, string name, object? newValues = null) =>
            _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = eventType,
                TargetType = "escalation_rule",
                TargetKey = ruleId.ToString(),
                Description = $"Eşik kuralı: {name}",
                NewValuesJson = newValues != null ? AuditLogService.ToJson(newValues) : null
            });

        // Audit trail için kuralın güvenlik-ilgili anlık görüntüsü (kim, hangi eşikte uyarılıyor).
        private static object Snapshot(EscalationRule r) => new
        {
            r.Name, r.ReportId, r.ResultSet, r.Column,
            r.Aggregation, r.Operator, r.Threshold, r.NotifyUserIds, r.IsActive
        };

        private static List<int> ParseUserIds(string csv)
        {
            var result = new List<int>();
            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (int.TryParse(part, out var id))
                    result.Add(id);
            return result;
        }
    }
}
