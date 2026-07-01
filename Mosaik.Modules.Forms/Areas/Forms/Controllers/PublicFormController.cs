using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Logging;
using Mosaik.Modules.Forms.Areas.Forms.ViewModels;
using Mosaik.Modules.Forms.Entities;
using Mosaik.Modules.Forms.Services;

namespace Mosaik.Modules.Forms.Areas.Forms.Controllers
{
    // Plan 41 Faz 3 — public/anonim form doldurma (kimlik doğrulaması YOK, token ile erişim).
    // Route: /Forms/p/{formId}/{token}. AntiSpam katmanlı (honeypot→timing→rate-limit).
    [Area("Forms")]
    [AllowAnonymous]
    public class PublicFormController : Controller
    {
        private readonly DbContext _db;
        private readonly PublicTokenService _tokens;
        private readonly FormSubmissionService _submission;
        private readonly SubmitRateLimiter _rateLimiter;
        private readonly IAuditLog _audit;

        public PublicFormController(
            DbContext db, PublicTokenService tokens, FormSubmissionService submission,
            SubmitRateLimiter rateLimiter, IAuditLog audit)
        {
            _db = db;
            _tokens = tokens;
            _submission = submission;
            _rateLimiter = rateLimiter;
            _audit = audit;
        }

        [HttpGet("/Forms/p/{formId:int}/{token}")]
        public async Task<IActionResult> Render(int formId, string token, CancellationToken ct)
        {
            var valid = await _tokens.ValidateAsync(formId, token, ct);
            if (valid == null)
                return View("Expired");

            var def = await _db.Set<FormDefinition>().AsNoTracking()
                .Include(d => d.Fields.OrderBy(f => f.Order))
                .FirstOrDefaultAsync(d => d.Id == formId && d.Status == 1 && d.IsPublic, ct);
            if (def == null)
                return View("Expired");

            var latestVersion = await _db.Set<FormDefinitionVersion>().AsNoTracking()
                .Where(v => v.FormDefinitionId == formId)
                .OrderByDescending(v => v.Version)
                .FirstOrDefaultAsync(ct);
            if (latestVersion == null)
                return View("Expired");

            return View(new PublicFormRenderViewModel(
                FormId: formId, Token: token, Name: def.Name, Description: def.Description,
                SchemaJson: latestVersion.SchemaJson));
        }

        [HttpPost("/Forms/p/{formId:int}/{token}/Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int formId, string token, CancellationToken ct)
        {
            var valid = await _tokens.ValidateAsync(formId, token, ct);
            if (valid == null)
                return View("Expired");

            var def = await _db.Set<FormDefinition>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == formId && d.Status == 1 && d.IsPublic, ct);
            if (def == null)
                return View("Expired");

            // AntiSpam katmanlı (§4.5): honeypot → timing → IP-rate-limit. CAPTCHA sonraki faz opsiyonel.
            var honeypot = Request.Form["_hp"].ToString();
            // InvariantCulture ZORUNLU — JS "0.5" (nokta) gönderir; tr-TR server noktayı binlik-ayraç
            // sanıp "0.5"→5 parse ederdi (TooFast bypass, preview'de yakalandı). Parse başarısız = -1
            // (timing atlanır, bilinmiyor — 0 DEĞİL, çünkü 0 yanlışlıkla TooFast tetiklerdi).
            var elapsed = double.TryParse(Request.Form["_elapsed"].ToString(),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out var parsed) ? parsed : -1;
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rateLimited = _rateLimiter.IsRateLimited(formId, clientIp);

            var spam = AntiSpamGuard.Evaluate(honeypot, elapsed, rateLimited);
            if (spam != AntiSpamResult.Ok)
            {
                await _audit.LogAsync("form_public_spam_blocked", "form_definition", formId.ToString(), spam.ToString());
                // Bot'a ipucu verme — generic mesaj (honeypot/timing) veya rate-limit uyarısı.
                return spam == AntiSpamResult.RateLimited
                    ? StatusCode(429, "Çok fazla deneme. Lütfen biraz sonra tekrar deneyin.")
                    : BadRequest("Gönderim reddedildi.");
            }

            var values = Request.Form
                .Where(kv => kv.Key != "__RequestVerificationToken" && kv.Key != "_hp" && kv.Key != "_elapsed")
                .ToDictionary(kv => kv.Key, kv => (string?)kv.Value.ToString());

            var input = new FormSubmissionInput(
                FormDefinitionId: def.Id,
                FirmaId: def.FirmaId,
                Values: values,
                SubmittedById: null,               // anonim
                SubmitterEmail: valid.RecipientEmail,
                SubmitterPhone: null,
                SubmitterIp: clientIp,
                SubmitterUserAgent: Request.Headers.UserAgent.ToString(),
                PublicTokenId: valid.Id);

            // silent-failure-hunter CRITICAL: kullanım hakkını submission'dan ÖNCE atomik rezerve et
            // (over-use imkansız). Rezerve başarısız = slot render→submit arası tükendi/expire oldu.
            if (!await _tokens.ConsumeAsync(valid.Id, ct))
            {
                await _audit.LogAsync("form_public_token_exhausted", "public_form_token", valid.Id.ToString(), $"FormId: {formId}");
                return View("Expired");
            }

            var result = await _submission.SubmitAsync(input, ct);
            if (!result.IsSuccess)
            {
                // Submit başarısız (validation) — rezerve edilen slotu iade et (kullanıcı düzeltip tekrar denesin).
                await _tokens.RefundAsync(valid.Id, ct);
                if (result.ErrorCode == FormSubmissionService.FieldValidationErrorCode)
                    return Content(result.Message, "application/json");
                return BadRequest(result.Message);
            }

            await _audit.LogAsync("form_public_submit", "form_submission", result.Data.ToString(), $"FormId: {formId}");
            return Ok(new { submissionId = result.Data });
        }

        [HttpGet("/Forms/p/{formId:int}/{token}/Submitted")]
        public IActionResult Submitted() => View();
    }
}
