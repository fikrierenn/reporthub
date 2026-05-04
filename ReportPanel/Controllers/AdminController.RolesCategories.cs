using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    // Partial split (csharp-conventions hard-limit). Rol + Kategori CRUD action'lari
    // (HandlePostAction üzerinden Index POST'ta CRUD; bu dosya sadece detay sayfa
    // edit form'ları — basit, paralel yapılı).
    public partial class AdminController
    {
        [Route("Admin/EditRole/{id}")]
        public async Task<IActionResult> EditRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                TempData["Message"] = "Rol bulunamadi.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "roles" });
            }

            return View(new AdminRoleFormViewModel
            {
                Role = role
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditRole/{id}")]
        public async Task<IActionResult> EditRole(Role role)
        {
            var existing = await _context.Roles.FindAsync(role.RoleId);
            if (existing == null)
            {
                TempData["Message"] = "Rol bulunamadi.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "roles" });
            }

            var name = role.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                return View(new AdminRoleFormViewModel
                {
                    Role = role,
                    Message = "Rol adi zorunludur.",
                    MessageType = "error"
                });
            }

            var duplicate = await _context.Roles
                .AnyAsync(r => r.RoleId != existing.RoleId && r.Name.ToLower() == name.ToLower());
            if (duplicate)
            {
                return View(new AdminRoleFormViewModel
                {
                    Role = role,
                    Message = "Ayni isimde rol zaten var.",
                    MessageType = "error"
                });
            }

            var oldName = existing.Name;
            existing.Name = name;
            existing.Description = role.Description?.Trim();
            existing.IsActive = ReadFormBool("IsActive");
            await _context.SaveChangesAsync();
            if (!string.Equals(oldName, name, StringComparison.OrdinalIgnoreCase))
            {
                await _roleService.PropagateRenameToReportAllowedRolesAsync(oldName, name);
            }

            TempData["Message"] = "Rol guncellendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "roles" });
        }

        [Route("Admin/EditCategory/{id}")]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.ReportCategories.FindAsync(id);
            if (category == null)
            {
                TempData["Message"] = "Kategori bulunamadi.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "categories" });
            }

            return View(new AdminCategoryFormViewModel
            {
                Category = category
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditCategory/{id}")]
        public async Task<IActionResult> EditCategory(ReportCategory category)
        {
            var existing = await _context.ReportCategories.FindAsync(category.CategoryId);
            if (existing == null)
            {
                TempData["Message"] = "Kategori bulunamadi.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "categories" });
            }

            var name = category.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                return View(new AdminCategoryFormViewModel
                {
                    Category = category,
                    Message = "Kategori adi zorunludur.",
                    MessageType = "error"
                });
            }

            var duplicate = await _context.ReportCategories
                .AnyAsync(c => c.CategoryId != existing.CategoryId && c.Name.ToLower() == name.ToLower());
            if (duplicate)
            {
                return View(new AdminCategoryFormViewModel
                {
                    Category = category,
                    Message = "Ayni isimde kategori zaten var.",
                    MessageType = "error"
                });
            }

            existing.Name = name;
            existing.Description = category.Description?.Trim();
            existing.IsActive = ReadFormBool("IsActive");
            await _context.SaveChangesAsync();

            TempData["Message"] = "Kategori guncellendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "categories" });
        }
    }
}
