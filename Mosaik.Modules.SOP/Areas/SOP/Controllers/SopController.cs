using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Lookup;
using Mosaik.Core.Workflow;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;
using Mosaik.Modules.SOP.ViewModels;

namespace Mosaik.Modules.SOP.Areas.SOP.Controllers
{
    // Plan 34 Faz C — SOP admin CRUD + versiyon + onay başlatma.
    // Roller: admin + sop-editor (Faz B'de role seed atlandı, S-13 sonrası eklenir).
    [Area("SOP")]
    [Authorize(Roles = "admin,sop-editor")]
    public class SopController : Controller
    {
        private readonly SopService _sop;
        private readonly SopApprovalService _approval;
        private readonly ILookupService _lookup;
        private readonly DbContext _db;

        public SopController(SopService sop, SopApprovalService approval, ILookupService lookup, DbContext db)
        {
            _sop = sop;
            _approval = approval;
            _lookup = lookup;
            _db = db;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private int CurrentFirmaId =>
            int.TryParse(User.FindFirstValue("FirmaId"), out var id) ? id : 0;

        public async Task<IActionResult> Index(bool showArchived = false)
        {
            var list = await _sop.ListByFirmaAsync(CurrentFirmaId, onlyActive: !showArchived);
            ViewBag.CategoryMap = (await _lookup.GetValuesAsync("sopCategory"))
                .ToDictionary(v => v.Code, v => v.Label);
            ViewBag.ShowArchived = showArchived;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateLookupsAsync();
            return View(new SopFormViewModel
            {
                FirmaId = CurrentFirmaId,
                ReadDeadlineDays = 30,
                RequiresIKApproval = true,
                AiAdvisorEnabled = true
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SopFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync();
                return View(model);
            }

            var input = MapToInput(model);
            var result = await _sop.CreateAsync(input, CurrentUserId);
            if (!result.IsSuccess || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await PopulateLookupsAsync();
                return View(model);
            }

            TempData["Message"] = result.Message + " Şimdi 'Yeni Versiyon' ile editörde yaz veya 'PDF Seç' ile içe aktar.";
            return RedirectToAction(nameof(Details), new { id = result.Data.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var doc = await _sop.GetAsync(id);
            if (doc == null) return NotFound();

            await PopulateLookupsAsync();
            return View(new SopFormViewModel
            {
                Id = doc.Id,
                FirmaId = doc.FirmaId,
                Title = doc.Title,
                Description = doc.Description,
                Category = doc.Category,
                DepartmentIds = doc.DepartmentIds,
                IsCompanyWide = doc.IsCompanyWide,
                OwnerUserId = doc.OwnerUserId,
                ReadDeadlineDays = doc.ReadDeadlineDays,
                RequiresIKApproval = doc.RequiresIKApproval,
                AiAdvisorEnabled = doc.AiAdvisorEnabled,
                DocumentNumber = doc.DocumentNumber,
                RevisionNumber = doc.RevisionNumber,
                PublishDate = doc.PublishDate,
                RevisionDate = doc.RevisionDate,
                EffectiveDate = doc.EffectiveDate,
                PreparedBy = doc.PreparedBy,
                ApprovedBy = doc.ApprovedBy,
                ReviewFrequency = doc.ReviewFrequency,
                Classification = doc.Classification
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SopFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync();
                return View(model);
            }

            var input = MapToInput(model);
            var result = await _sop.UpdateAsync(id, input, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await PopulateLookupsAsync();
                return View(model);
            }

            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var doc = await _sop.GetAsync(id);
            if (doc == null) return NotFound();

            var active = await _sop.GetActiveVersionAsync(id);

            // Lookup status etiketleri (Mosaik kuralı: status değerleri DB lookup'tan).
            ViewBag.VersionStatusMap = (await _lookup.GetValuesAsync("sopVersionStatus"))
                .ToDictionary(v => v.Code, v => v.Label);
            ViewBag.EnrichmentStatusMap = (await _lookup.GetValuesAsync("sopEnrichmentStatus"))
                .ToDictionary(v => v.Code, v => v.Label);

            // Plan 34.1 Save-time AI Enrichment — son rapor (Active varsa onun versiyon ID'si, yoksa en son version).
            SopEnrichmentReport? latestReport = null;
            int? latestReportVersionNumber = null;
            var versionIds = doc.Versions.Select(v => v.Id).ToList();
            if (versionIds.Count > 0)
            {
                latestReport = await _db.Set<SopEnrichmentReport>()
                    .AsNoTracking()
                    .Where(r => versionIds.Contains(r.SopVersionId))
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();
                if (latestReport != null)
                {
                    latestReportVersionNumber = doc.Versions
                        .Where(v => v.Id == latestReport.SopVersionId)
                        .Select(v => (int?)v.VersionNumber)
                        .FirstOrDefault();
                }
            }

            return View(new SopDetailsViewModel
            {
                Document = doc,
                Versions = doc.Versions.OrderByDescending(v => v.VersionNumber).ToList(),
                ActiveVersion = active,
                LatestEnrichmentReport = latestReport,
                LatestEnrichmentVersionNumber = latestReportVersionNumber
            });
        }

        [HttpGet]
        public async Task<IActionResult> NewVersion(int id)
        {
            var doc = await _sop.GetAsync(id);
            if (doc == null) return NotFound();
            ViewBag.Document = doc;
            return View(new SopVersionFormViewModel { SopDocumentId = id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewVersion(SopVersionFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Document = await _sop.GetAsync(model.SopDocumentId);
                return View(model);
            }

            var result = await _sop.NewVersionAsync(model.SopDocumentId, model.ContentJson, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.Document = await _sop.GetAsync(model.SopDocumentId);
                return View(model);
            }

            // Plan 34.1 Save-time AI Enrichment — background review enqueue
            if (result.Data is not null)
                EnqueueEnrichment(result.Data.Id);

            TempData["Message"] = result.Message + " AI inceleme arka planda başlatıldı.";
            return RedirectToAction(nameof(Details), new { id = model.SopDocumentId });
        }

        // Plan 34.1 Save-time AI Enrichment — Hangfire background helper.
        // LLM yoğun (~15-30 sn Qwen 3B CPU); kullanıcıyı bekletmez.
        private static void EnqueueEnrichment(int versionId)
        {
            try
            {
                Hangfire.BackgroundJob.Enqueue<SopEnrichmentService>(
                    s => s.ReviewAsync(versionId, CancellationToken.None));
            }
            catch
            {
                // Enqueue başarısızsa save'i bozmaz. Audit zaten servis içinde.
            }
        }

        // Plan 34.1 Save-time AI Enrichment — önerileri kabul et.
        // SopEnrichmentService.AcceptAsync: suggested_category whitelist'teyse SopDocument'e yansır.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptEnrichment(int reportId, [FromServices] SopEnrichmentService enrichment)
        {
            // Report'tan SopDocument.Id çıkar (redirect için).
            var report = await _db.Set<SopEnrichmentReport>()
                .AsNoTracking()
                .Include(r => r.SopVersion)
                .FirstOrDefaultAsync(r => r.Id == reportId);
            if (report?.SopVersion is null)
            {
                TempData["Message"] = "Rapor bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var (ok, message) = await enrichment.AcceptAsync(reportId, CurrentUserId);
            TempData["Message"] = message;
            return RedirectToAction(nameof(Details), new { id = report.SopVersion.SopDocumentId });
        }

        // Plan 34.1: AI taramayı yeniden başlat (Draft veya Active versiyon üzerinde).
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RerunEnrichment(int versionId)
        {
            var version = await _sop.GetVersionAsync(versionId);
            if (version is null)
            {
                TempData["Message"] = "Versiyon bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            EnqueueEnrichment(versionId);
            TempData["Message"] = "AI inceleme yeniden başlatıldı. Sayfayı 30 sn sonra yenileyin.";
            return RedirectToAction(nameof(Details), new { id = version.SopDocumentId });
        }

        // Plan 34.1: SOP soft delete (arşivle). Index listeden gizler ama veri korur.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _sop.SoftDeleteAsync(id, CurrentUserId);
            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // Plan 34.1: SOP geri yükle (arşivden çıkar).
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var result = await _sop.RestoreAsync(id, CurrentUserId);
            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        // Plan 34.1: Kalıcı silme — sadece Approved versiyonu olmayan (Draft-only) SOP'lar.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(int id)
        {
            var result = await _sop.HardDeleteAsync(id, CurrentUserId);
            TempData["Message"] = result.Message;
            return result.IsSuccess
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(Details), new { id });
        }

        // Plan 34.1: Mevcut Draft versiyonu TinyMCE editor'de aç + düzenle.
        [HttpGet]
        public async Task<IActionResult> EditVersion(int versionId)
        {
            var version = await _sop.GetVersionAsync(versionId);
            if (version?.SopDocument == null) return NotFound();
            if (version.Status != 0)
            {
                TempData["Message"] = "Sadece Draft versiyon düzenlenebilir. Pending/Approved versiyonlar için yeni versiyon oluşturun.";
                return RedirectToAction(nameof(Details), new { id = version.SopDocumentId });
            }

            ViewBag.Document = version.SopDocument;
            ViewBag.VersionId = version.Id;
            ViewBag.VersionNumber = version.VersionNumber;
            return View(new SopVersionFormViewModel
            {
                SopDocumentId = version.SopDocumentId,
                ContentJson = version.ContentJson
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVersion(int versionId, SopVersionFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ContentJson))
            {
                ModelState.AddModelError(nameof(model.ContentJson), "İçerik boş olamaz.");
                var v = await _sop.GetVersionAsync(versionId);
                ViewBag.Document = v?.SopDocument;
                ViewBag.VersionId = versionId;
                ViewBag.VersionNumber = v?.VersionNumber;
                return View(model);
            }

            var result = await _sop.UpdateVersionContentAsync(versionId, model.ContentJson, CurrentUserId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                var v = await _sop.GetVersionAsync(versionId);
                ViewBag.Document = v?.SopDocument;
                ViewBag.VersionId = versionId;
                ViewBag.VersionNumber = v?.VersionNumber;
                return View(model);
            }

            // Plan 34.1 Save-time AI Enrichment — re-review on edit
            EnqueueEnrichment(versionId);

            TempData["Message"] = result.Message + " AI inceleme yeniden başlatıldı.";
            return RedirectToAction(nameof(Details), new { id = model.SopDocumentId });
        }

        // Versiyon yazdırma görünümü (window.print → PDF kaydet).
        [HttpGet]
        public async Task<IActionResult> Print(int versionId)
        {
            var version = await _sop.GetVersionAsync(versionId);
            if (version?.SopDocument == null) return NotFound();
            return View(new SopMyDetailsViewModel
            {
                Document = version.SopDocument,
                Version = version,
                Receipt = null
            });
        }

        // Plan 34.1 ek: PDF Import — yüklenen PDF'i text'e çevirip Draft versiyon oluştur.
        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(20_000_000)]                                      // 20 MB
        public async Task<IActionResult> ImportPdf(int id, IFormFile? pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                TempData["Message"] = "Lütfen bir PDF dosyası seçin.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var ext = Path.GetExtension(pdfFile.FileName)?.ToLowerInvariant();
            if (ext != ".pdf")
            {
                TempData["Message"] = "Sadece .pdf dosyaları kabul edilir.";
                return RedirectToAction(nameof(Details), new { id });
            }

            string html;
            try
            {
                await using var stream = pdfFile.OpenReadStream();
                html = PdfToHtml(stream, pdfFile.FileName);
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"PDF okunamadı: {ex.GetType().Name}";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                TempData["Message"] = "PDF'ten metin çıkarılamadı (taranmış görsel olabilir, OCR gerekli).";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _sop.NewVersionAsync(id, html, CurrentUserId);
            if (result.IsSuccess && result.Data is not null)
            {
                // Plan 34.1 Save-time AI Enrichment — PDF import sonrası otomatik review.
                EnqueueEnrichment(result.Data.Id);
                TempData["Message"] = $"PDF'ten Draft versiyon oluşturuldu. AI inceleme arka planda başlatıldı. Şimdi editörde düzenleyin + onaya gönderin.";
                return RedirectToAction(nameof(EditVersion), new { versionId = result.Data.Id });
            }

            TempData["Message"] = $"Versiyon oluşturulamadı: {result.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // PdfPig text → smart HTML (header tekrar strip, ALL CAPS → h2, • → ul/li).
        // SopContentSanitizer NewVersionAsync içinde HTML'i temizliyor.
        private static string PdfToHtml(Stream pdfStream, string fileName)
        {
            using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfStream);

            // 1) Tüm sayfaları satır-bazlı topla — PdfPig page.GetWords ile satır rekonstrüksiyonu.
            var allLines = new List<string>();
            foreach (var page in doc.GetPages())
            {
                allLines.AddRange(ReconstructLines(page));
            }

            // 2) Tekrar eden header/footer satırlarını tespit (sayfa sayısının yarısı veya daha fazla).
            var repeatingLines = DetectRepeatingLines(allLines, doc.NumberOfPages);

            // 3) Çevir.
            var sb = new System.Text.StringBuilder();
            sb.Append("<p><em>Kaynak: ")
              .Append(System.Net.WebUtility.HtmlEncode(fileName))
              .Append("</em></p>");

            bool inList = false;
            foreach (var raw in allLines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (repeatingLines.Contains(line)) continue;
                // Sayfa numarası başlığı (örn. "Sayfa 1") tek başına ise atla
                if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^Sayfa\s+\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;
                // Sadece sayı veya kısa sembol satırları
                if (line.Length <= 2) continue;

                // Bullet (• ya da · ya da -)
                bool isBullet = line.StartsWith("•") || line.StartsWith("·")
                    || (line.StartsWith("- ") && line.Length > 2);
                if (isBullet)
                {
                    if (!inList) { sb.Append("<ul>"); inList = true; }
                    var item = line.TrimStart('•', '·', '-', ' ');
                    sb.Append("<li>").Append(System.Net.WebUtility.HtmlEncode(item)).Append("</li>");
                    continue;
                }

                if (inList) { sb.Append("</ul>"); inList = false; }

                // ALL CAPS satır = h2 (ana başlık)
                if (IsAllCapsHeading(line))
                {
                    sb.Append("<h2>").Append(System.Net.WebUtility.HtmlEncode(line)).Append("</h2>");
                    continue;
                }

                // Title Case kısa satır + noktasız → h3 (alt başlık heuristic)
                if (IsLikelySubheading(line))
                {
                    sb.Append("<h3>").Append(System.Net.WebUtility.HtmlEncode(line)).Append("</h3>");
                    continue;
                }

                // Düz paragraf
                sb.Append("<p>").Append(System.Net.WebUtility.HtmlEncode(line)).Append("</p>");
            }
            if (inList) sb.Append("</ul>");
            return sb.ToString();
        }

        // PdfPig page.GetWords sıralı, satır yüksekliği kırılımı ile satır rekonstrüksiyonu.
        private static List<string> ReconstructLines(UglyToad.PdfPig.Content.Page page)
        {
            var lines = new List<string>();
            var words = page.GetWords()
                .OrderByDescending(w => w.BoundingBox.Bottom)                // yukarıdan aşağıya
                .ThenBy(w => w.BoundingBox.Left)
                .ToList();
            if (words.Count == 0) return lines;

            const double yTolerance = 3.0;                                    // aynı satır için y-eşiği
            var current = new List<UglyToad.PdfPig.Content.Word> { words[0] };
            for (int i = 1; i < words.Count; i++)
            {
                var prev = current[^1];
                var w = words[i];
                if (Math.Abs(prev.BoundingBox.Bottom - w.BoundingBox.Bottom) <= yTolerance)
                {
                    current.Add(w);
                }
                else
                {
                    lines.Add(string.Join(" ", current.OrderBy(x => x.BoundingBox.Left).Select(x => x.Text)));
                    current = new List<UglyToad.PdfPig.Content.Word> { w };
                }
            }
            if (current.Count > 0)
                lines.Add(string.Join(" ", current.OrderBy(x => x.BoundingBox.Left).Select(x => x.Text)));
            return lines;
        }

        // Sayfa sayısının yarısından fazla tekrar eden satır = header/footer.
        private static HashSet<string> DetectRepeatingLines(List<string> allLines, int pageCount)
        {
            if (pageCount < 2) return new();
            var freq = new Dictionary<string, int>();
            foreach (var l in allLines)
            {
                var line = l.Trim();
                if (line.Length < 5) continue;
                freq.TryGetValue(line, out var c);
                freq[line] = c + 1;
            }
            int threshold = Math.Max(2, (pageCount + 1) / 2);
            return freq.Where(kv => kv.Value >= threshold)
                       .Select(kv => kv.Key)
                       .ToHashSet();
        }

        // Türkçe ALL CAPS başlık (en az 1 harf, küçük harf yok).
        private static bool IsAllCapsHeading(string line)
        {
            if (line.Length < 3 || line.Length > 100) return false;
            bool hasLetter = false;
            foreach (var c in line)
            {
                if (char.IsLetter(c))
                {
                    hasLetter = true;
                    // Türkçe küçük harf set (i, ı, ö, ü, ç, ş, ğ ve normal)
                    if (char.IsLower(c)) return false;
                }
            }
            return hasLetter;
        }

        // Title Case, noktasız, kısa satır = alt başlık (örn. "İşe Giriş ve Kıyafet Düzeni").
        private static bool IsLikelySubheading(string line)
        {
            if (line.Length < 4 || line.Length > 80) return false;
            if (line.EndsWith(".") || line.EndsWith(":") || line.EndsWith("!") || line.EndsWith("?")) return false;
            // İlk harf büyük + bullet/sayı ile başlamamalı
            if (!char.IsUpper(line[0])) return false;
            // En az 2 kelime ve max 8 kelime
            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2 || words.Length > 8) return false;
            // Kelimelerin çoğu büyük harfle başlamalı (Title Case sinyali)
            int capWords = words.Count(w => w.Length > 0 && char.IsUpper(w[0]));
            return capWords >= words.Length - 1;                              // 1 küçük kelime ("ve", "ile") tolere et
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int versionId)
        {
            var result = await _approval.SubmitAsync(versionId, CurrentUserId);
            TempData["Message"] = result.Message;

            var version = await _sop.GetVersionAsync(versionId);
            if (version == null) return RedirectToAction(nameof(Index));
            return RedirectToAction(nameof(Details), new { id = version.SopDocumentId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int stepId, int versionId, string decision, string? comment)
        {
            var parsed = decision == "approve" ? ApprovalStatus.Approved
                       : decision == "reject"  ? ApprovalStatus.Rejected
                       : ApprovalStatus.Pending;
            if (parsed == ApprovalStatus.Pending)
            {
                TempData["Message"] = "Karar geçersiz.";
                return RedirectToAction(nameof(Details), new { id = (await _sop.GetVersionAsync(versionId))?.SopDocumentId ?? 0 });
            }

            var result = await _approval.DecideAsync(stepId, parsed, CurrentUserId, comment);
            TempData["Message"] = result.Message;

            var version = await _sop.GetVersionAsync(versionId);
            if (version == null) return RedirectToAction(nameof(Index));
            return RedirectToAction(nameof(Details), new { id = version.SopDocumentId });
        }

        private async Task PopulateLookupsAsync()
        {
            ViewBag.Categories = (await _lookup.GetValuesAsync("sopCategory"))
                .Select(v => new SelectListItemDto(v.Code, v.Label))
                .ToList();
        }

        private static SopDocumentInput MapToInput(SopFormViewModel m) =>
            new(
                FirmaId: m.FirmaId,
                Title: m.Title,
                Description: m.Description,
                Category: m.Category,
                DepartmentIds: m.DepartmentIds,
                IsCompanyWide: m.IsCompanyWide,
                OwnerUserId: m.OwnerUserId,
                ReadDeadlineDays: m.ReadDeadlineDays,
                RequiresIKApproval: m.RequiresIKApproval,
                AiAdvisorEnabled: m.AiAdvisorEnabled,
                DocumentNumber: m.DocumentNumber,
                RevisionNumber: m.RevisionNumber,
                PublishDate: m.PublishDate,
                RevisionDate: m.RevisionDate,
                EffectiveDate: m.EffectiveDate,
                PreparedBy: m.PreparedBy,
                ApprovedBy: m.ApprovedBy,
                ReviewFrequency: m.ReviewFrequency,
                Classification: m.Classification);
    }

    public sealed record SelectListItemDto(string Code, string Label);
}
