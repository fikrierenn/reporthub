using Microsoft.AspNetCore.Mvc;

namespace Mosaik.Controllers
{
    public partial class AiController
    {
        private const long WizardMaxBytes = 30 * 1024 * 1024; // 30 MB
        private static readonly byte[] PdfMagic = [0x25, 0x50, 0x44, 0x46]; // %PDF

        // POST /Ai/WizardStart — PDF yükle, AI extraction başlat, jobId döner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WizardStart(IFormFile? file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "Dosya seçilmedi." });

            if (file.Length > WizardMaxBytes)
                return BadRequest(new { error = "Dosya 30 MB sınırını aşıyor." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
                return BadRequest(new { error = "Sadece PDF dosyaları desteklenir." });

            // Magic byte kontrolü — %PDF (kısa okuma silent fail riskine karşı ReadExactlyAsync)
            var header = new byte[4];
            try
            {
                using var peek = file.OpenReadStream();
                await peek.ReadExactlyAsync(header);
            }
            catch (EndOfStreamException)
            {
                return BadRequest(new { error = "Geçersiz veya bozuk PDF dosyası." });
            }

            if (!header.SequenceEqual(PdfMagic))
                return BadRequest(new { error = "Geçersiz PDF dosyası." });

            // App_Data/wizard_temp/ — wwwroot dışı, public erişim yok
            var tempDir = Path.Combine(_env.ContentRootPath, "App_Data", "wizard_temp");
            Directory.CreateDirectory(tempDir);

            var tempFile = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + ".pdf");
            using (var fs = System.IO.File.Create(tempFile))
                await file.CopyToAsync(fs);

            var userId = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
            string jobId;
            try
            {
                jobId = _wizard.Start(tempFile, "application/pdf", userId);
            }
            catch (InvalidOperationException ioex)
            {
                // Start path-traversal vs. fırlatırsa temp dosya orphan kalmasın.
                try { if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile); } catch { }
                return BadRequest(new { error = ioex.Message });
            }

            return Ok(new { jobId });
        }

        // GET /Ai/WizardStatus?jobId=... — polling endpoint
        [HttpGet]
        public IActionResult WizardStatus(string? jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return BadRequest(new { error = "jobId gerekli." });

            var status = _wizard.GetStatus(jobId);
            if (status is null)
                return NotFound(new { error = "İş bulunamadı veya süresi doldu (10 dk TTL)." });

            return Ok(status);
        }
    }
}
