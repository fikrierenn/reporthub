using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    /// <summary>
    /// M-01: ReportCategory CRUD (create/update/delete). G-04 audit log calls included.
    /// AdminController.HandlePostAction'dan ekstrakte edildi.
    /// </summary>
    public class CategoryManagementService
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        public CategoryManagementService(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        public async Task<AdminOperationResult> CreateAsync(string? name, string? description, bool isActive)
        {
            var trimmedName = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
                return AdminOperationResult.Fail("Kategori adi zorunludur.");

            var exists = await _context.ReportCategories
                .AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower());
            if (exists)
                return AdminOperationResult.Fail("Ayni isimde kategori zaten var.");

            var entity = new ReportCategory
            {
                Name = trimmedName,
                Description = description ?? "",
                IsActive = isActive
            };
            _context.ReportCategories.Add(entity);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "category_create",
                TargetType = "category",
                TargetKey = entity.CategoryId.ToString(),
                Description = "Category created",
                NewValuesJson = AuditLogService.ToJson(new { entity.CategoryId, entity.Name, entity.Description, entity.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Kategori eklendi.");
        }

        public async Task<AdminOperationResult> UpdateAsync(int categoryId, string? name, string? description, bool isActive)
        {
            var category = await _context.ReportCategories.FindAsync(categoryId);
            if (category == null) return AdminOperationResult.Fail("Kategori bulunamadi.");

            var trimmedName = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
                return AdminOperationResult.Fail("Kategori adi zorunludur.");

            var duplicate = await _context.ReportCategories
                .AnyAsync(c => c.CategoryId != category.CategoryId && c.Name.ToLower() == trimmedName.ToLower());
            if (duplicate)
                return AdminOperationResult.Fail("Ayni isimde kategori zaten var.");

            var oldSnap = new { category.CategoryId, category.Name, category.Description, category.IsActive };

            category.Name = trimmedName;
            category.Description = description ?? "";
            category.IsActive = isActive;
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "category_update",
                TargetType = "category",
                TargetKey = category.CategoryId.ToString(),
                Description = "Category updated",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                NewValuesJson = AuditLogService.ToJson(new { category.CategoryId, category.Name, category.Description, category.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Kategori guncellendi.");
        }

        public async Task<AdminOperationResult> DeleteAsync(int categoryId)
        {
            var category = await _context.ReportCategories.FindAsync(categoryId);
            if (category == null) return AdminOperationResult.Fail("Kategori bulunamadi.");

            var oldSnap = new { category.CategoryId, category.Name, category.Description, category.IsActive };

            _context.ReportCategories.Remove(category);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "category_delete",
                TargetType = "category",
                TargetKey = category.CategoryId.ToString(),
                Description = "Category deleted",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Kategori silindi.");
        }
    }
}
