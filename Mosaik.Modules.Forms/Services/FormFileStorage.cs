using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Domain;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 41 Faz 4 — form eki + imza disk storage. Documents guard pattern kopyalandı
    // (advisor 2026-07-01: Forms ana projeye compile bağlanamaz, ADR-002 → izole storage).
    // İki aşama: Decode (saf — magic-byte+boyut+base64, DİSK'e dokunmaz) → WriteToDisk (I/O).
    // Ayrım orphan-on-failure'ı önler: SubmissionService önce tüm alanları Decode eder, hepsi
    // geçerliyse WriteToDisk çağırır (bir alan geçersizse hiçbir dosya diske yazılmaz).
    public class FormFileStorage(IWebHostEnvironment env, FormEncryptionService encryption, ILogger<FormFileStorage> logger)
    {
        public const int PublicMaxBytes = 10 * 1024 * 1024;   // anonim/public — sıkı (advisor conf 80)
        public const int InternalMaxBytes = 50 * 1024 * 1024; // login'li dahili form

        private static readonly JsonSerializerOptions JsonCase = new() { PropertyNameCaseInsensitive = true };

        public sealed record DecodedFile(byte[] Bytes, string Ext, string? Mime, string FileName);

        // survey-core file question değeri: [{ "name","type","content":"data:...;base64,..." }].
        // İlk dosyayı alır (v1 tek dosya). Boş/bozuk → hata.
        public ServiceResult<DecodedFile> DecodeFileField(string rawValue, int maxBytes)
        {
            string dataUrl;
            string fileName;
            try
            {
                var node = JsonSerializer.Deserialize<List<SurveyFileItem>>(rawValue, JsonCase);
                var first = node?.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.Content));
                if (first == null)
                    return ServiceResult<DecodedFile>.Failure("Dosya okunamadı.");
                dataUrl = first.Content!;
                fileName = string.IsNullOrWhiteSpace(first.Name) ? "dosya" : first.Name!;
            }
            catch (JsonException)
            {
                return ServiceResult<DecodedFile>.Failure("Dosya biçimi geçersiz.");
            }
            return DecodeDataUrl(dataUrl, fileName, maxBytes, requirePng: false);
        }

        // signaturepad değeri: doğrudan "data:image/png;base64,..." dataURL string (PNG zorunlu).
        public ServiceResult<DecodedFile> DecodeSignature(string dataUrl, int maxBytes)
            => DecodeDataUrl(dataUrl, "imza.png", maxBytes, requirePng: true);

        private ServiceResult<DecodedFile> DecodeDataUrl(string dataUrl, string fileName, int maxBytes, bool requirePng)
        {
            var comma = dataUrl.IndexOf(',');
            if (!dataUrl.StartsWith("data:", StringComparison.Ordinal) || comma < 0)
                return ServiceResult<DecodedFile>.Failure("Geçersiz dosya verisi.");

            var meta = dataUrl.AsSpan(5, comma - 5).ToString();   // "<mime>;base64"
            var mime = meta.Split(';')[0];
            byte[] bytes;
            try { bytes = Convert.FromBase64String(dataUrl[(comma + 1)..]); }
            catch (FormatException) { return ServiceResult<DecodedFile>.Failure("Dosya kodlaması bozuk."); }

            if (bytes.Length == 0)
                return ServiceResult<DecodedFile>.Failure("Dosya boş.");
            if (bytes.Length > maxBytes)
                return ServiceResult<DecodedFile>.Failure($"Dosya boyutu {maxBytes / (1024 * 1024)} MB sınırını aşıyor.");

            // Magic-byte — client MIME'a GÜVENİLMEZ (security). İmza yalnız PNG; ek: PDF/PNG/JPG/Office.
            var ext = SniffExtension(bytes);
            if (ext == null || (requirePng && ext != ".png"))
            {
                logger.LogWarning("Form file upload rejected (magic-byte). requirePng={Req} mime={Mime} len={Len}",
                    requirePng, mime, bytes.Length);
                return ServiceResult<DecodedFile>.Failure(requirePng
                    ? "İmza görseli geçersiz."
                    : "Yalnızca PDF, PNG, JPG ve Office belgeleri yüklenebilir.");
            }

            return ServiceResult<DecodedFile>.Ok(new DecodedFile(bytes, ext, mime, SafeFileName(fileName, ext)));
        }

        // Diske yaz — GUID ad + path-traversal guard (Documents pattern, DocumentsController.cs:126-140).
        // Entity döner ama DB'ye eklenmez (SubmissionService submission graph'ına bağlar).
        // Plan 57 A2-#1: encrypt=true (form IsEncrypted — ihbar) ise içerik DataProtection ile
        // şifreli yazılır; FileSize orijinal boyut kalır (görüntüleme), disk boyutu farklıdır.
        public async Task<FormSubmissionFile> WriteToDiskAsync(
            DecodedFile file, int firmaId, string fieldKey, bool encrypt = false, CancellationToken ct = default)
        {
            var uploadDir = Path.Combine(env.ContentRootPath, "App_Data", "forms", firmaId.ToString());
            Directory.CreateDirectory(uploadDir);

            var safeName = $"{Guid.NewGuid():N}{file.Ext}";
            var diskPath = Path.Combine(uploadDir, safeName);
            var resolved = Path.GetFullPath(diskPath);
            // Trailing separator — "forms/1" prefix'i "forms/10" gibi kardeş dizinle eşleşmesin (security M-1).
            var rootPrefix = Path.GetFullPath(uploadDir) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Dosya yolu doğrulaması başarısız.");

            var payload = encrypt ? encryption.EncryptBytes(file.Bytes) : file.Bytes;
            await System.IO.File.WriteAllBytesAsync(diskPath, payload, ct);
            var relPath = Path.GetRelativePath(env.ContentRootPath, diskPath).Replace('\\', '/');

            return new FormSubmissionFile
            {
                FirmaId = firmaId,
                FieldKey = fieldKey,
                FileName = file.FileName,
                DiskPath = relPath,
                FileSize = file.Bytes.LongLength,
                MimeType = file.Mime,
                IsEncrypted = encrypt
            };
        }

        // Rollback — SaveChanges başarısızsa yazılmış dosyaları temizle (orphan bırakma).
        public void TryDeleteAll(IEnumerable<FormSubmissionFile> files)
        {
            foreach (var f in files)
            {
                try
                {
                    var abs = Path.Combine(env.ContentRootPath, f.DiskPath.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(abs)) System.IO.File.Delete(abs);
                }
                // Cleanup rollback içinde çağrılır — ikincil exception birincil hatayı MASKELEMESİN
                // (silent-failure F4): tüm exception'lar yutulur + loglanır, propagate edilmez.
                catch (Exception ex) { logger.LogWarning(ex, "Form file cleanup failed: {Path}", f.DiskPath); }
            }
        }

        private static string? SniffExtension(byte[] b)
        {
            if (b.Length >= 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46) return ".pdf";           // %PDF
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";           // PNG
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";                           // JPEG
            if (b.Length >= 4 && b[0] == 0x50 && b[1] == 0x4B && b[2] == 0x03 && b[3] == 0x04) return ".docx";          // ZIP/Office (docx/xlsx)
            return null;
        }

        private static string SafeFileName(string name, string ext)
        {
            var baseName = Path.GetFileNameWithoutExtension(name);
            foreach (var c in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(c, '_');
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "dosya";
            if (baseName.Length > 100) baseName = baseName[..100];
            return baseName + ext;
        }

        private sealed record SurveyFileItem(string? Name, string? Type, string? Content);
    }
}
