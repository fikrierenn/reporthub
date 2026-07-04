using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    public sealed record FormFieldInput(
        string FieldKey, string Label, string? HelpText, byte FieldType, bool IsRequired,
        string? Options, string? ValidationRules, string? DefaultValue, string? Placeholder,
        string? ConditionalLogic = null);

    // Plan 41 Faz 2 — liste-tabanlı builder (drag-drop yok). Alan ekle/güncelle/sil/sırala.
    // Faz 4: KVKK DataElement eşleme (opsiyonel — cross-modül lookup ile doğrulanır).
    public class FormFieldService(DbContext db, DataElementLookupService dataElements)
    {
        // Plan 56 M-A G3 danışman HIGH önkoşulu: FieldKey survey `name` VE conditional visibleIf
        // referansı olarak ham basılıyor (FormSchemaBuilder.cs:25). Whitelist ile injection yüzeyi
        // kapatılır (security-principles #8 FilterKey deseni). ModelState kontrol edilmediği için
        // otoriter kapı BURADA (annotation defense-in-depth).
        private static readonly Regex FieldKeyPattern =
            new("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        private DbSet<FormField> Fields => db.Set<FormField>();
        private DbSet<FormFieldDataElementMap> Maps => db.Set<FormFieldDataElementMap>();

        public async Task<ServiceResult<int>> AddAsync(int formDefinitionId, int firmaId, FormFieldInput input, CancellationToken ct = default)
        {
            var owned = await db.Set<FormDefinition>().AsNoTracking()
                .AnyAsync(d => d.Id == formDefinitionId && d.FirmaId == firmaId, ct);
            if (!owned)
                return ServiceResult<int>.Failure("Form bulunamadı.");

            if (string.IsNullOrWhiteSpace(input.FieldKey) || string.IsNullOrWhiteSpace(input.Label))
                return ServiceResult<int>.Failure("Alan anahtarı ve etiket zorunlu.");

            if (!FieldKeyPattern.IsMatch(input.FieldKey.Trim()))
                return ServiceResult<int>.Failure("Alan anahtarı harf/alt çizgi ile başlamalı, yalnız harf-rakam-alt çizgi içerebilir.");

            var keyExists = await Fields.AsNoTracking()
                .AnyAsync(f => f.FormDefinitionId == formDefinitionId && f.FieldKey == input.FieldKey, ct);
            if (keyExists)
                return ServiceResult<int>.Failure("Bu alan anahtarı zaten kullanılıyor.");

            var condError = await ValidateConditionAsync(formDefinitionId, input.ConditionalLogic, input.FieldKey.Trim(), ct);
            if (condError != null)
                return ServiceResult<int>.Failure(condError);

            // MaxAsync(nullable) — boş sette null döner (DefaultIfEmpty EF SQL'e çevrilemez).
            var maxOrder = await Fields.AsNoTracking()
                .Where(f => f.FormDefinitionId == formDefinitionId)
                .MaxAsync(f => (int?)f.Order, ct) ?? 0;

            var field = new FormField
            {
                FormDefinitionId = formDefinitionId,
                Order = maxOrder + 1,
                FieldKey = input.FieldKey.Trim(),
                Label = input.Label.Trim(),
                HelpText = input.HelpText,
                FieldType = input.FieldType,
                IsRequired = input.IsRequired,
                Options = input.Options,
                ValidationRules = input.ValidationRules,
                ConditionalLogic = input.ConditionalLogic,
                DefaultValue = input.DefaultValue,
                Placeholder = input.Placeholder
            };
            Fields.Add(field);
            await db.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(field.Id);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(int fieldId, int formDefinitionId, int firmaId, FormFieldInput input, CancellationToken ct = default)
        {
            var field = await Fields
                .Include(f => f.FormDefinition)
                .FirstOrDefaultAsync(f => f.Id == fieldId && f.FormDefinitionId == formDefinitionId, ct);
            if (field?.FormDefinition == null || field.FormDefinition.FirmaId != firmaId)
                return ServiceResult<bool>.Failure("Alan bulunamadı.");

            if (string.IsNullOrWhiteSpace(input.FieldKey) || !FieldKeyPattern.IsMatch(input.FieldKey.Trim()))
                return ServiceResult<bool>.Failure("Alan anahtarı harf/alt çizgi ile başlamalı, yalnız harf-rakam-alt çizgi içerebilir.");

            if (field.FieldKey != input.FieldKey)
            {
                var keyExists = await Fields.AsNoTracking()
                    .AnyAsync(f => f.FormDefinitionId == formDefinitionId && f.FieldKey == input.FieldKey && f.Id != fieldId, ct);
                if (keyExists)
                    return ServiceResult<bool>.Failure("Bu alan anahtarı zaten kullanılıyor.");

                // G3 (silent-failure 2b): eski anahtara referans veren koşul varsa rename bloke — aksi
                // halde dependent alan sessizce kalıcı-gizli olur (koşul {eski_anahtar} çözülemez) → veri kaybı.
                if (await AnyDependentAsync(formDefinitionId, field.FieldKey, fieldId, ct))
                    return ServiceResult<bool>.Failure("Bu alanın anahtarı başka bir alanın görünürlük koşulunda kullanılıyor — önce o koşulu güncelleyin.");
            }

            var condError = await ValidateConditionAsync(formDefinitionId, input.ConditionalLogic, input.FieldKey.Trim(), ct);
            if (condError != null)
                return ServiceResult<bool>.Failure(condError);

            field.FieldKey = input.FieldKey.Trim();
            field.Label = input.Label.Trim();
            field.HelpText = input.HelpText;
            field.FieldType = input.FieldType;
            field.IsRequired = input.IsRequired;
            field.Options = input.Options;
            field.ValidationRules = input.ValidationRules;
            field.ConditionalLogic = input.ConditionalLogic;
            field.DefaultValue = input.DefaultValue;
            field.Placeholder = input.Placeholder;
            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        // Plan 56 M-A G3 danışman LOW: koşul referansı (cond.Field) dangling olmamalı — aynı formda
        // var olan BAŞKA bir FieldKey olmalı (kendine referans + silinmiş alana referans reddedilir).
        // Aksi halde görünürlük sessizce sabitlenir (silent logic hatası).
        private async Task<string?> ValidateConditionAsync(int formDefinitionId, string? conditionalLogic, string ownFieldKey, CancellationToken ct)
        {
            var cond = FormConditionEvaluator.TryParse(conditionalLogic);
            if (!string.IsNullOrWhiteSpace(conditionalLogic) && cond is null)
                return "Koşul tanımı geçersiz.";
            if (cond is null)
                return null;

            if (string.Equals(cond.Field, ownFieldKey, StringComparison.Ordinal))
                return "Bir alan kendi görünürlük koşuluna referans veremez.";

            var refExists = await Fields.AsNoTracking()
                .AnyAsync(f => f.FormDefinitionId == formDefinitionId && f.FieldKey == cond.Field, ct);
            if (!refExists)
                return $"Koşul referansı '{cond.Field}' formda bulunamadı.";

            return null;
        }

        public async Task<ServiceResult<bool>> RemoveAsync(int fieldId, int formDefinitionId, int firmaId, CancellationToken ct = default)
        {
            var field = await Fields
                .Include(f => f.FormDefinition)
                .FirstOrDefaultAsync(f => f.Id == fieldId && f.FormDefinitionId == formDefinitionId, ct);
            if (field?.FormDefinition == null || field.FormDefinition.FirmaId != firmaId)
                return ServiceResult<bool>.Failure("Alan bulunamadı.");

            // G3 (silent-failure 2a): bu alana görünürlük koşulunda referans veren alan varsa silme bloke —
            // aksi halde dependent alan sessizce kalıcı-gizli olur (referans çözülemez) → veri kaybı.
            if (await AnyDependentAsync(formDefinitionId, field.FieldKey, fieldId, ct))
                return ServiceResult<bool>.Failure("Bu alan başka bir alanın görünürlük koşulunda kullanılıyor — önce o koşulu kaldırın.");

            Fields.Remove(field);
            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        // G3 — verilen anahtara görünürlük koşulunda referans veren (kendisi hariç) alan var mı?
        // ConditionalLogic JSON kolonu SQL'de parse edilemez → aday satırları çekip bellekte değerlendir.
        private async Task<bool> AnyDependentAsync(int formDefinitionId, string referencedKey, int excludeFieldId, CancellationToken ct)
        {
            var candidates = await Fields.AsNoTracking()
                .Where(f => f.FormDefinitionId == formDefinitionId && f.ConditionalLogic != null && f.Id != excludeFieldId)
                .Select(f => f.ConditionalLogic)
                .ToListAsync(ct);
            return candidates.Any(c => FormConditionEvaluator.TryParse(c)?.Field == referencedKey);
        }

        public async Task<ServiceResult<bool>> MoveAsync(int fieldId, int formDefinitionId, int firmaId, FieldMoveDirection direction, CancellationToken ct = default)
        {
            var owned = await db.Set<FormDefinition>().AsNoTracking()
                .AnyAsync(d => d.Id == formDefinitionId && d.FirmaId == firmaId, ct);
            if (!owned)
                return ServiceResult<bool>.Failure("Form bulunamadı.");

            var fields = await Fields
                .Where(f => f.FormDefinitionId == formDefinitionId)
                .ToListAsync(ct);

            var swaps = FormFieldOrderer.Move(fields.Select(f => (f.Id, f.Order)).ToList(), fieldId, direction);
            if (swaps.Count == 0)
                return ServiceResult<bool>.Ok(true); // sınırda — no-op, hata değil

            var byId = fields.ToDictionary(f => f.Id);
            foreach (var (id, newOrder) in swaps)
                byId[id].Order = newOrder;

            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        // Faz 4 §4.3 — field → KVKK DataElement eşle (OPTIONAL, Yayında bloklamaz). DataElementId
        // cross-modül lookup ile doğrulanır (Kvkk modülü yoksa Exists=false → hata, sessiz kabul yok).
        public async Task<ServiceResult<int>> MapDataElementAsync(
            int fieldId, int formDefinitionId, int firmaId, int dataElementId, byte usageType, string? notes, CancellationToken ct = default)
        {
            var field = await Fields
                .Include(f => f.FormDefinition)
                .FirstOrDefaultAsync(f => f.Id == fieldId && f.FormDefinitionId == formDefinitionId, ct);
            if (field?.FormDefinition == null || field.FormDefinition.FirmaId != firmaId)
                return ServiceResult<int>.Failure("Alan bulunamadı.");

            if (!await dataElements.ExistsAsync(dataElementId, ct))
                return ServiceResult<int>.Failure("Seçilen KVKK veri öğesi bulunamadı (KVKK modülü aktif mi?).");

            var exists = await Maps.AsNoTracking()
                .AnyAsync(m => m.FormFieldId == fieldId && m.DataElementId == dataElementId, ct);
            if (exists)
                return ServiceResult<int>.Failure("Bu veri öğesi zaten bu alana bağlı.");

            var map = new FormFieldDataElementMap
            {
                FormFieldId = fieldId,
                DataElementId = dataElementId,
                UsageType = usageType,
                Notes = notes
            };
            Maps.Add(map);
            await db.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(map.Id);
        }

        public async Task<ServiceResult<bool>> UnmapDataElementAsync(
            int mapId, int formDefinitionId, int firmaId, CancellationToken ct = default)
        {
            var map = await Maps
                .Include(m => m.FormField!).ThenInclude(f => f.FormDefinition)
                .FirstOrDefaultAsync(m => m.Id == mapId, ct);
            if (map?.FormField?.FormDefinition == null
                || map.FormField.FormDefinitionId != formDefinitionId
                || map.FormField.FormDefinition.FirmaId != firmaId)
                return ServiceResult<bool>.Failure("Eşleme bulunamadı.");

            Maps.Remove(map);
            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }
    }
}
