using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.Logging;
using Mosaik.Modules.Forms.Areas.Forms.ViewModels;
using Mosaik.Modules.Forms.Services;

namespace Mosaik.Modules.Forms.Areas.Forms.Controllers
{
    // Plan 41 Faz 1 — form render + submit (internal/auth'lu kullanıcı). Public/anonim erişim
    // Faz 3 (PublicFormController) kapsamında.
    [Area("Forms")]
    [Authorize]
    public class FormController : Controller
    {
        private readonly FormRendererService _renderer;
        private readonly FormSubmissionService _submission;
        private readonly IAuditLog _audit;

        public FormController(FormRendererService renderer, FormSubmissionService submission, IAuditLog audit)
        {
            _renderer = renderer;
            _submission = submission;
            _audit = audit;
        }

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // Route: /Forms/f/{slug} — admin CRUD (/Forms/FormDefinition/*) ile çakışmaması için "f/" prefix.
        [HttpGet("/Forms/f/{slug}")]
        public async Task<IActionResult> Render(string slug, CancellationToken ct)
        {
            var model = await _renderer.GetBySlugAsync(slug, CurrentFirmaId, ct);
            if (model == null)
                return NotFound();

            return View(new FormRenderViewModel(slug, model.Definition.Name, model.Definition.Description, model.SchemaJson));
        }

        [HttpPost("/Forms/f/{slug}/Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string slug, CancellationToken ct)
        {
            var model = await _renderer.GetBySlugAsync(slug, CurrentFirmaId, ct);
            if (model == null)
                return NotFound();

            var values = Request.Form
                .Where(kv => kv.Key != "__RequestVerificationToken")
                .ToDictionary(kv => kv.Key, kv => (string?)kv.Value.ToString());

            var input = new FormSubmissionInput(
                FormDefinitionId: model.Definition.Id,
                FirmaId: CurrentFirmaId,
                Values: values,
                SubmittedById: CurrentUserId,
                SubmitterEmail: null,
                SubmitterPhone: null,
                SubmitterIp: HttpContext.Connection.RemoteIpAddress?.ToString(),
                SubmitterUserAgent: Request.Headers.UserAgent.ToString(),
                PublicTokenId: null);

            var result = await _submission.SubmitAsync(input, ct);
            if (!result.IsSuccess)
            {
                // silent-failure-hunter HIGH — alan-bazlı hata JSON'u (fieldKey→mesaj) ile plain-text
                // genel hatayı ayırt et; JS survey-core question.addError'a bağlayabilsin.
                if (result.ErrorCode == FormSubmissionService.FieldValidationErrorCode)
                    return Content(result.Message, "application/json");
                return BadRequest(result.Message);
            }

            await _audit.LogAsync("form_submitted", "form_submission", result.Data.ToString(), $"Slug: {slug}");
            return Ok(new { submissionId = result.Data });
        }

        [HttpGet("/Forms/f/{slug}/Submitted")]
        public IActionResult Submitted(string slug)
        {
            ViewBag.Slug = slug;
            return View();
        }
    }
}
