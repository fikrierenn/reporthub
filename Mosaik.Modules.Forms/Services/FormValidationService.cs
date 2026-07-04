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
                // Plan 56 M-A G3 — koşullu görünürlük AUTHORITATIVE: koşulu diğer submitted değerlerle
                // yeniden değerlendir. Görünmüyorsa required + format doğrulama atlanır (kötü client
                // "gizliydi" diye zorunlu alanı boş bırakamaz / gizli alana değer basamaz). Bozuk koşul
                // → görünür varsayılır (fail-closed, required zorlanır).
                if (!FormConditionEvaluator.IsVisible(field, values))
                    continue;

                values.TryGetValue(field.FieldKey, out var raw);
                var error = FormFieldValidator.Validate(field, raw);
                if (error != null)
                    errors[field.FieldKey] = error;
            }
            return errors;
        }
    }
}
