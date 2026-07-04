using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Domain;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    public sealed record FormSubmissionInput(
        int FormDefinitionId,
        int FirmaId,
        IReadOnlyDictionary<string, string?> Values,
        int? SubmittedById,
        string? SubmitterEmail,
        string? SubmitterPhone,
        string? SubmitterIp,
        string? SubmitterUserAgent,
        int? PublicTokenId);

    // Plan 41 Faz 1 — submission save + FormVersionId bind (§4.6 ZORUNLU). Workflow/ProcessInstance
    // trigger stub (Plan 36/42 — sonraki fazlarda gerçek çağrı eklenir).
    // Faz 4 — File/Signature alanları: base64 dataURL decode → magic-byte doğrula → disk yaz → ValueFileId.
    public class FormSubmissionService(DbContext db, FormValidationService validation, FormFileStorage fileStorage, FormEncryptionService encryption, ILogger<FormSubmissionService> logger)
    {
        // silent-failure-hunter HIGH — alan-bazlı hata dict'i bu koda taşınır (JSON-encoded
        // Message); controller/JS ayırt edip survey-core question.addError'a bağlar.
        public const string FieldValidationErrorCode = "field_validation";

        public async Task<ServiceResult<int>> SubmitAsync(FormSubmissionInput input, CancellationToken ct = default)
        {
            var def = await db.Set<FormDefinition>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == input.FormDefinitionId && d.FirmaId == input.FirmaId && d.Status == 1, ct);
            if (def == null)
                return ServiceResult<int>.Failure("Form bulunamadı veya yayında değil.");

            var latestVersion = await db.Set<FormDefinitionVersion>().AsNoTracking()
                .Where(v => v.FormDefinitionId == def.Id)
                .OrderByDescending(v => v.Version)
                .FirstOrDefaultAsync(ct);
            if (latestVersion == null)
                return ServiceResult<int>.Failure("Form yayınlanmış ama sürüm kaydı yok — tutarsız durum.");

            var errors = await validation.ValidateAsync(def.Id, input.Values, ct);
            if (errors.Count > 0)
                return ServiceResult<int>.Failure(JsonSerializer.Serialize(errors), FieldValidationErrorCode);

            // Login VEYA geçerli public token yoksa reddet. Public token (PublicTokenId) kimlik
            // doğrulama yerine geçer — public form IsAnonymous olmasa da token ile submit kabul.
            if (!def.IsAnonymous && input.SubmittedById == null && input.PublicTokenId == null)
                return ServiceResult<int>.Failure("Bu form anonim submit'e izin vermiyor, giriş yapmalısınız.");

            var fields = await db.Set<FormField>().AsNoTracking()
                .Where(f => f.FormDefinitionId == def.Id)
                .ToListAsync(ct);

            // Public/anonim (token ile veya IsAnonymous) daha sıkı boyut limiti (advisor conf 80).
            var maxBytes = def.IsAnonymous || input.PublicTokenId != null
                ? FormFileStorage.PublicMaxBytes
                : FormFileStorage.InternalMaxBytes;

            // Pass 1: File/Signature alanlarını DECODE et (disk'e dokunmadan) — bir alan geçersizse
            // hiçbir dosya yazılmadan field_validation döner (orphan dosya bırakma).
            var decoded = new List<(FormField Field, FormFileStorage.DecodedFile File)>();
            var fileErrors = new Dictionary<string, string>();
            foreach (var field in fields)
            {
                if (field.FieldType is not (FormFieldType.File or FormFieldType.Signature))
                    continue;
                // G3 AUTHORITATIVE: koşulu sağlanmayan (gizli) alanın dosyası diske yazılmaz/saklanmaz —
                // kötü client gizli alana değer basamaz (anonimlik/koşul bütünlüğü server'da zorlanır).
                if (!FormConditionEvaluator.IsVisible(field, input.Values))
                    continue;
                if (!input.Values.TryGetValue(field.FieldKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                    continue;

                var r = field.FieldType == FormFieldType.Signature
                    ? fileStorage.DecodeSignature(raw, maxBytes)
                    : fileStorage.DecodeFileField(raw, maxBytes);
                if (!r.IsSuccess)
                    fileErrors[field.FieldKey] = r.Message;
                else
                    decoded.Add((field, r.Data));
            }
            if (fileErrors.Count > 0)
                return ServiceResult<int>.Failure(JsonSerializer.Serialize(fileErrors), FieldValidationErrorCode);

            var submission = new FormSubmission
            {
                FormDefinitionId = def.Id,
                FirmaId = input.FirmaId,
                FormVersionId = latestVersion.Id,
                SubmittedById = input.SubmittedById,
                SubmitterEmail = input.SubmitterEmail,
                SubmitterPhone = input.SubmitterPhone,
                SubmitterIp = input.SubmitterIp,
                SubmitterUserAgent = input.SubmitterUserAgent,
                PublicTokenId = input.PublicTokenId,
                Status = 0
            };

            // Pass 2a: dosya olmayan alanlar.
            foreach (var field in fields)
            {
                if (field.FieldType is FormFieldType.Hidden or FormFieldType.Section)
                    continue;
                if (field.FieldType is FormFieldType.File or FormFieldType.Signature)
                    continue;
                // G3 AUTHORITATIVE: gizli alanın değeri saklanmaz (validation zaten atlar; burada da
                // persist edilmez — aksi halde koşullu-gizli alana basılan değer sızardı).
                if (!FormConditionEvaluator.IsVisible(field, input.Values))
                {
                    // Gizli alana gönderilen dolu değer atılıyor = tamper/client-drift sinyali — iz bırak.
                    if (input.Values.TryGetValue(field.FieldKey, out var dropped) && !string.IsNullOrWhiteSpace(dropped))
                        logger.LogWarning("Forms: gizli alana gönderilen değer atıldı. form={FormId} field={FieldKey}", def.Id, field.FieldKey);
                    continue;
                }
                if (!input.Values.TryGetValue(field.FieldKey, out var raw) || raw == null)
                    continue;

                submission.Values.Add(BuildFieldValue(field, raw, def.IsEncrypted));
            }

            // Pass 2b: decode edilmiş dosyaları diske yaz + submission graph'ına bağla + kaydet.
            // Write loop + SaveChanges TEK try içinde (silent-failure F3): mid-batch WriteToDisk IOException'ı
            // da orphan temizliğine dahil olur — yazılmış dosyalar `written`'da, catch hepsini siler.
            var written = new List<FormSubmissionFile>();
            try
            {
                foreach (var (field, file) in decoded)
                {
                    var stored = await fileStorage.WriteToDiskAsync(file, input.FirmaId, field.FieldKey, ct);
                    written.Add(stored);
                    submission.Files.Add(stored);
                    submission.Values.Add(new FormSubmissionFieldValue
                    {
                        FormFieldId = field.Id,
                        FieldKey = field.FieldKey,
                        File = stored
                    });
                }

                db.Set<FormSubmission>().Add(submission);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception)
            {
                fileStorage.TryDeleteAll(written); // disk-write veya DB başarısız — yazılmış dosyaları temizle
                throw;
            }

            // Plan 36/42 — workflow/ProcessInstance trigger stub. Sonraki fazlarda gerçek çağrı.

            return ServiceResult<int>.Ok(submission.Id);
        }

        private FormSubmissionFieldValue BuildFieldValue(FormField field, string raw, bool encrypt)
        {
            var value = new FormSubmissionFieldValue { FormFieldId = field.Id, FieldKey = field.FieldKey };
            // Form şifreli ise (ihbar/DSAR): değer ValueText'e ŞİFRELİ blob olarak yazılır, tipli kolonlar
            // null kalır (aksi halde ValueNumber/Date/Bool düz metin sızdırır). Decrypt = yetki-gated (Gap 2).
            if (encrypt)
            {
                // Boş/whitespace → hiçbir kolon set etme (typed path ile tutarlı; boş değeri şifreleyip
                // IsEncrypted=true kirliliği yaratma — security-reviewer H-2).
                if (string.IsNullOrEmpty(raw)) return value;
                value.ValueText = encryption.Encrypt(raw);
                value.IsEncrypted = true;
                return value;
            }
            switch (field.FieldType)
            {
                case FormFieldType.Number:
                    // InvariantCulture ZORUNLU — survey-core "." ondalık gönderir; tr-TR host "."'ı binlik
                    // ayraç sanıp "3.5"→35 yapardı (code-reviewer locale bug; Faz 3 _elapsed'ın aynısı).
                    value.ValueNumber = decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
                    break;
                case FormFieldType.Date:
                case FormFieldType.DateTime:
                    value.ValueDate = DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
                        ? d.ToUniversalTime() : null;
                    break;
                case FormFieldType.Checkbox:
                    value.ValueBool = bool.TryParse(raw, out var b) && b;
                    break;
                default:
                    value.ValueText = raw;
                    break;
            }
            return value;
        }
    }
}
