using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        public ProfileController(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return NotFound();
            }

            // M-03: Rolleri UserRole junction'dan al (CSV deprecate).
            var roleNames = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.Role!.Name)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Roles = string.Join(", ", roleNames),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                Message = TempData["Message"]?.ToString() ?? "",
                MessageType = TempData["MessageType"]?.ToString() ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return NotFound();
            }

            // M-03: Rol listesi — re-render sırasında view'e geri koymak için.
            var roleNamesForRender = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.Role!.Name)
                .ToListAsync();
            var rolesJoined = string.Join(", ", roleNamesForRender);

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                model.Message = "Ad soyad bos birakilamaz.";
                model.MessageType = "error";
                model.Username = user.Username;
                model.Roles = rolesJoined;
                model.IsActive = user.IsActive;
                model.LastLoginAt = user.LastLoginAt;
                return View(model);
            }

            var passwordChanged = false;
            if (!string.IsNullOrWhiteSpace(model.NewPassword) || !string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword != model.ConfirmPassword)
                {
                    model.Message = "Yeni sifreler eslesmiyor.";
                    model.MessageType = "error";
                    model.Username = user.Username;
                    model.Roles = rolesJoined;
                    model.IsActive = user.IsActive;
                    model.LastLoginAt = user.LastLoginAt;
                    return View(model);
                }

                user.PasswordHash = PasswordHasher.CreateHash(model.NewPassword);
                passwordChanged = true;
            }

            var oldValues = new
            {
                user.FullName,
                user.Email
            };

            user.FullName = model.FullName.Trim();
            user.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var newValues = new
            {
                user.FullName,
                user.Email
            };

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "profile_update",
                TargetType = "user",
                TargetKey = user.UserId.ToString(),
                Description = "Profile updated",
                OldValuesJson = AuditLogService.ToJson(oldValues),
                NewValuesJson = AuditLogService.ToJson(newValues),
                IsSuccess = true
            });

            if (passwordChanged)
            {
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "password_change",
                    TargetType = "user",
                    TargetKey = user.UserId.ToString(),
                    Description = "Password changed",
                    IsSuccess = true
                });
            }

            TempData["Message"] = "Profil guncellendi";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index");
        }
    }
}
