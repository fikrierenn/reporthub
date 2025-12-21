using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        public AuthController(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        [HttpGet("/Login")]
        public IActionResult Login(string? returnUrl = null, string? logout = null)
        {
            if (!string.IsNullOrEmpty(logout))
            {
                return RedirectToAction("Logout");
            }

            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            return View(new LoginViewModel());
        }

        [HttpPost("/Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

            if (user == null || !PasswordHasher.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Kullanici adi veya sifre hatali.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new("full_name", user.FullName)
            };

            foreach (var role in SplitRoles(user.Roles))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                var lowerRole = role.ToLowerInvariant();
                if (!string.Equals(role, lowerRole, StringComparison.Ordinal))
                {
                    claims.Add(new Claim(ClaimTypes.Role, lowerRole));
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            await UpdateLastLogin(user.UserId);
            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "login",
                TargetType = "user",
                TargetKey = user.UserId.ToString(),
                Description = "User login",
                IsSuccess = true
            });

            return RedirectToLocal(returnUrl);
        }

        [HttpGet("/Logout")]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "user";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "logout",
                TargetType = "user",
                TargetKey = username,
                Description = "User logout",
                IsSuccess = true,
                Username = username
            });
            return RedirectToAction("Login");
        }

        [HttpGet("/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task UpdateLastLogin(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                return;
            }

            user.LastLoginAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        private static IEnumerable<string> SplitRoles(string roles)
        {
            if (string.IsNullOrWhiteSpace(roles))
            {
                return Array.Empty<string>();
            }

            return roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(r => !string.IsNullOrWhiteSpace(r));
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
