namespace Mosaik.Core.Logging
{
    // Plan 17 v2 — Cross-modül audit log abstraction.
    // Host (Mosaik.Services.AuditLogService) implement eder, modüller IAuditLog
    // inject ederek kullanır. Mosaik.Core'da olduğu için cross-csproj erişim
    // dairesel referans riski olmadan sağlanır.
    public interface IAuditLog
    {
        Task LogAsync(
            string eventType,
            string? targetType = null,
            string? targetKey = null,
            string? description = null,
            string? oldValuesJson = null,
            string? newValuesJson = null,
            bool isSuccess = true);
    }
}
