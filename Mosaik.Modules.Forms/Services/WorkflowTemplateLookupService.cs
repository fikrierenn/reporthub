using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 57 B3 — form→workflow şablon bağlama picker'ı. Cross-modül: WorkflowTemplate CLR tipi
    // ana projede, Forms compile bağlanamaz (ADR-002) → raw SQL (DataElementLookupService deseni).
    // Sadece EntityType='FormSubmission' + aktif + aynı-firma şablonlar listelenir
    // (WorkflowEngine.StartAsync da aynı üçlüyü doğrular — çift taraflı tutarlı).
    public class WorkflowTemplateLookupService(DbContext db, ILogger<WorkflowTemplateLookupService> logger)
    {
        public sealed record TemplateLookup(int Id, string Name);

        public async Task<IReadOnlyList<TemplateLookup>> ListForFormsAsync(int firmaId, CancellationToken ct = default)
        {
            try
            {
                return await db.Database.SqlQueryRaw<TemplateLookup>(
                        "SELECT Id, Name FROM dbo.WorkflowTemplates WHERE EntityType = 'FormSubmission' AND IsActive = 1 AND FirmaId = {0} ORDER BY Name",
                        firmaId)
                    .ToListAsync(ct);
            }
            catch (DbException ex)
            {
                logger.LogWarning(ex, "WorkflowTemplates sorgulanamadı — boş liste (picker opsiyonel, form kaydı etkilenmez).");
                return [];
            }
        }

        // Fail-closed: doğrulanamadı → false (geçersiz TriggersWorkflowId persist edilmez).
        public async Task<bool> ExistsAsync(int templateId, int firmaId, CancellationToken ct = default)
        {
            try
            {
                var rows = await db.Database.SqlQueryRaw<int>(
                        "SELECT Id AS Value FROM dbo.WorkflowTemplates WHERE Id = {0} AND FirmaId = {1} AND EntityType = 'FormSubmission' AND IsActive = 1",
                        templateId, firmaId)
                    .ToListAsync(ct);
                return rows.Count > 0;
            }
            catch (DbException ex)
            {
                logger.LogWarning(ex, "WorkflowTemplate doğrulanamadı (Id={Id}) — fail-closed, bağlama reddedildi.", templateId);
                return false;
            }
        }
    }
}
