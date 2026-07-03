using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mosaik.Modules.GorevTanimlari.Services;

namespace Mosaik.Modules.GorevTanimlari.Areas.GorevTanimlari.Controllers;

// Organizasyon şeması — bizim tasarım omurgası (OrgPositions) + görev tanımı detayı.
// index.html interaktif chart'ının Mosaik'e taşınmış hâli. Görüntüleme herkese açık;
// düzenleme (Rename/Add/Delete/Move/SaveDefinition) admin-only + AntiForgery + DB yazma.
[Area("GorevTanimlari")]
[Authorize]
public class SemaController : Controller
{
    private readonly SemaService _svc;
    private readonly SemaEditService _edit;
    private readonly ILogger<SemaController> _logger;

    public SemaController(SemaService svc, SemaEditService edit, ILogger<SemaController> logger)
    {
        _svc = svc;
        _edit = edit;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var json = "{\"org\":[],\"advisors\":[]}";
        try { json = await _svc.BuildTreeJsonAsync(ct); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Organizasyon şeması verisi yüklenemedi.");
            ViewBag.Error = "Şema verisi yüklenemedi. Pozisyon/görev tanımı verisi erişilebilir mi kontrol edin.";
        }
        ViewBag.SemaJson = json;
        return View();
    }

    // Düzenleme sonrası JS taze veri çeker (tam sayfa reload yerine).
    [HttpGet]
    public async Task<IActionResult> Data(CancellationToken ct)
    {
        try { return Content(await _svc.BuildTreeJsonAsync(ct), "application/json"); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şema verisi (Data) yüklenemedi.");
            return Content("{\"org\":[],\"advisors\":[]}", "application/json");
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(int id, string? title, string? holderName, CancellationToken ct)
    {
        try
        {
            var (ok, err) = await _edit.RenameAsync(id, title, holderName, User.Identity?.Name, ct);
            return Json(new { ok, error = err });
        }
        catch (Exception ex) { return Fail(ex, "Rename", id); }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int? parentId, string? title, string? holderName, CancellationToken ct)
    {
        try
        {
            var (ok, err, newId) = await _edit.AddAsync(parentId, title, holderName, User.Identity?.Name, ct);
            return Json(new { ok, error = err, id = newId });
        }
        catch (Exception ex) { return Fail(ex, "Add", parentId ?? 0); }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var (ok, err) = await _edit.DeleteAsync(id, User.Identity?.Name, ct);
            return Json(new { ok, error = err });
        }
        catch (Exception ex) { return Fail(ex, "Delete", id); }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int? newParentId, CancellationToken ct)
    {
        try
        {
            var (ok, err) = await _edit.MoveAsync(id, newParentId, User.Identity?.Name, ct);
            return Json(new { ok, error = err });
        }
        catch (Exception ex) { return Fail(ex, "Move", id); }
    }

    public sealed class DefinitionDto
    {
        public int OrgPositionId { get; set; }
        public List<string> Paragraphs { get; set; } = new();
        public List<KpiInput> Kpi { get; set; } = new();
    }
    public sealed class KpiInput
    {
        public string Text { get; set; } = "";
        public bool Active { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDefinition([FromBody] DefinitionDto dto, CancellationToken ct)
    {
        if (dto is null) return Json(new { ok = false, error = "Geçersiz istek." });
        try
        {
            var kpi = dto.Kpi.Select(k => new SemaEditService.KpiDto(k.Text, k.Active)).ToList();
            var (ok, err) = await _edit.SaveDefinitionAsync(dto.OrgPositionId, dto.Paragraphs, kpi, User.Identity?.Name, ct);
            return Json(new { ok, error = err });
        }
        catch (Exception ex) { return Fail(ex, "SaveDefinition", dto.OrgPositionId); }
    }

    private IActionResult Fail(Exception ex, string op, int key)
    {
        _logger.LogError(ex, "Şema düzenleme hatası: {Op} key={Key}", op, key);
        return Json(new { ok = false, error = "İşlem kaydedilemedi. Tekrar deneyin veya sistem yöneticisine bildirin." });
    }
}
