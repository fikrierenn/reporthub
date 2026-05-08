using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mosaik.Models;
using Mosaik.ViewModels;

namespace Mosaik.Controllers
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
                TempData["Message"] = "Rol bulunamadı.";
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
        public async Task<IActionResult> EditRole(int id, Role role)
        {
            // IDOR guard: existing'i route id ile bul, role.RoleId form'dan gelmesin (Role.RoleId'de [BindNever] ileri taşınana kadar explicit guard).
            var existing = await _context.Roles.FindAsync(id);
            if (existing == null)
            {
                TempData["Message"] = "Rol bulunamadı.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "roles" });
            }

            var name = role.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                role.RoleId = id;
                return View(new AdminRoleFormViewModel
                {
                    Role = role,
                    Message = "Rol adı zorunludur.",
                    MessageType = "error"
                });
            }

            var duplicate = await _context.Roles
                .AnyAsync(r => r.RoleId != existing.RoleId && r.Name.ToLower() == name.ToLower());
            if (duplicate)
            {
                role.RoleId = id;
                return View(new AdminRoleFormViewModel
                {
                    Role = role,
                    Message = "Aynı isimde rol zaten var.",
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

            TempData["Message"] = "Rol güncellendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "roles" });
        }

        [Route("Admin/EditGroup/{id}")]
        public async Task<IActionResult> EditGroup(int id)
        {
            var group = await _context.ReportGroups.FindAsync(id);
            if (group == null)
            {
                TempData["Message"] = "Grup bulunamadı.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "groups" });
            }

            return View(new AdminGroupFormViewModel
            {
                Group = group
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditGroup/{id}")]
        public async Task<IActionResult> EditGroup(int id, ReportGroup group)
        {
            // ReportGroup.GroupId [BindNever] — route id explicit.
            var existing = await _context.ReportGroups.FindAsync(id);
            if (existing == null)
            {
                TempData["Message"] = "Grup bulunamadı.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "groups" });
            }

            var name = group.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                group.GroupId = id;
                return View(new AdminGroupFormViewModel
                {
                    Group = group,
                    Message = "Grup adı zorunludur.",
                    MessageType = "error"
                });
            }

            var duplicate = await _context.ReportGroups
                .AnyAsync(c => c.GroupId != existing.GroupId && c.Name.ToLower() == name.ToLower());
            if (duplicate)
            {
                group.GroupId = id;
                return View(new AdminGroupFormViewModel
                {
                    Group = group,
                    Message = "Aynı isimde grup zaten var.",
                    MessageType = "error"
                });
            }

            existing.Name = name;
            existing.Description = group.Description?.Trim();
            existing.IsActive = ReadFormBool("IsActive");
            await _context.SaveChangesAsync();

            TempData["Message"] = "Grup güncellendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "groups" });
        }
    }
}
