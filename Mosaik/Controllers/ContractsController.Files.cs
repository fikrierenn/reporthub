using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Controllers
{
    public partial class ContractsController
    {
        // Magic byte signatures for upload validation (H-3 hardening)
        private static readonly byte[] PdfMagic        = [0x25, 0x50, 0x44, 0x46];       // %PDF
        private static readonly byte[] OfficeOpenMagic = [0x50, 0x4B, 0x03, 0x04];       // PK ZIP (docx)
        private static readonly byte[] OldOfficeMagic  = [0xD0, 0xCF, 0x11, 0xE0];       // OLE compound (.doc)

        // POST /Contracts/UploadFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(int contractId, IFormFile file)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var contract = await _db.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId && firmas.Contains(c.FirmaId));

            if (contract is null) return NotFound();

            if (file is null || file.Length == 0)
            {
                TempData["Error"] = "Dosya seçilmedi.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            if (file.Length > 20 * 1024 * 1024)
            {
                TempData["Error"] = "Dosya 20 MB sınırını aşıyor.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            var allowedMime = new[] { "application/pdf", "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

            if (!allowedMime.Contains(file.ContentType))
            {
                TempData["Error"] = "Yalnızca PDF ve Word dosyaları kabul edilir.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            // Magic byte verification — extension/MIME spoof'a karşı (H-3 hardening)
            if (!await VerifyMagicBytesAsync(file))
            {
                TempData["Error"] = "Dosya içeriği bildirilen tipiyle eşleşmiyor.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            // App_Data/contracts/{firmaId}/{contractId}/ — wwwroot DEĞİL (UseStaticFiles ile direkt erişim engelli).
            var uploadDir = Path.Combine(_env.ContentRootPath, "App_Data", "contracts",
                contract.FirmaId.ToString(), contractId.ToString());
            Directory.CreateDirectory(uploadDir);

            var safeName = SanitizeFileName(file.FileName);
            var diskPath = Path.Combine(uploadDir, safeName);
            var resolvedPath = Path.GetFullPath(diskPath);
            var rootPrefix = Path.GetFullPath(uploadDir);
            if (!resolvedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Contract upload path traversal attempt blocked: {Resolved}", resolvedPath);
                TempData["Error"] = "Dosya adı geçersiz.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            try
            {
                await using (var fs = new FileStream(diskPath, FileMode.Create))
                    await file.CopyToAsync(fs);
            }
            catch (IOException ioex)
            {
                _logger.LogError(ioex, "Contract file upload disk write failed: {Path}", diskPath);
                TempData["Error"] = "Dosya kaydedilemedi (disk hatası). Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            var contractFile = new ContractFile
            {
                FirmaId = contract.FirmaId,
                ContractId = contractId,
                FileName = file.FileName,
                FilePath = Path.GetRelativePath(_env.ContentRootPath, diskPath).Replace('\\', '/'),
                FileSize = file.Length,
                MimeType = file.ContentType,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Username
            };

            _db.ContractFiles.Add(contractFile);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Contract file DB save failed; cleaning orphan: {Path}", diskPath);
                try { System.IO.File.Delete(diskPath); }
                catch (IOException cleanupEx) { _logger.LogWarning(cleanupEx, "Orphan cleanup failed: {Path}", diskPath); }
                TempData["Error"] = "Dosya kaydı tamamlanamadı. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "contract_file_upload",
                TargetType = "contract_file",
                TargetKey = contractFile.Id.ToString(),
                Description = $"Sözleşme dosyası yüklendi: {file.FileName} ({file.Length} bayt)",
                IsSuccess = true,
                NewValuesJson = AuditLogService.ToJson(new { contractFile.Id, contractFile.FirmaId, contractFile.ContractId, contractFile.FileSize, contractFile.MimeType })
            });

            TempData["Message"] = "Dosya yüklendi.";
            return RedirectToAction(nameof(Details), new { id = contractId });
        }

        // GET /Contracts/Download/5 — yetkili kullanıcı için dosya indir (App_Data private storage).
        public async Task<IActionResult> Download(int id)
        {
            var firmas = AccessibleFirmaIds;
            if (firmas.Count == 0) return Forbid();

            var contractFile = await _db.ContractFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id && firmas.Contains(f.FirmaId));

            if (contractFile is null) return NotFound();

            var fullPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, contractFile.FilePath.Replace('/', Path.DirectorySeparatorChar)));
            var allowedRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data", "contracts"));

            if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("ContractFile.FilePath outside allowed root: {FilePath}", contractFile.FilePath);
                return NotFound();
            }

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("ContractFile {Id} disk path missing: {Path}", id, fullPath);
                return NotFound();
            }

            return PhysicalFile(fullPath, contractFile.MimeType, contractFile.FileName);
        }

        private static async Task<bool> VerifyMagicBytesAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var header = new byte[4];
            var read = await stream.ReadAsync(header.AsMemory(0, 4));
            if (read < 4) return false;
            return StartsWith(header, PdfMagic) || StartsWith(header, OfficeOpenMagic) || StartsWith(header, OldOfficeMagic);
        }

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            if (data.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
                if (data[i] != prefix[i]) return false;
            return true;
        }

        private static string SanitizeFileName(string original)
        {
            if (string.IsNullOrWhiteSpace(original)) original = "file";
            var name = Path.GetFileName(original);
            foreach (var ch in Path.GetInvalidFileNameChars())
                name = name.Replace(ch, '_');
            name = name.Replace('\0', '_');
            if (name.Length > 100) name = name[..100];
            var ext = Path.GetExtension(name);
            var stem = Path.GetFileNameWithoutExtension(name);
            return stem + "_" + Guid.NewGuid().ToString("N")[..8] + ext;
        }
    }
}
