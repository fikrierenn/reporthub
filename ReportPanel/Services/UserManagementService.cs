using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    public record UserFilterInput(string Key, string Value, string? DataSourceKey);

    public record UserFormInput(
        string? Username,
        string? FullName,
        string? Email,
        bool IsAdUser,
        bool IsActive,
        string? Password,
        HashSet<int> SelectedRoleIds,
        List<UserFilterInput> DataFilters);

    /// <summary>
    /// M-01: User CRUD. CreateUser + EditUser form action'lari ve delete_user
    /// HandlePostAction case'i bu servise gelir. Audit (user_create/update/delete),
    /// UserRole senkronu (UserRoleSyncService), UserDataFilter senkronu hepsi
    /// dahil.
    /// </summary>
    public class UserManagementService
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;
        private readonly UserRoleSyncService _userRoleSync;

        public UserManagementService(
            ReportPanelContext context,
            AuditLogService auditLog,
            UserRoleSyncService userRoleSync)
        {
            _context = context;
            _auditLog = auditLog;
            _userRoleSync = userRoleSync;
        }

        public async Task<AdminOperationResult> CreateAsync(UserFormInput input)
        {
            var username = NormalizeUsername(input.Username);
            var fullName = (input.FullName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName) || input.SelectedRoleIds.Count == 0)
                return AdminOperationResult.Fail("Zorunlu alanlar bos birakilamaz.");
            if (!input.IsAdUser && string.IsNullOrWhiteSpace(input.Password))
                return AdminOperationResult.Fail("Sifre alani zorunludur.");

            if (await _context.Users.AnyAsync(u => u.Username == username))
                return AdminOperationResult.Fail("Bu kullanici adi zaten mevcut.");

            var now = DateTime.UtcNow;
            var entity = new User
            {
                Username = username,
                FullName = fullName,
                Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim(),
                IsAdUser = input.IsAdUser,
                IsActive = input.IsActive,
                PasswordHash = input.IsAdUser
                    ? PasswordHasher.CreateHash(Guid.NewGuid().ToString("N"))
                    : PasswordHasher.CreateHash(input.Password!),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Users.Add(entity);
            await _context.SaveChangesAsync();
            await _userRoleSync.SyncAsync(entity.UserId, input.SelectedRoleIds);
            await SyncDataFiltersAsync(entity.UserId, input.DataFilters);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "user_create",
                TargetType = "user",
                TargetKey = entity.UserId.ToString(),
                Description = "User created",
                NewValuesJson = AuditLogService.ToJson(new
                {
                    entity.UserId,
                    entity.Username,
                    entity.FullName,
                    entity.Email,
                    Roles = await GetRoleNamesAsync(input.SelectedRoleIds),
                    entity.IsAdUser,
                    entity.IsActive
                }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Kullanici eklendi");
        }

        public async Task<AdminOperationResult> UpdateAsync(int userId, UserFormInput input)
        {
            var existing = await _context.Users.FindAsync(userId);
            if (existing == null) return AdminOperationResult.Fail("Kullanici bulunamadi");

            var username = NormalizeUsername(input.Username);
            var fullName = (input.FullName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName) || input.SelectedRoleIds.Count == 0)
                return AdminOperationResult.Fail("Zorunlu alanlar bos birakilamaz.");

            if (await _context.Users.AnyAsync(u => u.Username == username && u.UserId != existing.UserId))
                return AdminOperationResult.Fail("Bu kullanici adi zaten mevcut.");

            var wasAdUser = existing.IsAdUser;

            // AD user -> local user geçişi: şifre zorunlu.
            if (!input.IsAdUser && wasAdUser && string.IsNullOrWhiteSpace(input.Password))
                return AdminOperationResult.Fail("Sifre alani zorunludur.");

            existing.Username = username;
            existing.FullName = fullName;
            existing.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
            existing.IsAdUser = input.IsAdUser;
            existing.IsActive = input.IsActive;

            if (existing.IsAdUser)
            {
                if (!string.IsNullOrWhiteSpace(input.Password))
                    existing.PasswordHash = PasswordHasher.CreateHash(Guid.NewGuid().ToString("N"));
            }
            else if (!string.IsNullOrWhiteSpace(input.Password))
            {
                existing.PasswordHash = PasswordHasher.CreateHash(input.Password);
            }

            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _userRoleSync.SyncAsync(existing.UserId, input.SelectedRoleIds);
            await SyncDataFiltersAsync(existing.UserId, input.DataFilters);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "user_update",
                TargetType = "user",
                TargetKey = existing.UserId.ToString(),
                Description = "User updated",
                NewValuesJson = AuditLogService.ToJson(new
                {
                    existing.UserId,
                    existing.Username,
                    existing.FullName,
                    existing.Email,
                    Roles = await GetRoleNamesAsync(input.SelectedRoleIds),
                    existing.IsAdUser,
                    existing.IsActive
                }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Kullanici guncellendi");
        }

        public async Task<AdminOperationResult> DeleteAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return AdminOperationResult.Fail("Kullanici bulunamadi");

            var roleNames = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.Role!.Name)
                .ToListAsync();
            var oldSnap = new
            {
                user.UserId,
                user.Username,
                user.FullName,
                user.Email,
                Roles = roleNames,
                user.IsActive
            };

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "user_delete",
                TargetType = "user",
                TargetKey = user.UserId.ToString(),
                Description = "User deleted",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Kullanici silindi");
        }

        // ---- internals ----

        private async Task<List<string>> GetRoleNamesAsync(HashSet<int> roleIds)
        {
            if (roleIds.Count == 0) return new List<string>();
            return await _context.Roles
                .Where(r => roleIds.Contains(r.RoleId))
                .Select(r => r.Name)
                .ToListAsync();
        }

        private async Task SyncDataFiltersAsync(int userId, List<UserFilterInput> filters)
        {
            var existing = await _context.UserDataFilters.Where(f => f.UserId == userId).ToListAsync();
            _context.UserDataFilters.RemoveRange(existing);

            foreach (var f in filters)
            {
                if (string.IsNullOrWhiteSpace(f.Key) || string.IsNullOrWhiteSpace(f.Value)) continue;
                _context.UserDataFilters.Add(new UserDataFilter
                {
                    UserId = userId,
                    FilterKey = f.Key.Trim(),
                    FilterValue = f.Value.Trim(),
                    DataSourceKey = string.IsNullOrWhiteSpace(f.DataSourceKey) ? null : f.DataSourceKey.Trim(),
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        public static string NormalizeUsername(string? raw)
        {
            var value = raw?.Trim() ?? "";
            var slashIndex = value.IndexOf('\\');
            if (slashIndex > 0 && slashIndex < value.Length - 1)
                return value.Substring(slashIndex + 1);
            return value;
        }
    }
}
