namespace Mosaik.Core.AI.Rag
{
    // Plan 44 — Default izin uygulayıcısı.
    // 4 katman (sırayla): security level → role whitelist → dept whitelist → user whitelist.
    // NULL whitelist = tüm kullanıcılar erişebilir.
    // SQL-side kaba filter (SecurityLevel <= clearance) sonrası çağrılır — ikinci savunma hattı.
    public class DefaultRagAccessPolicy : IRagAccessPolicy
    {
        public bool IsAccessible(
            byte chunkSecurityLevel,
            string? allowedRoleIds,
            string? allowedDepartmentIds,
            string? allowedUserIds,
            RagUserContext user)
        {
            // 1. Security level check
            if (chunkSecurityLevel > user.SecurityClearance) return false;

            // 2. Role whitelist (null → herkes)
            if (allowedRoleIds is not null)
            {
                var allowed = Split(allowedRoleIds);
                if (!user.RoleNames.Any(r => allowed.Contains(r, StringComparer.OrdinalIgnoreCase)))
                    return false;
            }

            // 3. Department whitelist (null → herkes)
            if (allowedDepartmentIds is not null)
            {
                var allowed = Split(allowedDepartmentIds)
                    .Select(s => int.TryParse(s, out var id) ? id : -1)
                    .ToHashSet();
                if (!user.DepartmentIds.Any(d => allowed.Contains(d)))
                    return false;
            }

            // 4. User explicit whitelist (null → herkes)
            if (allowedUserIds is not null)
            {
                var allowed = Split(allowedUserIds)
                    .Select(s => int.TryParse(s, out var id) ? id : -1)
                    .ToHashSet();
                if (!allowed.Contains(user.UserId))
                    return false;
            }

            return true;
        }

        private static string[] Split(string csv) =>
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
