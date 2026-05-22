using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Domain;
using Mosaik.Core.Logging;
using Mosaik.Modules.Circular.Models;

namespace Mosaik.Modules.Circular.Services
{
    // Plan 17 Faz E — Block file upload/download/delete.
    //
    // Güvenlik:
    //  - Whitelist uzantı (AllowedExtensions)
    //  - Max 10 MB (MaxBytes)
    //  - Sanitize filename (path traversal yok)
    //  - GUID-based disk path (orijinal ad URL'de açılmaz)
    //  - Audit log: blok_dosya_upload / blok_dosya_delete / blok_dosya_download
    public class BlockFileService
    {
        public const long MaxBytes = 10L * 1024 * 1024; // 10 MB

        // Whitelist — kod + SQL'deki DosyaTuru lookup ile senkron (03_AddBlokDosyalari.sql)
        public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "pdf", "docx", "xlsx", "jpg", "jpeg", "png", "txt"
        };

        // MIME whitelist (Content-Type spoofing önle)
        private static readonly Dictionary<string, string[]> AllowedMimeByExt = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pdf"]  = new[] { "application/pdf" },
            ["docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            ["xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            ["jpg"]  = new[] { "image/jpeg" },
            ["jpeg"] = new[] { "image/jpeg" },
            ["png"]  = new[] { "image/png" },
            ["txt"]  = new[] { "text/plain" }
        };

        private readonly DbContext _db;
        private readonly IAuditLog _audit;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BlockFileService> _logger;

        public BlockFileService(DbContext db, IAuditLog audit, IWebHostEnvironment env, ILogger<BlockFileService> logger)
        {
            _db = db;
            _audit = audit;
            _env = env;
            _logger = logger;
        }

        private DbSet<BlockFile> Files => _db.Set<BlockFile>();

        public Task<List<BlockFile>> ListByBlockAsync(int blockId) =>
            Files.AsNoTracking()
                .Where(d => d.BlockId == blockId && d.IsActive)
                .OrderBy(d => d.UploadedAt)
                .ToListAsync();

        public Task<List<BlockFile>> ListByBlockIdsAsync(IEnumerable<int> blockIds) =>
            Files.AsNoTracking()
                .Where(d => blockIds.Contains(d.BlockId) && d.IsActive)
                .OrderBy(d => d.BlockId).ThenBy(d => d.UploadedAt)
                .ToListAsync();

        public Task<BlockFile?> GetAsync(int id) =>
            Files.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

        public async Task<ServiceResult<BlockFile>> UploadAsync(int blockId, IFormFile file, int uploadedById)
        {
            if (file == null || file.Length == 0)
                return ServiceResult<BlockFile>.Failure("Dosya seçilmedi.");

            if (file.Length > MaxBytes)
                return ServiceResult<BlockFile>.Failure($"Dosya 10 MB'dan büyük olamaz ({file.Length / 1024 / 1024} MB).");

            var origName = SanitizeFilename(file.FileName);
            var ext = Path.GetExtension(origName).TrimStart('.').ToLowerInvariant();

            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                return ServiceResult<BlockFile>.Failure($"İzin verilmeyen uzantı: {ext}. İzinli: {string.Join(", ", AllowedExtensions)}");

            var contentType = file.ContentType ?? "";
            if (AllowedMimeByExt.TryGetValue(ext, out var allowedMimes) && !allowedMimes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
                return ServiceResult<BlockFile>.Failure($"Dosya türü uzantıyla uyumsuz (Content-Type: {contentType}).");

            // Disk path: App_Data/blocks/{yyyy}/{MM}/{guid}.{ext}
            // Plan 41 HIGH-1 fix: wwwroot → ContentRoot/App_Data (UseStaticFiles auth bypass önlendi)
            var now = DateTime.UtcNow;
            var folderRel = Path.Combine("App_Data", "blocks", now.ToString("yyyy"), now.ToString("MM"));
            var folderAbs = Path.Combine(_env.ContentRootPath, folderRel);
            Directory.CreateDirectory(folderAbs);

            var guidName = $"{Guid.NewGuid():N}.{ext}";
            var fileAbs = Path.Combine(folderAbs, guidName);
            var fileRel = Path.Combine(folderRel, guidName).Replace('\\', '/');

            try
            {
                await using var stream = File.Create(fileAbs);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BlockFileService.UploadAsync disk write failed blockId={BlockId} file={File}", blockId, origName);
                return ServiceResult<BlockFile>.Failure("Dosya kaydedilemedi.");
            }

            var record = new BlockFile
            {
                BlockId = blockId,
                FileName = origName,
                FilePath = fileRel,
                Extension = ext,
                MimeType = contentType,
                FileSize = file.Length,
                UploadedById = uploadedById,
                UploadedAt = now,
                IsActive = true
            };
            Files.Add(record);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "block_file_upload",
                targetType: "blockFile",
                targetKey: record.Id.ToString(),
                description: $"{origName} ({file.Length} byte) block={blockId}'a yüklendi");

            return ServiceResult<BlockFile>.Ok(record, "Dosya yüklendi.");
        }

        public async Task<ServiceResult> DeleteAsync(int id, int deletedById)
        {
            var file = await Files.FirstOrDefaultAsync(d => d.Id == id && d.IsActive);
            if (file == null) return ServiceResult.Failure("Dosya bulunamadı.");

            file.IsActive = false;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "block_file_delete",
                targetType: "blockFile",
                targetKey: id.ToString(),
                description: $"{file.FileName} silindi (silen userId={deletedById})");

            // Disk dosyası kalır (geri yükleme için). Kalıcı silme için ileride cron job.
            return ServiceResult.Ok("Dosya silindi.");
        }

        // null döner → path traversal tespit edildi (BlockController NotFound döner)
        public string? GetAbsolutePath(BlockFile file)
        {
            var allowedRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data", "blocks"));
            var candidate   = Path.GetFullPath(Path.Combine(_env.ContentRootPath,
                file.FilePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!candidate.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("BlockFile {Id} FilePath outside allowed root: {FilePath}", file.Id, file.FilePath);
                return null;
            }
            return candidate;
        }

        // Sanitize: path traversal + non-ASCII path char'ları temizle, sadece file adı bırak
        private static string SanitizeFilename(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "file";
            // Path bileşenlerini at, sadece son adı al
            var name = Path.GetFileName(raw);
            // Tehlikeli karakterleri çıkar
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            // Çift uzantı ve gizli file engelleme
            name = name.TrimStart('.');
            if (name.Length > 200) name = name.Substring(0, 200);
            return string.IsNullOrEmpty(name) ? "file" : name;
        }
    }
}
