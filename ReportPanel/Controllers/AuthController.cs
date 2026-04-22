using System.Security.Claims;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.DirectoryServices.AccountManagement;
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
    public class AuthController : Controller
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;
        private readonly IHostEnvironment _env;

        public AuthController(ReportPanelContext context, AuditLogService auditLog, IHostEnvironment env)
        {
            _context = context;
            _auditLog = auditLog;
            _env = env;
        }

        [HttpGet("/Login")]
        [AllowAnonymous]
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

            var windowsIdentity = User?.Identity?.Name ?? "";
            var model = new LoginViewModel();
            if (!string.IsNullOrWhiteSpace(windowsIdentity))
            {
                model.Username = windowsIdentity;
            }

            return View(model);
        }

        [HttpGet("/Login/WindowsIdentity")]
        [AllowAnonymous]
        public IActionResult WindowsIdentity()
        {
            var name = User?.Identity?.Name ?? "";
            return Json(new { username = name });
        }


        [HttpPost("/Login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var input = model.Username?.Trim() ?? "";
            var (domain, normalizedUsername) = SplitDomainUsername(input);

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == normalizedUsername);

            if (user == null)
            {
                if (!string.IsNullOrWhiteSpace(domain))
                {
                    if (!OperatingSystem.IsWindows())
                    {
                        ModelState.AddModelError(string.Empty, "Windows AD dogrulamasi sadece Windows ortaminda desteklenir.");
                        return View(model);
                    }

                    if (!ValidateAdCredentials(domain, normalizedUsername, model.Password))
                    {
                        ModelState.AddModelError(string.Empty, "Kullanici adi veya sifre hatali.");
                        return View(model);
                    }

                    var pendingUser = new User
                    {
                        Username = normalizedUsername,
                        FullName = normalizedUsername,
                        Email = null,
                        Roles = "",
                        IsAdUser = true,
                        IsActive = false,
                        PasswordHash = PasswordHasher.CreateHash(Guid.NewGuid().ToString("N")),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Users.Add(pendingUser);
                    await _context.SaveChangesAsync();
                    await _auditLog.LogAsync(new AuditLogEntry
                    {
                        EventType = "user_pending_create",
                        TargetType = "user",
                        TargetKey = pendingUser.UserId.ToString(),
                        Username = normalizedUsername,
                        Description = "User auto-created as inactive",
                        IsSuccess = true
                    });

                    ViewData["LoginWarning"] = "Hesabiniz olusturuldu ancak aktif degil. Yetkiniz yok, gerekli tanimlamalar icin bilgi islem ile iletisime geciniz.";
                    return View(model);
                }

                ModelState.AddModelError(string.Empty, "Kullanici adi veya sifre hatali.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ViewData["LoginWarning"] = "Hesabiniz pasif. Yetkiniz yok, gerekli tanimlamalar icin bilgi islem ile iletisime geciniz.";
                return View(model);
            }

            if (user.IsAdUser)
            {
                if (string.IsNullOrWhiteSpace(domain))
                {
                    ModelState.AddModelError(string.Empty, "AD kullanicilari icin DOMAIN\\kullanici formatini kullanin.");
                    return View(model);
                }

                if (!OperatingSystem.IsWindows())
                {
                    ModelState.AddModelError(string.Empty, "Windows AD dogrulamasi sadece Windows ortaminda desteklenir.");
                    return View(model);
                }

                if (!ValidateAdCredentials(domain, normalizedUsername, model.Password))
                {
                    ModelState.AddModelError(string.Empty, "Kullanici adi veya sifre hatali.");
                    return View(model);
                }
            }
            else if (!PasswordHasher.Verify(model.Password, user.PasswordHash))
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

            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.Role!.Name)
                .ToListAsync();

            if (roles.Count == 0 && !string.IsNullOrWhiteSpace(user.Roles))
            {
                roles = user.Roles
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .ToList();
            }

            foreach (var role in roles)
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
        [Authorize]
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
        [AllowAnonymous]
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

        private static (string? Domain, string Username) SplitDomainUsername(string input)
        {
            var value = input?.Trim() ?? "";
            var slashIndex = value.IndexOf('\\');
            if (slashIndex > 0 && slashIndex < value.Length - 1)
            {
                var domain = value.Substring(0, slashIndex);
                var username = value.Substring(slashIndex + 1);
                return (domain, username);
            }

            return (null, value);
        }

        [SupportedOSPlatform("windows")]
        private static bool ValidateAdCredentials(string domain, string username, string password)
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                using var context = new PrincipalContext(ContextType.Domain, domain);
                return context.ValidateCredentials(username, password, ContextOptions.Negotiate);
            }
            catch
            {
                return false;
            }
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
