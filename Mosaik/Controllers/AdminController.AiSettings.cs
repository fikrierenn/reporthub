using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Ai;
using Mosaik.Models;
using Mosaik.Services;
using AuditLogEntry = Mosaik.Services.AuditLogEntry;

namespace Mosaik.Controllers
{
    public partial class AdminController
    {
        // GET /Admin/AiSettings — liste sayfası (tüm profiller)
        [Route("Admin/AiSettings")]
        [HttpGet]
        public async Task<IActionResult> AiSettings()
        {
            var rows = await _context.AiSettings.AsNoTracking()
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.Priority)
                .ThenBy(x => x.Id)
                .ToListAsync();
            return View(rows);
        }

        // GET /Admin/AiSettings/Edit/{id?} — yeni veya düzenle
        [Route("Admin/AiSettings/Edit/{id?}")]
        [HttpGet]
        public async Task<IActionResult> AiSettingsEdit(int? id)
        {
            AiSettings entity;
            if (id.HasValue)
            {
                entity = await _context.AiSettings.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id.Value)
                    ?? throw new InvalidOperationException("AI profili bulunamadı.");
            }
            else
            {
                entity = new AiSettings { Provider = "groq", Model = "llama-3.3-70b-versatile" };
            }
            return View("AiSettingsEdit", entity);
        }

        [Route("Admin/AiSettings/Edit/{id?}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AiSettingsEdit(int? id, AiSettings input,
            [FromServices] IDataProtectionProvider dpProvider)
        {
            AiSettings entity;
            if (id.HasValue)
            {
                entity = await _context.AiSettings.FirstOrDefaultAsync(x => x.Id == id.Value)
                    ?? throw new InvalidOperationException("AI profili bulunamadı.");
            }
            else
            {
                entity = new AiSettings();
                _context.AiSettings.Add(entity);
            }

            entity.Name = string.IsNullOrWhiteSpace(input.Name) ? null : input.Name.Trim();
            entity.Provider = (input.Provider ?? "groq").Trim();
            // Boş ise eskiyi koru (re-edit'te key görünmesin diye). Yeni key girilmişse encrypt.
            if (!string.IsNullOrWhiteSpace(input.ApiKey))
                entity.ApiKey = dpProvider.CreateProtector("Mosaik.AiSettings.ApiKey").Protect(input.ApiKey.Trim());
            entity.Model = (input.Model ?? "").Trim();
            entity.MaxTokens = Math.Clamp(input.MaxTokens, 128, 8192);
            entity.Temperature = Math.Clamp(input.Temperature, 0.0, 2.0);
            entity.BaseUrl = string.IsNullOrWhiteSpace(input.BaseUrl) ? null : input.BaseUrl.Trim();
            entity.IsEnabled = input.IsEnabled;
            entity.Priority = input.Priority < 0 ? 100 : input.Priority;
            await _context.SaveChangesAsync();

            // IsPrimary: tek bir kayıt primary olabilir
            if (input.IsPrimary && !entity.IsPrimary)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE AiSettings SET IsPrimary = 0 WHERE Id <> {0}", entity.Id);
                entity.IsPrimary = true;
                await _context.SaveChangesAsync();
            }

            AiSettingsProvider.InvalidateCache();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "ai_settings_save",
                TargetType = "ai_settings",
                TargetKey = entity.Id.ToString(),
                Description = $"Provider={entity.Provider}, Model={entity.Model}, Primary={entity.IsPrimary}, Enabled={entity.IsEnabled}"
            });

            TempData["Message"] = "AI profili kaydedildi.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(AiSettings));
        }

        [Route("Admin/AiSettings/SetPrimary/{id:int}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AiSettingsSetPrimary(int id)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE AiSettings SET IsPrimary = 0 WHERE Id <> {0}", id);
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE AiSettings SET IsPrimary = 1 WHERE Id = {0}", id);
            AiSettingsProvider.InvalidateCache();

            TempData["Message"] = "Birincil provider güncellendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(AiSettings));
        }

        [Route("Admin/AiSettings/Delete/{id:int}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AiSettingsDelete(int id)
        {
            var entity = await _context.AiSettings.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return NotFound();
            _context.AiSettings.Remove(entity);
            await _context.SaveChangesAsync();
            AiSettingsProvider.InvalidateCache();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "ai_settings_delete",
                TargetType = "ai_settings",
                TargetKey = id.ToString()
            });

            TempData["Message"] = "AI profili silindi.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(AiSettings));
        }

        [Route("Admin/AiSettings/Test/{id:int}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AiSettingsTest(int id, [FromServices] IAiSummaryProvider ai)
        {
            var entity = await _context.AiSettings.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return NotFound();

            if (string.IsNullOrWhiteSpace(entity.ApiKey))
            {
                TempData["Message"] = "API key kayıtlı değil.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(AiSettings));
            }
            if (!entity.IsEnabled)
            {
                TempData["Message"] = "Bu profil kapalı (IsEnabled=false). Önce aktif et.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(AiSettings));
            }

            // Sadece BU profil ile test yapmak için: cache'i temizle, geçici tek-config
            // Pragmatik: bu profil primary VE diğerlerini geçici devre dışı bırakmak yerine
            // SQL ile direkt çağırıyoruz. Daha temiz: ITestableAi servisi. Şimdilik yapay
            // kontrol — Provider sırasını override etmek için tek-elemanlı override:
            AiSettingsProvider.InvalidateCache();

            // Daha basit: Provider ordered list zaten primary'i ilk dener. Tek profili
            // test etmek için diğerlerini geçici etkisizleştirelim:
            var others = await _context.AiSettings.Where(x => x.Id != id && x.IsEnabled).ToListAsync();
            foreach (var o in others) o.IsEnabled = false;
            entity.IsPrimary = true;
            await _context.SaveChangesAsync();
            AiSettingsProvider.InvalidateCache();

            AiSummaryResult result;
            try
            {
                result = await ai.GenerateAsync(new AiRequest(
                    SystemPrompt: "Sen kısa Türkçe özet üreten bir asistansın. Çıktı sadece JSON: {\"ok\": true, \"echo\": \"<gelen mesajın aynısı>\"}",
                    UserPrompt: "Test mesajı: Mosaik AI bağlantısı çalışıyor.",
                    Purpose: "settings_test"));
            }
            finally
            {
                // Exception olsa bile diğer profilleri geri aç.
                foreach (var o in others) o.IsEnabled = true;
                await _context.SaveChangesAsync();
                AiSettingsProvider.InvalidateCache();
            }

            entity.LastTestAt = DateTime.UtcNow;
            entity.LastTestSuccess = result.IsSuccess;
            var msg = result.IsSuccess
                ? $"OK · model={result.ModelUsed} · in={result.InputTokens} out={result.OutputTokens}"
                : $"FAIL · {result.Error}";
            entity.LastTestMessage = msg.Length > 500 ? msg.Substring(0, 500) : msg;
            await _context.SaveChangesAsync();

            TempData["Message"] = result.IsSuccess
                ? $"Test başarılı [{entity.Name ?? entity.Provider}]. Yanıt: {result.RawJson}"
                : $"Test başarısız [{entity.Name ?? entity.Provider}]: {result.Error}";
            TempData["MessageType"] = result.IsSuccess ? "success" : "error";
            return RedirectToAction(nameof(AiSettings));
        }
    }
}
