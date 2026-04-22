using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    /// <summary>
    /// M-03 Faz A kapsaminda AdminController.SyncUserRoles private method'undan ekstrakte edildi.
    /// UserRole junction senkronu bir kullanici icin istenen rol setini yansitir — mevcutlari siler,
    /// yenileri ekler. Idempotent: ayni roleIds ile tekrar cagrilmasi no-op sonuclanir (sirali
    /// insert/delete olsa bile son state ayni).
    /// </summary>
    public class UserRoleSyncService
    {
        private readonly ReportPanelContext _context;

        public UserRoleSyncService(ReportPanelContext context)
        {
            _context = context;
        }

        public async Task SyncAsync(int userId, HashSet<int> roleIds)
        {
            var existing = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            _context.UserRoles.RemoveRange(existing);

            foreach (var roleId in roleIds)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
