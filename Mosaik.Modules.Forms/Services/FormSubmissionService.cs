using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
    public class FormSubmissionService(DbContext db, FormValidationService validation)
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

            if (!def.IsAnonymous && input.SubmittedById == null)
                return ServiceResult<int>.Failure("Bu form anonim submit'e izin vermiyor, giriş yapmalısınız.");

            var fields = await db.Set<FormField>().AsNoTracking()
                .Where(f => f.FormDefinitionId == def.Id)
                .ToListAsync(ct);

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

            foreach (var field in fields)
            {
                if (!input.Values.TryGetValue(field.FieldKey, out var raw) || raw == null)
                    continue;
                if (field.FieldType is FormFieldType.Hidden or FormFieldType.Section)
                    continue;

                submission.Values.Add(BuildFieldValue(field, raw));
            }

            db.Set<FormSubmission>().Add(submission);
            await db.SaveChangesAsync(ct);

            // Plan 36/42 — workflow/ProcessInstance trigger stub. Sonraki fazlarda gerçek çağrı.

            return ServiceResult<int>.Ok(submission.Id);
        }

        private static FormSubmissionFieldValue BuildFieldValue(FormField field, string raw)
        {
            var value = new FormSubmissionFieldValue { FormFieldId = field.Id, FieldKey = field.FieldKey };
            switch (field.FieldType)
            {
                case FormFieldType.Number:
                    value.ValueNumber = decimal.TryParse(raw, out var n) ? n : null;
                    break;
                case FormFieldType.Date:
                case FormFieldType.DateTime:
                    value.ValueDate = DateTime.TryParse(raw, out var d) ? d.ToUniversalTime() : null;
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
