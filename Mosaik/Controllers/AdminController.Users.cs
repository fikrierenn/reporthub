using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.Services;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
{
    // Partial split (csharp-conventions hard-limit). Kullanıcı CRUD action'lari +
    // BuildUserFormInput + BuildCreateUserFormAsync.
    public partial class AdminController
    {
        [Route("Admin/CreateUser")]
        public async Task<IActionResult> CreateUser()
        {
            return View(await BuildAdminUserFormAsync(
                user: new User { IsActive = true },
                selectedRoleIds: new HashSet<int>(),
                postedFilters: new List<UserDataFilter>(),
                message: null,
                messageType: null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CreateUser")]
        public async Task<IActionResult> CreateUser(User user)
        {
            var input = BuildUserFormInput(user);
            if (!ModelState.IsValid)
                return View(await BuildAdminUserFormAsync(user, input.SelectedRoleIds, postedFilters: null,
                    message: "Form gecersiz, hatalari duzeltin.", messageType: "error"));

            var result = await _userService.CreateAsync(input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "users" });
            }
            return View(await BuildAdminUserFormAsync(user, input.SelectedRoleIds, postedFilters: null,
                message: result.Message, messageType: "error"));
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

            var selectedRoleIds = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.RoleId)
                .ToListAsync();
            var dataFilters = await _context.UserDataFilters
                .Where(f => f.UserId == user.UserId)
                .OrderBy(f => f.FilterKey)
                .ThenBy(f => f.FilterValue)
                .ToListAsync();

            return View(await BuildAdminUserFormAsync(user, selectedRoleIds.ToHashSet(),
                postedFilters: dataFilters, message: null, messageType: null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditUser/{id}")]
        public async Task<IActionResult> EditUser(int id, User user)
        {
            // User.UserId'de [BindNever] (M-07 mass assignment koruması) — form'dan
            // UserId hidden input gelse bile bind edilmez. Route parametresi id'yi kullan.
            user.UserId = id; // view'de "Pasif" gibi yaniltici state olusmasin
            var input = BuildUserFormInput(user);

            if (!ModelState.IsValid)
                return View(await BuildAdminUserFormAsync(user, input.SelectedRoleIds, postedFilters: null,
                    message: "Form gecersiz, hatalari duzeltin.", messageType: "error"));

            var result = await _userService.UpdateAsync(id, input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "users" });
            }
            return View(await BuildAdminUserFormAsync(user, input.SelectedRoleIds, postedFilters: null,
                message: result.Message, messageType: "error"));
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
            // SelectedFirmaIds checkbox grubu — seçili olanları CSV'ye birleştir ("1,2,3").
            // Hiç seçilmediyse NULL (modül kapalı).
            // Not: form field adı User.FirmaIds (string) ile çakışmasın diye SelectedFirmaIds.
            var firmaIds = ParseIds(Request.Form["SelectedFirmaIds"]);
            string? firmaIdsCsv = firmaIds.Count > 0
                ? string.Join(",", firmaIds.OrderBy(x => x))
                : null;

            return new UserFormInput(
                Username: user.Username,
                FullName: user.FullName,
                Email: user.Email,
                IsAdUser: ReadFormBool("IsAdUser"),
                IsActive: ReadFormBool("IsActive"),
                Password: Request.Form["Password"],
                FirmaIds: firmaIdsCsv,
                SelectedRoleIds: ParseIds(Request.Form["SelectedRoles"]),
                DataFilters: filters);
        }

        // 3 cagri noktasi: CreateUser GET (postedFilters=empty), CreateUser POST error
        // (postedFilters=null -> Request.Form'dan oku), EditUser GET (postedFilters=DB'den
        // gelen kayit), EditUser POST error (postedFilters=null -> Request.Form'dan oku).
        // postedFilters null oldugunda Request.Form parsing yapilir (POST hata durumu).
        private async Task<AdminUserFormViewModel> BuildAdminUserFormAsync(
            User user,
            HashSet<int> selectedRoleIds,
            List<UserDataFilter>? postedFilters,
            string? message,
            string? messageType)
        {
            var roles = await _context.Roles.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
            var dataSources = await _context.DataSources.AsNoTracking().Where(ds => ds.IsActive).OrderBy(ds => ds.Title).ToListAsync();
            var filterDefs = await _context.FilterDefinitions
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder).ThenBy(f => f.Label)
                .ToListAsync();

            var filters = postedFilters ?? ReadPostedFilters();

            ViewBag.Firmas = await _context.Firmas
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.Ad)
                .ToListAsync();

            return new AdminUserFormViewModel
            {
                User = user,
                AvailableRoles = roles,
                SelectedRoleIds = selectedRoleIds,
                DataFilters = filters,
                DataSources = dataSources,
                FilterDefinitions = filterDefs,
                Message = message ?? "",
                MessageType = messageType ?? ""
            };
        }

        private List<UserDataFilter> ReadPostedFilters()
        {
            var filterKeys = Request.Form["FilterKeys"].ToArray();
            var filterValues = Request.Form["FilterValues"].ToArray();
            var filterDataSources = Request.Form["FilterDataSources"].ToArray();
            var posted = new List<UserDataFilter>();
            for (var i = 0; i < filterKeys.Length; i++)
            {
                var key = filterKeys[i]?.Trim();
                var value = i < filterValues.Length ? filterValues[i]?.Trim() : null;
                var ds = i < filterDataSources.Length ? filterDataSources[i]?.Trim() : null;
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
                posted.Add(new UserDataFilter
                {
                    FilterKey = key!,
                    FilterValue = value!,
                    DataSourceKey = string.IsNullOrWhiteSpace(ds) ? null : ds
                });
            }
            return posted;
        }
    }
}
