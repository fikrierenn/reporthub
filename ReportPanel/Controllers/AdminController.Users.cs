using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    // Partial split (csharp-conventions hard-limit). Kullanıcı CRUD action'lari +
    // BuildUserFormInput + BuildCreateUserFormAsync.
    public partial class AdminController
    {
        [Route("Admin/CreateUser")]
        public async Task<IActionResult> CreateUser()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();
            var dataSources = await _context.DataSources
                .AsNoTracking()
                .Where(ds => ds.IsActive)
                .OrderBy(ds => ds.Title)
                .ToListAsync();
            var filterDefs = await _context.FilterDefinitions
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder).ThenBy(f => f.Label)
                .ToListAsync();
            return View(new AdminUserFormViewModel
            {
                User = new User { IsActive = true },
                AvailableRoles = roles,
                SelectedRoleIds = new HashSet<int>(),
                DataFilters = new List<UserDataFilter>(),
                DataSources = dataSources,
                FilterDefinitions = filterDefs
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CreateUser")]
        public async Task<IActionResult> CreateUser(User user)
        {
            var input = BuildUserFormInput(user);
            var result = await _userService.CreateAsync(input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "users" });
            }
            return View(await BuildCreateUserFormAsync(user, input.SelectedRoleIds, result.Message, "error"));
        }

        [Route("Admin/EditUser/{id}")]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Message"] = "Kullanici bulunamadi";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "users" });
            }

            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();
            var selectedRoleIds = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.RoleId)
                .ToListAsync();
            var dataFilters = await _context.UserDataFilters
                .Where(f => f.UserId == user.UserId)
                .OrderBy(f => f.FilterKey)
                .ThenBy(f => f.FilterValue)
                .ToListAsync();
            var dataSources = await _context.DataSources
                .Where(ds => ds.IsActive)
                .OrderBy(ds => ds.Title)
                .ToListAsync();
            var filterDefs = await _context.FilterDefinitions
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder).ThenBy(f => f.Label)
                .ToListAsync();
            return View(new AdminUserFormViewModel
            {
                User = user,
                AvailableRoles = roles,
                SelectedRoleIds = selectedRoleIds.ToHashSet(),
                DataFilters = dataFilters,
                DataSources = dataSources,
                FilterDefinitions = filterDefs
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditUser/{id}")]
        public async Task<IActionResult> EditUser(User user)
        {
            var input = BuildUserFormInput(user);
            var result = await _userService.UpdateAsync(user.UserId, input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "users" });
            }
            var allRoles = await _context.Roles.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
            return View(new AdminUserFormViewModel
            {
                User = user,
                AvailableRoles = allRoles,
                SelectedRoleIds = input.SelectedRoleIds,
                Message = result.Message,
                MessageType = "error"
            });
        }

        // M-01: Form -> UserFormInput. UserManagementService.NormalizeUsername static.
        private UserFormInput BuildUserFormInput(User user)
        {
            var filterKeys = Request.Form["FilterKeys"].ToArray();
            var filterValues = Request.Form["FilterValues"].ToArray();
            var filterDataSources = Request.Form["FilterDataSources"].ToArray();
            var filters = new List<UserFilterInput>();
            for (var i = 0; i < filterKeys.Length; i++)
            {
                var k = filterKeys[i]?.Trim() ?? "";
                var v = i < filterValues.Length ? (filterValues[i]?.Trim() ?? "") : "";
                var ds = i < filterDataSources.Length ? filterDataSources[i]?.Trim() : null;
                if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v)) continue;
                filters.Add(new UserFilterInput(k, v, ds));
            }
            return new UserFormInput(
                Username: user.Username,
                FullName: user.FullName,
                Email: user.Email,
                IsAdUser: ReadFormBool("IsAdUser"),
                IsActive: ReadFormBool("IsActive"),
                Password: Request.Form["Password"],
                SelectedRoleIds: ParseIds(Request.Form["SelectedRoles"]),
                DataFilters: filters);
        }

        private async Task<AdminUserFormViewModel> BuildCreateUserFormAsync(User user, HashSet<int> selectedRoleIds, string message, string messageType)
        {
            var roles = await _context.Roles.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
            var dataSources = await _context.DataSources.AsNoTracking().Where(ds => ds.IsActive).OrderBy(ds => ds.Title).ToListAsync();
            // Form tarafindan gonderilen filtreleri geri yukle ki kullanici kayip hissetmesin
            var filterKeys = Request.Form["FilterKeys"].ToArray();
            var filterValues = Request.Form["FilterValues"].ToArray();
            var filterDataSources = Request.Form["FilterDataSources"].ToArray();
            var postedFilters = new List<UserDataFilter>();
            for (var i = 0; i < filterKeys.Length; i++)
            {
                var key = filterKeys[i]?.Trim();
                var value = i < filterValues.Length ? filterValues[i]?.Trim() : null;
                var ds = i < filterDataSources.Length ? filterDataSources[i]?.Trim() : null;
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
                postedFilters.Add(new UserDataFilter
                {
                    FilterKey = key!,
                    FilterValue = value!,
                    DataSourceKey = string.IsNullOrWhiteSpace(ds) ? null : ds
                });
            }
            var filterDefs = await _context.FilterDefinitions
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder).ThenBy(f => f.Label)
                .ToListAsync();
            return new AdminUserFormViewModel
            {
                User = user,
                AvailableRoles = roles,
                SelectedRoleIds = selectedRoleIds,
                DataFilters = postedFilters,
                DataSources = dataSources,
                FilterDefinitions = filterDefs,
                Message = message,
                MessageType = messageType
            };
        }
    }
}
