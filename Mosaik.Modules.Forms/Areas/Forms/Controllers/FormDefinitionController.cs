using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Logging;
using Mosaik.Modules.Forms.Areas.Forms.ViewModels;
using Mosaik.Modules.Forms.Entities;
using Mosaik.Modules.Forms.Services;

namespace Mosaik.Modules.Forms.Areas.Forms.Controllers
{
    // Plan 41 Faz 2 — admin Form CRUD + liste-tabanlı builder (drag-drop yok, yukarı/aşağı buton).
    [Area("Forms")]
    [Authorize(Roles = "admin")]
    public class FormDefinitionController : Controller
    {
        private readonly FormDefinitionService _definitions;
        private readonly FormFieldService _fields;
        private readonly PublicTokenService _tokens;
        private readonly DataElementLookupService _dataElements;
        private readonly FormSubmissionQueryService _submissions;
        private readonly Microsoft.EntityFrameworkCore.DbContext _db;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly FormEncryptionService _encryption;
        private readonly WorkflowTemplateLookupService _workflowTemplates;
        private readonly IAuditLog _audit;

        public FormDefinitionController(
            FormDefinitionService definitions, FormFieldService fields, PublicTokenService tokens,
            DataElementLookupService dataElements, FormSubmissionQueryService submissions,
            Microsoft.EntityFrameworkCore.DbContext db,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
            FormEncryptionService encryption, WorkflowTemplateLookupService workflowTemplates, IAuditLog audit)
        {
            _definitions = definitions;
            _fields = fields;
            _tokens = tokens;
            _dataElements = dataElements;
            _submissions = submissions;
            _db = db;
            _env = env;
            _encryption = encryption;
            _workflowTemplates = workflowTemplates;
            _audit = audit;
        }

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> Index(bool includeArchived = false)
        {
            ViewBag.IncludeArchived = includeArchived;
            var list = await _definitions.ListByFirmaAsync(CurrentFirmaId, includeArchived);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.WorkflowTemplates = await _workflowTemplates.ListForFormsAsync(CurrentFirmaId);
            return View("Edit", new FormDefinitionFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FormDefinitionFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.WorkflowTemplates = await _workflowTemplates.ListForFormsAsync(CurrentFirmaId);
                return View("Edit", model);
            }

            var r = await _definitions.CreateAsync(ToInput(model), CurrentFirmaId, CurrentUserId);
            if (!r.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, r.Message);
                ViewBag.WorkflowTemplates = await _workflowTemplates.ListForFormsAsync(CurrentFirmaId);
                return View("Edit", model);
            }

            await _audit.LogAsync("form_create", "form_definition", r.Data.ToString(), model.Name);
            TempData["Message"] = "Form oluşturuldu.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Details), new { id = r.Data });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var def = await _definitions.GetAsync(id, CurrentFirmaId);
            if (def == null)
                return NotFound();

            var templates = await _workflowTemplates.ListForFormsAsync(CurrentFirmaId);
            ViewBag.WorkflowTemplates = templates;
            // Silent-failure H3: bağlı şablon sonradan silinmiş/pasifleşmişse admin'e görünür uyarı —
            // aksi halde stale bağ submit'te sessizce TEMPLATE_NOT_FOUND üretirdi.
            if (def.TriggersWorkflowId is int boundId && templates.All(t => t.Id != boundId))
                ViewBag.StaleWorkflowWarning = "Bu forma bağlı onay şablonu artık aktif değil — akış tetiklenmeyecek. Yeni bir şablon seçin veya bağı kaldırın.";
            return View(new FormDefinitionFormViewModel
            {
                Id = def.Id, Slug = def.Slug, Name = def.Name, Description = def.Description,
                Category = def.Category, IsPublic = def.IsPublic, IsAnonymous = def.IsAnonymous,
                IsEncrypted = def.IsEncrypted, TriggersWorkflowId = def.TriggersWorkflowId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FormDefinitionFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.WorkflowTemplates = await _workflowTemplates.ListForFormsAsync(CurrentFirmaId);
                return View(model);
            }

            var r = await _definitions.UpdateAsync(id, ToInput(model), CurrentFirmaId);
            if (!r.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, r.Message);
                ViewBag.WorkflowTemplates = await _workflowTemplates.ListForFormsAsync(CurrentFirmaId);
                return View(model);
            }

            await _audit.LogAsync("form_update", "form_definition", id.ToString(), model.Name);
            TempData["Message"] = "Form güncellendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var def = await _definitions.GetAsync(id, CurrentFirmaId);
            if (def == null)
                return NotFound();
            ViewBag.FieldTypeOptions = FieldTypeOptions();

            // Faz 4 — KVKK DataElement eşleme picker verisi (Kvkk modülü yoksa boş → picker gizlenir).
            var elements = await _dataElements.ListActiveAsync();
            ViewBag.DataElements = elements;
            ViewBag.DataElementNames = elements.ToDictionary(e => e.Id, e => e.DisplayName);
            return View(def);
        }

        // Faz 4 §4.3 — field → KVKK DataElement eşle (opsiyonel).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MapDataElement(int fieldId, int formDefinitionId, int dataElementId, byte usageType, string? notes)
        {
            var r = await _fields.MapDataElementAsync(fieldId, formDefinitionId, CurrentFirmaId, dataElementId, usageType, notes);
            TempData["Message"] = r.IsSuccess ? "Veri öğesi eşlendi." : r.Message;
            TempData["MessageType"] = r.IsSuccess ? "success" : "error";
            if (r.IsSuccess)
                await _audit.LogAsync("form_field_map_dataelement", "form_field", fieldId.ToString(), $"dataElementId={dataElementId}");
            return RedirectToAction(nameof(Details), new { id = formDefinitionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnmapDataElement(int mapId, int formDefinitionId)
        {
            var r = await _fields.UnmapDataElementAsync(mapId, formDefinitionId, CurrentFirmaId);
            TempData["Message"] = r.IsSuccess ? "Eşleme kaldırıldı." : r.Message;
            TempData["MessageType"] = r.IsSuccess ? "success" : "error";
            if (r.IsSuccess)
                await _audit.LogAsync("form_field_unmap_dataelement", "form_field_map", mapId.ToString(), string.Empty);
            return RedirectToAction(nameof(Details), new { id = formDefinitionId });
        }

        // Faz 4 — submission eki/imza indir. Admin-only + firma guard + App_Data/forms altında path guard.
        [HttpGet]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            var file = await _db.Set<FormSubmissionFile>().AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == fileId && f.FirmaId == CurrentFirmaId);
            if (file == null)
                return NotFound();

            // Trailing separator — "forms" prefix'i "forms_export" gibi kardeş dizinle eşleşmesin (security M-1).
            var root = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data", "forms")) + Path.DirectorySeparatorChar;
            var abs = Path.GetFullPath(Path.Combine(_env.ContentRootPath, file.DiskPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!abs.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(abs))
                return NotFound();

            // Plan 57 A2-#1 — şifreli dosya: decrypt SADECE İhbar Komitesi/admin (G2 canDecrypt kuralı).
            // Ham ciphertext da yetkisize verilmez (anlamsız + key-id sızdırma yok) → Forbid.
            if (file.IsEncrypted)
            {
                var canDecrypt = User.IsInRole("ihbar-komitesi") || User.IsInRole("admin");
                if (!canDecrypt)
                    return Forbid();

                // Bilinçli tam-buffer (M-1): DataProtection'da streaming decrypt yok; şifreli form
                // upload'ı 10MB cap'li (SubmitAsync maxBytes) → bellek maliyeti sınırlı, admin-only path.
                var cipher = await System.IO.File.ReadAllBytesAsync(abs);
                if (!_encryption.TryDecryptBytes(cipher, out var plain))
                {
                    // Key kayıp/bozuk — sessiz bozuk dosya İNDİRTME (silent-failure): logla + generic mesaj.
                    // Redirect kullanıcının geldiği yanıt-detay sayfasına (mesajı orası render eder;
                    // Index TempData göstermiyordu — silent-failure HIGH fix).
                    await _audit.LogAsync("form_file_decrypt_failed", "form_submission_file", fileId.ToString(), file.FileName);
                    TempData["Message"] = "Dosya çözülemedi — sistem yöneticisine bildirin.";
                    TempData["MessageType"] = "error";
                    return RedirectToAction(nameof(SubmissionDetail), new { id = file.FormSubmissionId });
                }

                await _audit.LogAsync("form_file_download", "form_submission_file", fileId.ToString(), $"{file.FileName} (şifreli, çözüldü)");
                return File(plain, file.MimeType ?? "application/octet-stream", file.FileName);
            }

            await _audit.LogAsync("form_file_download", "form_submission_file", fileId.ToString(), file.FileName);
            try
            {
                // TOCTOU: Exists ile OpenRead arası dosya silinebilir/kilitlenebilir (AV) — 500 yerine 404.
                var stream = System.IO.File.OpenRead(abs);
                return File(stream, file.MimeType ?? "application/octet-stream", file.FileName);
            }
            catch (IOException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var r = await _definitions.PublishAsync(id, CurrentFirmaId, CurrentUserId);
            TempData["Message"] = r.Message;
            TempData["MessageType"] = r.IsSuccess ? "success" : "error";
            // silent-failure-hunter MEDIUM: eşlenmemiş alan uyarı listesi (r.Data) hesaplanıp
            // atılıyordu — hangi alanların bağlı olmadığı admin'e gösterilsin (Details render eder).
            if (r.IsSuccess && r.Data is { Count: > 0 })
                TempData["PublishWarnings"] = System.Text.Json.JsonSerializer.Serialize(r.Data);
            if (r.IsSuccess)
                await _audit.LogAsync("form_publish", "form_definition", id.ToString(), r.Message);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var r = await _definitions.SetStatusAsync(id, CurrentFirmaId, 2);
            TempData["Message"] = r.IsSuccess ? "Form arşivlendi." : r.Message;
            TempData["MessageType"] = r.IsSuccess ? "success" : "error";
            if (r.IsSuccess)
                await _audit.LogAsync("form_archive", "form_definition", id.ToString(), string.Empty);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var r = await _definitions.SetStatusAsync(id, CurrentFirmaId, 0);
            TempData["Message"] = r.IsSuccess ? "Form taslağa alındı." : r.Message;
            TempData["MessageType"] = r.IsSuccess ? "success" : "error";
            if (r.IsSuccess)
                await _audit.LogAsync("form_restore", "form_definition", id.ToString(), string.Empty);
            return RedirectToAction(nameof(Details), new { id });
        }

        // Plan 41 Faz 3 — public link üret. Token plaintext SADECE burada bir kez gösterilir
        // (DB'de hash saklanır — geri okunamaz).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePublicLink(int id, int expiryDays, int maxUses, string? recipientEmail)
        {
            if (expiryDays < 1) expiryDays = 30;
            if (maxUses < 1) maxUses = 1;
            var expiresAt = DateTime.UtcNow.AddDays(expiryDays);
            var r = await _tokens.CreateAsync(id, CurrentFirmaId, CurrentUserId, expiresAt, maxUses, recipientEmail);
            if (!r.IsSuccess)
            {
                TempData["Message"] = r.Message;
                TempData["MessageType"] = "error";
            }
            else
            {
                // Tam URL — admin kopyalasın (bir daha gösterilmez).
                var url = $"{Request.Scheme}://{Request.Host}/Forms/p/{id}/{r.Data!.PlainToken}";
                TempData["PublicLink"] = url;
                TempData["Message"] = "Public link oluşturuldu — aşağıdaki bağlantıyı kopyalayın (bir daha gösterilmez).";
                TempData["MessageType"] = "success";
                await _audit.LogAsync("form_public_link_create", "form_definition", id.ToString(), $"maxUses={maxUses}, expiry={expiryDays}g");
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddField(int formDefinitionId, FormFieldFormViewModel model)
        {
            var r = await _fields.AddAsync(formDefinitionId, CurrentFirmaId, ToFieldInput(model));
            TempData["Message"] = r.IsSuccess ? "Alan eklendi." : r.Message;
            TempData["MessageType"] = r.IsSuccess ? "success" : "error";
            if (r.IsSuccess)
                await _audit.LogAsync("form_field_add", "form_field", r.Data.ToString(), model.Label);
            return RedirectToAction(nameof(Details), new { id = formDefinitionId });
        }

        [HttpGet]
        public async Task<IActionResult> EditField(int id, int formDefinitionId)
        {
            var def = await _definitions.GetAsync(formDefinitionId, CurrentFirmaId);
            var field = def?.Fields.FirstOrDefault(f => f.Id == id);
            if (field == null)
                return NotFound();

            ViewBag.FieldTypeOptions = FieldTypeOptions();
            // G3 koşul referansı seçimi: aynı formdaki DİĞER alanların anahtarları (kendine referans yok).
            ViewBag.SiblingFieldKeys = def!.Fields
                .Where(f => f.Id != id)
                .OrderBy(f => f.Order)
                .Select(f => f.FieldKey)
                .ToList();
            return View(FromField(field));
        }

        private static List<(byte Value, string Text)> FieldTypeOptions() =>
            Enumerable.Range(0, 13).Select(i => ((byte)i, FormLabels.FieldType((byte)i))).ToList();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditField(int id, FormFieldFormViewModel model)
        {
            var r = await _fields.UpdateAsync(id, model.FormDefinitionId, CurrentFirmaId, ToFieldInput(model));
            TempData["Message"] = r.IsSuccess ? "Alan güncellendi." : r.Message;
            TempData["MessageType"] = r.IsSuccess ? "success" : "error";
            if (r.IsSuccess)
                await _audit.LogAsync("form_field_update", "form_field", id.ToString(), model.Label);
            return RedirectToAction(nameof(Details), new { id = model.FormDefinitionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveField(int id, int formDefinitionId)
        {
            var r = await _fields.RemoveAsync(id, formDefinitionId, CurrentFirmaId);
            TempData["Message"] = r.IsSuccess ? "Alan kaldırıldı." : r.Message;
            TempData["MessageType"] = r.IsSuccess ? "success" : "error";
            if (r.IsSuccess)
                await _audit.LogAsync("form_field_remove", "form_field", id.ToString(), string.Empty);
            return RedirectToAction(nameof(Details), new { id = formDefinitionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveField(int id, int formDefinitionId, string direction)
        {
            if (direction != "up" && direction != "down")
                return BadRequest();
            var dir = direction == "up" ? FieldMoveDirection.Up : FieldMoveDirection.Down;
            var r = await _fields.MoveAsync(id, formDefinitionId, CurrentFirmaId, dir);
            if (!r.IsSuccess)
            {
                // silent-failure-hunter CRITICAL: cross-firma / stale field move sessizce "başarı"
                // gibi görünüyordu — sonuç kontrol + audit eklendi (diğer field action'larla tutarlı).
                TempData["Message"] = r.Message;
                TempData["MessageType"] = "error";
            }
            else
            {
                await _audit.LogAsync("form_field_move", "form_field", id.ToString(), direction);
            }
            return RedirectToAction(nameof(Details), new { id = formDefinitionId });
        }

        // Plan 56 M-A G2 — submission admin liste/detay/export. Şifreli alan decrypt YETKİ-GATED.
        [HttpGet]
        public async Task<IActionResult> Submissions(int id, CancellationToken ct)
        {
            var r = await _submissions.ListAsync(id, CurrentFirmaId, ct);
            if (r is null) return NotFound();
            await _audit.LogAsync("form_submissions_view", "form", id.ToString(), $"Yanıt listesi ({r.Value.Rows.Count})");
            ViewBag.FormId = id;
            ViewBag.FormName = r.Value.Form.Name;
            return View(r.Value.Rows);
        }

        [HttpGet]
        public async Task<IActionResult> SubmissionDetail(int id, CancellationToken ct)
        {
            // Şifreli alanı sadece İhbar Komitesi veya admin çözer; diğerine "🔒" maske (mosaik-security).
            var canDecrypt = User.IsInRole("ihbar-komitesi") || User.IsInRole("admin");
            var d = await _submissions.DetailAsync(id, CurrentFirmaId, canDecrypt, ct);
            if (d is null) return NotFound();
            await _audit.LogAsync("form_submission_view", "form_submission", id.ToString(), "Yanıt detayı");
            // Audit "ne oldu": gerçekten çözülen alan varsa logla (yetkili+key sağlam) — sadece izinli olmak değil.
            if (d.AnyDecrypted)
                await _audit.LogAsync("form_submission_decrypt", "form_submission", id.ToString(), "Şifreli alan çözüldü (yetkili erişim)");
            return View(d);
        }

        [HttpGet]
        public async Task<IActionResult> ExportSubmissions(int id, CancellationToken ct)
        {
            var r = await _submissions.ExportAsync(id, CurrentFirmaId, ct);
            if (r is null) return NotFound();
            await _audit.LogAsync("form_submissions_export", "form", id.ToString(),
                $"Yanıtlar Excel export ({r.Value.Count} kayıt, şifreli alan maskeli)");
            var name = $"form-{id}-yanitlar.xlsx";
            return File(r.Value.Bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
        }

        private static FormDefinitionInput ToInput(FormDefinitionFormViewModel m) => new(
            Slug: m.Slug, Name: m.Name, Description: m.Description, Category: m.Category,
            IsPublic: m.IsPublic, IsAnonymous: m.IsAnonymous, IsEncrypted: m.IsEncrypted,
            TriggersWorkflowId: m.TriggersWorkflowId);

        // Options: satır satır girilen metin → JSON string dizisi. ValidationRules: minLength/maxLength/regex → JSON.
        private static FormFieldInput ToFieldInput(FormFieldFormViewModel m)
        {
            string? optionsJson = null;
            if (FormLabels.RequiresOptions(m.FieldType) && !string.IsNullOrWhiteSpace(m.OptionsRaw))
            {
                var lines = m.OptionsRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                optionsJson = JsonSerializer.Serialize(lines);
            }

            string? rulesJson = null;
            if (m.MinLength.HasValue || m.MaxLength.HasValue || !string.IsNullOrWhiteSpace(m.Regex))
            {
                var rules = new JsonObject();
                if (m.MinLength.HasValue) rules["minLength"] = m.MinLength.Value;
                if (m.MaxLength.HasValue) rules["maxLength"] = m.MaxLength.Value;
                if (!string.IsNullOrWhiteSpace(m.Regex)) rules["regex"] = m.Regex;
                rulesJson = rules.ToJsonString();
            }

            // G3 koşullu görünürlük: referans alan + değer verildiyse structured JSON. op eq/ne whitelist.
            string? conditionalJson = null;
            if (!string.IsNullOrWhiteSpace(m.ConditionalField) && !string.IsNullOrWhiteSpace(m.ConditionalValue))
            {
                var op = m.ConditionalOp == "ne" ? "ne" : "eq";
                conditionalJson = new JsonObject
                {
                    ["field"] = m.ConditionalField.Trim(),
                    ["op"] = op,
                    ["value"] = m.ConditionalValue
                }.ToJsonString();
            }

            return new FormFieldInput(
                FieldKey: m.FieldKey, Label: m.Label, HelpText: m.HelpText, FieldType: m.FieldType,
                IsRequired: m.IsRequired, Options: optionsJson, ValidationRules: rulesJson,
                DefaultValue: m.DefaultValue, Placeholder: m.Placeholder, ConditionalLogic: conditionalJson);
        }

        private static FormFieldFormViewModel FromField(FormField f)
        {
            var vm = new FormFieldFormViewModel
            {
                Id = f.Id, FormDefinitionId = f.FormDefinitionId, FieldKey = f.FieldKey, Label = f.Label,
                HelpText = f.HelpText, FieldType = f.FieldType, IsRequired = f.IsRequired,
                DefaultValue = f.DefaultValue, Placeholder = f.Placeholder
            };

            if (!string.IsNullOrWhiteSpace(f.Options))
            {
                try
                {
                    var arr = JsonSerializer.Deserialize<string[]>(f.Options);
                    if (arr != null) vm.OptionsRaw = string.Join('\n', arr);
                }
                catch (JsonException) { /* bozuk JSON — boş bırak, admin yeniden yazar */ }
            }

            if (!string.IsNullOrWhiteSpace(f.ValidationRules))
            {
                try
                {
                    var rules = JsonNode.Parse(f.ValidationRules) as JsonObject;
                    if (rules?["minLength"] is JsonValue minL && minL.TryGetValue(out int minLen)) vm.MinLength = minLen;
                    if (rules?["maxLength"] is JsonValue maxL && maxL.TryGetValue(out int maxLen)) vm.MaxLength = maxLen;
                    if (rules?["regex"] is JsonValue rx && rx.TryGetValue(out string? pattern)) vm.Regex = pattern;
                }
                catch (JsonException) { /* bozuk JSON — boş bırak */ }
            }

            // G3 koşullu görünürlük: structured JSON → VM alanları (bozuksa boş, admin yeniden kurar).
            var cond = FormConditionEvaluator.TryParse(f.ConditionalLogic);
            if (cond != null)
            {
                vm.ConditionalField = cond.Field;
                vm.ConditionalOp = cond.Op;
                vm.ConditionalValue = cond.Value;
            }

            return vm;
        }
    }
}
