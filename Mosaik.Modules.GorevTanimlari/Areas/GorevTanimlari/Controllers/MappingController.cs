using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Modules.GorevTanimlari.Services;
using Mosaik.Modules.GorevTanimlari.ViewModels;

namespace Mosaik.Modules.GorevTanimlari.Areas.GorevTanimlari.Controllers;

// Zirve personel → görev-tanımı eşleme ekranı (kişi-düzeyi CRUD).
[Area("GorevTanimlari")]
[Authorize(Roles = "admin")]
public class MappingController : Controller
{
    private readonly MappingService _svc;
    private readonly ILogger<MappingController> _logger;

    public MappingController(MappingService svc, ILogger<MappingController> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new MappingIndexViewModel();
        try
        {
            vm.People = await _svc.GetPeopleAsync();
            vm.Docs = await _svc.DocsAsync();
        }
        catch (Exception ex)
        {
            // Hassas detay (connstring/stack) response'a sızmasın → logla, kullanıcıya sade mesaj.
            _logger.LogError(ex, "Görev tanımı eşleme ekranı yüklenemedi.");
            ViewBag.Error = "Personel/eşleme verisi yüklenemedi. IK veri kaynağı erişilebilir mi kontrol edin.";
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string? personelno, int? docId)
    {
        if (string.IsNullOrWhiteSpace(personelno) || personelno.Length > 50)
            return Json(new { ok = false, error = "Geçersiz personel." });
        try
        {
            await _svc.SaveAsync(personelno, docId, User.Identity?.Name);
            return Json(new { ok = true });
        }
        catch (DbUpdateException ex)
        {
            // Unique-violation/eşzamanlı çift kayıt vb. — logla, sade hata dön.
            _logger.LogWarning(ex, "Eşleme kaydedilemedi. Personelno={Personelno}", personelno);
            return Json(new { ok = false, error = "Kaydedilemedi (eşzamanlı değişiklik olabilir, tekrar deneyin)." });
        }
    }
}
