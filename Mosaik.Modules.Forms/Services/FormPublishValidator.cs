using System.Text.Json;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 41 Faz 2 — Taslak→Yayında geçişi. DataElement mapping OPTIONAL (§4.3 düzeltme,
    // 2026-06-29) — warning banner üretir, blocker DEĞİL. Saf/testable.
    public static class FormPublishValidator
    {
        // En az 1 veri-taşıyan alan (section/hidden hariç) olmalı — boş form yayınlanamaz.
        public static bool HasSubmittableField(IEnumerable<FormField> fields) =>
            fields.Any(f => f.FieldType is not (FormFieldType.Section or FormFieldType.Hidden));

        // BLOCKER: seçenek-gerektiren alan (Select/Radio/MultiSelect) en az 1 geçerli seçeneğe
        // sahip olmalı — aksi halde yayında boş/seçilemez dropdown render edilirdi (silent-failure).
        public static List<string> GetEmptyChoiceErrors(IEnumerable<FormField> fields)
        {
            var errors = new List<string>();
            foreach (var f in fields.Where(f => FormLabels.RequiresOptions(f.FieldType)))
            {
                if (!HasAtLeastOneChoice(f.Options))
                    errors.Add($"'{f.Label}' seçenekli bir alan ama hiç seçenek tanımlı değil.");
            }
            return errors;
        }

        private static bool HasAtLeastOneChoice(string? optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson))
                return false;
            try
            {
                var arr = JsonSerializer.Deserialize<string[]>(optionsJson);
                return arr is { Length: > 0 };
            }
            catch (JsonException)
            {
                return false; // bozuk JSON = seçenek yok say (fail-closed)
            }
        }

        // Bağlanmamış alan sayısı — Yayında geçişini bloklamaz, sadece admin UI'da uyarı gösterilir.
        public static List<string> GetUnmappedFieldWarnings(IEnumerable<FormField> fields, ISet<int> mappedFieldIds)
        {
            return fields
                .Where(f => f.FieldType is not (FormFieldType.Section or FormFieldType.Hidden))
                .Where(f => !mappedFieldIds.Contains(f.Id))
                .Select(f => $"'{f.Label}' bir KVKK DataElement'e bağlı değil (opsiyonel).")
                .ToList();
        }
    }
}
