using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 41 Faz 1 — server-side authoritative validation orchestration (DB'den field'ları
    // çeker, FormFieldValidator'a delege eder — client survey-core validation'ına güvenilmez).
    // Alan-bazlı hata dict döner (silent-failure-hunter HIGH — tek stringe sıkıştırma yerine
    // survey-core question.addError ile alan-altına yerleştirilebilsin).
    public class FormValidationService(DbContext db)
    {
        public async Task<Dictionary<string, string>> ValidateAsync(
            int formDefinitionId, IReadOnlyDictionary<string, string?> values, CancellationToken ct = default)
        {
            var fields = await db.Set<FormField>().AsNoTracking()
                .Where(f => f.FormDefinitionId == formDefinitionId)
                .ToListAsync(ct);

            var errors = new Dictionary<string, string>();
            foreach (var field in fields)
            {
                values.TryGetValue(field.FieldKey, out var raw);
                var error = FormFieldValidator.Validate(field, raw);
                if (error != null)
                    errors[field.FieldKey] = error;
            }
            return errors;
        }
    }
}
