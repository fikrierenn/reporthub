using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    public sealed record FormFieldInput(
        string FieldKey, string Label, string? HelpText, byte FieldType, bool IsRequired,
        string? Options, string? ValidationRules, string? DefaultValue, string? Placeholder);

    // Plan 41 Faz 2 — liste-tabanlı builder (drag-drop yok). Alan ekle/güncelle/sil/sırala.
    public class FormFieldService(DbContext db)
    {
        private DbSet<FormField> Fields => db.Set<FormField>();

        public async Task<ServiceResult<int>> AddAsync(int formDefinitionId, int firmaId, FormFieldInput input, CancellationToken ct = default)
        {
            var owned = await db.Set<FormDefinition>().AsNoTracking()
                .AnyAsync(d => d.Id == formDefinitionId && d.FirmaId == firmaId, ct);
            if (!owned)
                return ServiceResult<int>.Failure("Form bulunamadı.");

            if (string.IsNullOrWhiteSpace(input.FieldKey) || string.IsNullOrWhiteSpace(input.Label))
                return ServiceResult<int>.Failure("Alan anahtarı ve etiket zorunlu.");

            var keyExists = await Fields.AsNoTracking()
                .AnyAsync(f => f.FormDefinitionId == formDefinitionId && f.FieldKey == input.FieldKey, ct);
            if (keyExists)
                return ServiceResult<int>.Failure("Bu alan anahtarı zaten kullanılıyor.");

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

            if (field.FieldKey != input.FieldKey)
            {
                var keyExists = await Fields.AsNoTracking()
                    .AnyAsync(f => f.FormDefinitionId == formDefinitionId && f.FieldKey == input.FieldKey && f.Id != fieldId, ct);
                if (keyExists)
                    return ServiceResult<bool>.Failure("Bu alan anahtarı zaten kullanılıyor.");
            }

            field.FieldKey = input.FieldKey.Trim();
            field.Label = input.Label.Trim();
            field.HelpText = input.HelpText;
            field.FieldType = input.FieldType;
            field.IsRequired = input.IsRequired;
            field.Options = input.Options;
            field.ValidationRules = input.ValidationRules;
            field.DefaultValue = input.DefaultValue;
            field.Placeholder = input.Placeholder;
            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> RemoveAsync(int fieldId, int formDefinitionId, int firmaId, CancellationToken ct = default)
        {
            var field = await Fields
                .Include(f => f.FormDefinition)
                .FirstOrDefaultAsync(f => f.Id == fieldId && f.FormDefinitionId == formDefinitionId, ct);
            if (field?.FormDefinition == null || field.FormDefinition.FirmaId != firmaId)
                return ServiceResult<bool>.Failure("Alan bulunamadı.");

            Fields.Remove(field);
            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
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
    }
}
